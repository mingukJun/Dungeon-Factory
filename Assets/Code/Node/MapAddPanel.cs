using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapAddPanel : MonoBehaviour
{
    public static MapAddPanel Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private AddMapItem itemPrefab;
    [SerializeField] private Transform itemParent;

    [Header("Dim Background")]
    [SerializeField] private Button dimRoot;   // 풀스크린 Dim 오브젝트 (Image + Button)

    [Header("Buttons")]
    [SerializeField] private Button btnCancel;

    private readonly List<AddMapItem> spawnedItems = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (btnCancel != null)
            btnCancel.onClick.AddListener(OnClickCancel);

        if (dimRoot != null)
            dimRoot.onClick.AddListener(OnClickDim);

        Hide();
    }

    // 외부에서 호출: 팝업 열기
    public void Show()
    {
        // Dim + Panel 활성화
        if (dimRoot != null)
            dimRoot.gameObject.SetActive(true);
        panelRoot.gameObject.SetActive(true);

        RefreshList();
    }

    public void Hide()
    {
        panelRoot.gameObject.SetActive(false);

        if (dimRoot != null)
            dimRoot.gameObject.SetActive(false);
    }

    private void RefreshList()
    {
        // 기존 아이템 정리
        foreach (Transform child in itemParent)
            Destroy(child.gameObject);

        spawnedItems.Clear();

        // MapUpgrade DB에서 맵 가져오기 (필요하면 필터링)
        List<C_P_MapUpgrade> openList = new();

        C_P_MapUpgrade.ForEachEntity(p =>
        {
            if (p.F_isOpen && p.F_map != null)
                openList.Add(p);
        });

        // map만 추출
        List<C_M_Map> mapList = new();
        foreach (var row in openList)
            mapList.Add(row.F_map);

        int total = mapList.Count;

        // UI 생성
        for (int i = 0; i < mapList.Count; i++)
        {
            var map = mapList[i];

            var item = Instantiate(itemPrefab, itemParent);
            item.Setup(map, i + 1, total, OnClickAddMap);

            spawnedItems.Add(item);  // 여기에는 AddMapItem 타입만 넣음
        }
    }

    /// 아이템의 Add 버튼 눌렀을 때 호출
    private void OnClickAddMap(C_M_Map map)
    {
        // Player DB : P_MapUpgrade 테이블에 기록
        var exist = C_P_MapUpgrade.FindEntity(r => r.F_map == map);

        if (exist == null)
        {
            Debug.Log("해당 맵이 P_MapUpgrade DB에 없음");
            return;
        }

        if (exist.F_currentCount >= exist.F_maxCount)
        {
            Debug.Log($"[{map.F_name}] 맵은 이미 최대 수량({exist.F_maxCount})에 도달했습니다.");
            // 여기서 팝업 띄우거나 토스트 메시지 띄우고 싶으면 추가하면 됨.
            return;
        }

        // 아직 여유가 있으면 개수 증가
        exist.F_currentCount++;

        // 저장
        GameSaveLoadManager.Instance.SaveGame();  // BGDatabase SaveLoad 애드온 사용한 저장 매니저 :contentReference[oaicite:2]{index=2}
    }

    private void OnClickCancel()
    {
        Hide();
    }

    private void OnClickDim()
    {
        Hide();
    }
}
