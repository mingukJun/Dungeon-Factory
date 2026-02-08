using BansheeGz.BGDatabase;
using System;
using System.IO;
using UnityEngine;

public class GameSaveLoadManager : MonoBehaviour
{
    public static GameSaveLoadManager Instance { get; private set; }

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "gameSave.dat");

    private void Awake()
    {
        // 싱글턴 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 게임 시작 시 자동 로드
        LoadGame();
    }

    // ============================================================
    // 저장
    // ============================================================
    public void SaveGame()
    {
        try
        {
            var addon = BGRepo.I.Addons.Get<BGAddonSaveLoad>();
            if (addon == null)
            {
                Debug.LogError("BGAddonSaveLoad 애드온을 찾을 수 없습니다. BGDatabase 설정을 확인하세요.");
                return;
            }

            byte[] bytes = addon.Save();
            File.WriteAllBytes(SaveFilePath, bytes);
            Debug.Log($"[GameSaveLoad] 저장 성공: {SaveFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameSaveLoad] 저장 실패: {ex}");
        }
    }

    // ============================================================
    // 불러오기
    // ============================================================
    public void LoadGame()
    {
        try
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning($"[GameSaveLoad] 저장 파일이 존재하지 않습니다. 경로: {SaveFilePath}");
                return;
            }

            var addon = BGRepo.I.Addons.Get<BGAddonSaveLoad>();
            if (addon == null)
            {
                Debug.LogError("BGAddonSaveLoad 애드온을 찾을 수 없습니다. BGDatabase 설정을 확인하세요.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(SaveFilePath);
            addon.Load(bytes);
            Debug.Log("[GameSaveLoad] 불러오기 성공");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameSaveLoad] 불러오기 실패: {ex}");
        }
    }

    // ============================================================
    // 종료 시 자동 저장 (원하면 주석 해제)
    // ============================================================
    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
