using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : SerializedMonoBehaviour
{
    private const PanelType DEFAULT_PANEL = PanelType.Map;

    [Title("패널 매핑")]
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.Foldout,
            KeyLabel = "Panel", ValueLabel = "Root", IsReadOnly = false)]
    [SerializeField] private Dictionary<PanelType, GameObject> map = new();

    [Title("초기 설정"), EnumToggleButtons]
    [SerializeField] private PanelType defaultPanel = DEFAULT_PANEL;

    [ShowInInspector, ReadOnly]
    private PanelType current = DEFAULT_PANEL;

    // 에디터에서 즉시 확인용 미리보기
    [PropertySpace(6)]
    [EnumToggleButtons, LabelText("미리보기 전환"), OnValueChanged(nameof(__PreviewSelect)), DisableInPlayMode]
    [SerializeField] private PanelType previewSelect = DEFAULT_PANEL;

    void Start()
    {
        HideAll();
        Show(defaultPanel);
    }

    public void Show(PanelType id)
    {
        if (current == id) return;
        HideAll();

        if (id != PanelType.None && map.TryGetValue(id, out var go) && go)
        {
            go.SetActive(true);
            current = id;
        }
        else current = DEFAULT_PANEL;
    }

    [Button("Hide All", ButtonSizes.Small)]
    public void HideAll()
    {
        foreach (var kv in map)
            if (kv.Value) kv.Value.SetActive(false);
        current = PanelType.None;
    }

    public PanelType Current => current;

    // ---------- Odin 편의 기능 ----------

    // 이름 규칙(예: "Panel_Inventory")으로 자동 연결
    [Button("Auto-Bind by Name"), PropertySpace(10)]
    private void AutoBind()
    {
        foreach (PanelType id in System.Enum.GetValues(typeof(PanelType)))
        {
            if (id == PanelType.None) continue;
            string name = $"Panel_{id}";
            var go = GameObject.Find(name);
            if (go) map[id] = go;
        }
    }

    // 에디터 미리보기 전환
    private void __PreviewSelect()
    {
        if (Application.isPlaying) return;
        HideAll();
        if (previewSelect != PanelType.None && map.TryGetValue(previewSelect, out var go) && go)
            go.SetActive(true);
    }
}
