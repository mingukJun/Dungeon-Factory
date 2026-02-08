using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 유니티 프로젝트와 제미나이 AI를 실시간으로 연결하는 도구입니다.
/// Firestore REST API의 경로 규칙(짝수 세그먼트)을 준수하여 수정되었습니다.
/// </summary>
public class GeminiScriptEditor : EditorWindow
{
    private string appId = "default-app-id";
    private string apiKey = ""; // Google AI Studio API 키
    private string projectId = "my-game-6a914"; // Firebase 프로젝트 ID

    private string targetSubFolder = "Code";
    private bool ignorePlugins = true;

    private bool isProcessing = false;
    private string statusMessage = "준비 완료. [현황 동기화] 버튼을 눌러주세요.";

    [MenuItem("Gemini AI/Open AI Bridge")]
    public static void ShowWindow()
    {
        GeminiScriptEditor window = GetWindow<GeminiScriptEditor>("제미나이 브릿지");
        window.minSize = new Vector2(400, 350);
    }

    private void OnGUI()
    {
        GUILayout.Label("제미나이 실시간 프로젝트 브릿지", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");
        apiKey = EditorGUILayout.PasswordField("Gemini API Key", apiKey);
        projectId = EditorGUILayout.TextField("Firebase Project ID", projectId);
        appId = EditorGUILayout.TextField("App ID", appId);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        GUILayout.Label("필터 설정", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        targetSubFolder = EditorGUILayout.TextField("대상 폴더 (Assets/)", targetSubFolder);
        ignorePlugins = EditorGUILayout.Toggle("Plugins 폴더 제외", ignorePlugins);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUI.enabled = !isProcessing;
        if (GUILayout.Button("선택한 폴더 현황 동기화", GUILayout.Height(35)))
        {
            _ = SyncProjectContext();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("클라우드 명령 반영 (Sync Commands)", GUILayout.Height(35)))
        {
            _ = SyncFromCloud();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("상태:", statusMessage, EditorStyles.wordWrappedLabel);
    }

    private async Task SyncProjectContext()
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(apiKey))
        {
            statusMessage = "Project ID와 API Key를 입력해주세요.";
            return;
        }

        isProcessing = true;
        statusMessage = "코드 분석 중...";
        Repaint();

        try
        {
            string searchRoot = Path.Combine(Application.dataPath, targetSubFolder);
            if (!Directory.Exists(searchRoot))
            {
                statusMessage = $"오류: '{targetSubFolder}' 폴더를 찾을 수 없습니다.";
                isProcessing = false;
                return;
            }

            string[] allFiles = Directory.GetFiles(searchRoot, "*.cs", SearchOption.AllDirectories);
            List<string> filteredFiles = new List<string>();

            foreach (var file in allFiles)
            {
                string normalizedPath = file.Replace('\\', '/');
                if (normalizedPath.Contains("/Editor/")) continue;
                if (ignorePlugins && normalizedPath.Contains("/Plugins/")) continue;
                filteredFiles.Add(file);
            }

            StringBuilder contextBuilder = new StringBuilder();
            contextBuilder.AppendLine($"Last Sync: {DateTime.Now}");
            contextBuilder.AppendLine($"Scan Root: Assets/{targetSubFolder}");
            contextBuilder.AppendLine("---");

            foreach (string file in filteredFiles)
            {
                string relativePath = "Assets" + file.Replace(Application.dataPath, "").Replace('\\', '/');
                string content = File.ReadAllText(file);
                contextBuilder.AppendLine($"File: {relativePath}\n{content}\n---");
            }

            string dataToUpload = contextBuilder.ToString();

            // 필수 규칙 준수: artifacts/{appId}/public/data/project_context/latest (6개 세그먼트)
            await UploadToCloud("project_context", "latest", dataToUpload);
            statusMessage = $"동기화 성공! ({filteredFiles.Count}개 파일 보고됨)";
        }
        catch (Exception e)
        {
            statusMessage = "동기화 오류: " + e.Message;
            Debug.LogError($"[Gemini Error] {e.Message}");
        }
        isProcessing = false;
        Repaint();
    }

    private async Task SyncFromCloud()
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(apiKey)) return;
        isProcessing = true;
        statusMessage = "명령 확인 중...";
        try
        {
            // 필수 규칙 준수: artifacts/{appId}/public/data/latest_command/info (6개 세그먼트)
            string safeAppId = Uri.EscapeDataString(appId.Trim());
            string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/artifacts/{safeAppId}/public/data/latest_command/info?key={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    ProcessIncomingCommand(json);
                    statusMessage = "명령 실행 완료!";
                }
                else statusMessage = "새로운 명령 없음.";
            }
        }
        catch (Exception e) { statusMessage = "수신 오류: " + e.Message; }
        isProcessing = false;
        Repaint();
    }

    private async Task UploadToCloud(string collectionName, string documentId, string data)
    {
        string safeAppId = Uri.EscapeDataString(appId.Trim());
        string safeColl = Uri.EscapeDataString(collectionName.Trim());
        string safeDoc = Uri.EscapeDataString(documentId.Trim());

        // Firestore REST PATCH URL 구성
        // updateMask.fieldPaths=content 를 추가하여 'content' 필드만 업데이트함을 명시 (BadRequest 방지)
        string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/artifacts/{safeAppId}/public/data/{safeColl}/{safeDoc}?key={apiKey}&updateMask.fieldPaths=content";

        using (HttpClient client = new HttpClient())
        {
            string jsonPayload = "{\"fields\": {\"content\": {\"stringValue\": \"" + JsonEscape(data) + "\"}}}";
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"{response.StatusCode}: {errorBody}");
            }
        }
    }

    private void ProcessIncomingCommand(string json)
    {
        string commandType = GetVal(json, "commandType");
        string filePath = GetVal(json, "filePath");
        string code = GetVal(json, "code");
        if (string.IsNullOrEmpty(commandType) || string.IsNullOrEmpty(filePath)) return;

        string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), filePath);

        if (commandType == "Delete") AssetDatabase.DeleteAsset(filePath);
        else if (commandType == "Modify")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, code);
            AssetDatabase.ImportAsset(filePath);
        }
        AssetDatabase.Refresh();
    }

    private string GetVal(string json, string field)
    {
        try
        {
            string key = $"\"{field}\":";
            int idx = json.IndexOf(key);
            if (idx == -1) return null;
            string vKey = "\"stringValue\": \"";
            int start = json.IndexOf(vKey, idx) + vKey.Length;
            int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start).Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
        catch { return null; }
    }

    private string JsonEscape(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}