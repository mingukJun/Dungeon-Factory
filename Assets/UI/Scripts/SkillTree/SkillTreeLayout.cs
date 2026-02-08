using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class NodeDef
{
    public string id;
    [TextArea] public string title;
    public List<string> children = new(); // 간단화: 자식만 적는다
}

public class SkillTreeLayout : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform content;         // ScrollView Content
    [SerializeField] private RectTransform connectionsRoot; // 선 부모(옵션, 없으면 content 사용)
    [SerializeField] private SkillNodeView nodePrefab;
    [SerializeField] private Image linePrefab;              // 얇은 Image

    [Header("Layout")]
    [SerializeField] private Vector2 startOffset = new(100, -100);
    [SerializeField] private float xSpacing = 220f;
    [SerializeField] private float ySpacing = 160f;
    [SerializeField] private float lineThickness = 8f;

    [Header("Test Data (임시)")]
    [SerializeField]
    private List<NodeDef> testNodes = new()
    {
        new NodeDef{ id="A", title="Root", children = new(){ "B","C" } },
        new NodeDef{ id="B", title="B", children = new(){ "D","E" } },
        new NodeDef{ id="C", title="C", children = new(){ "F" } },
        new NodeDef{ id="D", title="D", children = new() },
        new NodeDef{ id="E", title="E", children = new(){ "G" } },
        new NodeDef{ id="F", title="F", children = new() },
        new NodeDef{ id="G", title="G", children = new() },
    };

    private readonly Dictionary<string, SkillNodeView> _spawned = new();
    private readonly Dictionary<string, List<string>> _parents = new();
    private readonly Dictionary<string, int> _level = new();  // x-축(깊이)

    [ContextMenu("Build (TestNodes)")]
    public void BuildFromTest()
    {
        Build(testNodes);
    }

    public void Build(List<NodeDef> defs)
    {
        if (!content) content = (RectTransform)transform;
        if (!connectionsRoot) connectionsRoot = content;

        Clear();

        // 1) 인덱스
        var byId = defs.ToDictionary(d => d.id, d => d);
        // 부모표 역연결 구성
        foreach (var d in defs)
        {
            if (!_parents.ContainsKey(d.id)) _parents[d.id] = new();
            foreach (var c in d.children)
            {
                if (!_parents.ContainsKey(c)) _parents[c] = new();
                _parents[c].Add(d.id);
            }
        }

        // 2) 루트=부모가 없는 노드들
        var roots = defs.Select(d => d.id).Where(id => _parents[id].Count == 0).ToList();
        if (roots.Count == 0) roots.Add(defs[0].id); // 안전장치

        // 3) 레벨(BFS) 계산
        var q = new Queue<string>();
        foreach (var r in roots) { _level[r] = 0; q.Enqueue(r); }
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var child in byId[cur].children)
            {
                var next = _level[cur] + 1;
                if (!_level.ContainsKey(child) || next < _level[child])
                {
                    _level[child] = next;
                    q.Enqueue(child);
                }
            }
        }

        // 4) 같은 레벨별 정렬(간단히 DFS 순서)
        var levels = new Dictionary<int, List<string>>();
        foreach (var kv in _level)
        {
            if (!levels.ContainsKey(kv.Value)) levels[kv.Value] = new();
            levels[kv.Value].Add(kv.Key);
        }
        foreach (var k in levels.Keys.ToList())
            levels[k] = OrderByDfs(roots, byId).Where(id => _level[id] == k).ToList();

        // 5) 노드 생성 & 위치 배치
        var positions = new Dictionary<string, Vector2>();
        int maxCols = levels.Keys.Count == 0 ? 1 : (levels.Keys.Max() + 1);
        int maxRows = 1;

        foreach (var col in Enumerable.Range(0, maxCols))
        {
            if (!levels.ContainsKey(col)) continue;
            var list = levels[col];
            maxRows = Mathf.Max(maxRows, list.Count);

            for (int row = 0; row < list.Count; row++)
            {
                var id = list[row];
                var node = Instantiate(nodePrefab, content);
               // node.Init(id, byId[id].title);
                var pos = startOffset + new Vector2(col * xSpacing, -row * ySpacing);
              //  node.RT.anchoredPosition = pos;
                positions[id] = pos;
                _spawned[id] = node;
            }
        }

        // 6) 선 생성
        foreach (var from in defs)
        {
            foreach (var toId in from.children)
            {
                if (!positions.ContainsKey(from.id) || !positions.ContainsKey(toId)) continue;
                var line = Instantiate(linePrefab, connectionsRoot);
               // UILineBetween.Stretch((RectTransform)line.transform, positions[from.id], positions[toId], lineThickness);
            }
        }

        // 7) Content 크기 보정(스크롤/줌 여유)
        var w = startOffset.x + (maxCols - 1) * xSpacing + 400f;
        var h = -startOffset.y + (maxRows - 1) * ySpacing + 400f;
        content.sizeDelta = new Vector2(w, h);
    }

    private void Clear()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);
        if (connectionsRoot)
            for (int i = connectionsRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(connectionsRoot.GetChild(i).gameObject);

        _spawned.Clear();
        _parents.Clear();
        _level.Clear();
    }

    // DFS 기반 순회 순서(간단 버전): 형제간 교차 조금 줄임
    private List<string> OrderByDfs(List<string> roots, Dictionary<string, NodeDef> byId)
    {
        var seen = new HashSet<string>();
        var order = new List<string>();
        foreach (var r in roots)
            Dfs(r);
        void Dfs(string id)
        {
            if (seen.Contains(id)) return;
            seen.Add(id);
            order.Add(id);
            foreach (var c in byId[id].children)
                Dfs(c);
        }
        return order;
    }
}