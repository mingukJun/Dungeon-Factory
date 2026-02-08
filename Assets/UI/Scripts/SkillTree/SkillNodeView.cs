using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum NodeState { Locked, Available, Purchased, Maxed }

[Serializable]
public struct NodeSkin   // 상태별 색/외형 프리셋
{
    public Color bg;
    public Color iconTint;
    public Color outline;
}

public class SkillNodeView : MonoBehaviour,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identity")]
    [SerializeField] private string nodeId;
    public string NodeId => nodeId;

    [Header("UI Refs")]
    [SerializeField] private Image bg;            // 바탕
    [SerializeField] private Image icon;          // 아이콘
    [SerializeField] private Image outline;       // 테두리(굵은 외곽선)
    [SerializeField] private GameObject lockGO;   // 자물쇠/비활성 오버레이(선택)
    [SerializeField] private GameObject badgeMax; // MAX 배지(선택)
    [SerializeField] private Text countText;      // "0/10" 같은 카운트(선택)
    [SerializeField] private CanvasGroup cg;      // 전체 투명도/인터랙션 제어(선택)

    [Header("Skins by State")]
    public NodeSkin locked;
    public NodeSkin available;
    public NodeSkin purchased;
    public NodeSkin maxed;

    [Header("Effects")]
    [Tooltip("강조 시 테두리 두께/스케일을 살짝 키워줌")]
    [SerializeField] private float outlinePulseScale = 1.08f;
    [SerializeField] private float outlinePulseTime = 0.12f;
    [SerializeField] private Material grayscaleMat; // 잠금 시 아이콘을 회색으로 보이게(선택)

    [Header("Popup")]
    [Tooltip("지정되면 클릭/롱프레스 시 이 프레젠터로 팝업 호출")]
    [SerializeField] private SkillPopupPresenter popupPresenter;
    [Tooltip("클릭으로 팝업을 띄울지 여부")]
    [SerializeField] private bool showPopupOnClick = true;
    [Tooltip("롱프레스(길게 누르기)로도 팝업을 띄울지 여부")]
    [SerializeField] private bool showPopupOnLongPress = true;
    [SerializeField, Range(0.2f, 1.0f)] private float longPressThreshold = 0.35f;

    [Header("Events")]
    public UnityEvent<SkillNodeView> onClicked;
    public UnityEvent<SkillNodeView> onLongPressed;
    public UnityEvent<SkillNodeView> onHoverEnter;
    public UnityEvent<SkillNodeView> onHoverExit;

    // --- runtime state ---
    public NodeState State { get; private set; } = NodeState.Locked;
    private bool _highlight;
    private bool _pressing;
    private float _pressTime;

    RectTransform RT => (RectTransform)transform;

    // ===== Public API =====
    public void Init(string id, Sprite iconSprite = null, string countLabel = null)
    {
        nodeId = id;
        if (icon && iconSprite) icon.sprite = iconSprite;
        if (countText && !string.IsNullOrEmpty(countLabel)) countText.text = countLabel;
        ApplyState(State, instant: true);
    }

    public void SetCount(int cur, int max)
    {
        if (!countText) return;
        countText.text = $"{cur}/{max}";
        bool isMax = max > 0 && cur >= max;
        if (badgeMax) badgeMax.SetActive(isMax);
        if (isMax) SetState(NodeState.Maxed);
    }

    public void SetState(NodeState state, bool animate = true)
    {
        State = state;
        ApplyState(state, instant: !animate);
    }

    public void SetInteractable(bool on)
    {
        if (cg)
        {
            cg.interactable = on;
            cg.blocksRaycasts = on;
            cg.alpha = on ? 1f : 0.55f;
        }
    }

    public void SetHighlight(bool on)
    {
        if (_highlight == on) return;
        _highlight = on;
        StopAllCoroutines();
        if (outline)
        {
            if (on) StartCoroutine(PulseOutline(outlinePulseScale, outlinePulseTime));
            else outline.rectTransform.localScale = Vector3.one;
        }
    }

    // ===== Pointer Events =====
    public void OnPointerClick(PointerEventData eventData)
    {
        // (롱프레스가 이미 처리했으면 클릭은 무시)
        if (_pressing) return;

        onClicked?.Invoke(this);
        if (showPopupOnClick) ShowPopup();
        // 클릭시 강조 한 번 톡
        QuickBump();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressing = true;
        _pressTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pressing) return;
        _pressing = false;

        float held = Time.unscaledTime - _pressTime;
        if (held >= longPressThreshold)
        {
            onLongPressed?.Invoke(this);
            if (showPopupOnLongPress) ShowPopup();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHoverEnter?.Invoke(this);
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke(this);
        SetHighlight(false);
    }

    // ===== Internals =====
    private void ApplyState(NodeState s, bool instant)
    {
        var skin = s switch
        {
            NodeState.Purchased => purchased,
            NodeState.Maxed => maxed,
            NodeState.Available => available,
            _ => locked
        };

        if (bg) bg.color = skin.bg;
        if (outline) outline.color = skin.outline;
        if (icon)
        {
            icon.color = skin.iconTint;
            icon.material = (s == NodeState.Locked && grayscaleMat) ? grayscaleMat : null;
        }
        if (lockGO) lockGO.SetActive(s == NodeState.Locked);
        if (badgeMax) badgeMax.SetActive(s == NodeState.Maxed);

        if (!instant && outline) QuickBump(); // 상태 전환시 살짝 반응
    }

    private void QuickBump()
    {
        if (!outline) return;
        StopAllCoroutines();
        StartCoroutine(PulseOutline(1.06f, 0.08f));
    }

    private System.Collections.IEnumerator PulseOutline(float scale, float time)
    {
        var t = 0f;
        var rt = outline.rectTransform;
        var from = Vector3.one;
        var to = Vector3.one * scale;
        // up
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = t / time;
            rt.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }
        // down
        t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = t / time;
            rt.localScale = Vector3.Lerp(to, from, k);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private void ShowPopup()
    {
        if (!popupPresenter) return;

        // 화면상의 앵커 좌표 계산
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, RT.position);
        popupPresenter.ShowForNode(this, screenPos);
    }
}