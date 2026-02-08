using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeActionPanel : MonoBehaviour
{
    public static NodeActionPanel Current { get; private set; }

    [Header("Panel")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private NodeActionButton buttonPrefab;
    [SerializeField] private Transform buttonParent;

    [Header("Dim Background")]
    [SerializeField] private Button dimRoot;   // 풀스크린 Dim 오브젝트 (Image + Button)

    [Header("Buttons")]
    [SerializeField] private Button btnCancel;

    private readonly List<NodeActionButton> spawnedButtons = new();

    private void Awake()
    {
        Current = this;

        if (btnCancel != null)
            btnCancel.onClick.AddListener(OnClickCancel);

        if (dimRoot != null)
            dimRoot.onClick.AddListener(OnClickDim);

        Hide();
    }

    /// <summary>
    /// Node 액션 목록을 띄운다.
    /// </summary>
    public void Show(List<NodeAction> actions)
    {
        // 기존 버튼 정리
        foreach (var b in spawnedButtons)
        {
            Destroy(b.gameObject);
        }
        spawnedButtons.Clear();

        // 액션만큼 버튼 생성
        foreach (var action in actions)
        {
            var btn = Instantiate(buttonPrefab, buttonParent);
            btn.Setup(action.label, action.callback);
            spawnedButtons.Add(btn);
        }

        // Dim + Panel 활성화
        if (dimRoot != null)
            dimRoot.gameObject.SetActive(true);

        panelRoot.gameObject.SetActive(true);
    }

    public void Hide()
    {
        panelRoot.gameObject.SetActive(false);

        if (dimRoot != null)
            dimRoot.gameObject.SetActive(false);
    }

    private void OnClickCancel()
    {
        Hide();
    }

    /// <summary>
    /// Dim 배경을 클릭했을 때 이벤트 (버튼 OnClick에 연결)
    /// </summary>
    private void OnClickDim()
    {
        Hide();
    }
}
