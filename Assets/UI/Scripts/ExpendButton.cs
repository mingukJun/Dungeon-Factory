using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpendButton : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private Button toggleButton;             // 펼침/접힘 버튼
    [SerializeField] private RectTransform content;           // 숨겨진 버튼들 부모
    [SerializeField] private LayoutElement contentLayout;     // content에 부착(권장)
    [SerializeField] private CanvasGroup contentCanvasGroup;  // content에 부착(권장, 페이드용)

    [Header("Label / Icon (Optional)")]
    [SerializeField] private TextMeshProUGUI label;           // 버튼 텍스트(선택)
    [SerializeField] private RectTransform arrowIcon;         // ▼ 아이콘(선택, 회전 연출)

    [Header("Animation")]
    [SerializeField] private bool startExpanded = false;
    [SerializeField] private float openDuration = 0.28f;
    [SerializeField] private float closeDuration = 0.22f;
    [SerializeField] private Ease openEase = Ease.OutCubic;
    [SerializeField] private Ease closeEase = Ease.InCubic;
    [Tooltip("자식 항목을 순차적으로 페이드/살짝 스케일업")]
    [SerializeField] private bool staggerChildren = true;
    [SerializeField] private float childStagger = 0.02f;
    [SerializeField] private float childScalePunch = 0.06f; // 1+값까지 커졌다가 안착

    [Header("Options")]
    [Tooltip("Time.timeScale의 영향 배제(추천)")]
    [SerializeField] private bool timeScaleIndependent = true;
    [SerializeField] private bool blockRaycastsWhenClosed = true;

    // State
    public bool IsExpanded { get; private set; }

    // Internal
    private float measuredHeight;  // 실제 콘텐츠 높이
    private Sequence currentSeq;

    void Reset()
    {
        // 자동 참조 시도(에디터에서 Reset 누르면 기본 세팅)
        if (!toggleButton) toggleButton = GetComponentInChildren<Button>();
        if (!content) content = transform.Find("HiddenGroup") as RectTransform;
        if (content)
        {
            if (!contentLayout) contentLayout = content.GetComponent<LayoutElement>() ?? content.gameObject.AddComponent<LayoutElement>();
            if (!contentCanvasGroup) contentCanvasGroup = content.GetComponent<CanvasGroup>() ?? content.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Awake()
    {
        if (!contentLayout) contentLayout = content.gameObject.AddComponent<LayoutElement>();
        if (!contentCanvasGroup) contentCanvasGroup = content.gameObject.AddComponent<CanvasGroup>();

        // 높이 측정(레이아웃 강제 갱신)
        ForceRebuild(content);
        measuredHeight = GetContentPreferredHeight();

        // 초기 상태 세팅
        SetExpanded(startExpanded, instant: true);

        // 클릭 이벤트
        if (toggleButton) toggleButton.onClick.AddListener(Toggle);
    }

    /// <summary>외부에서 토글 호출</summary>
    public void Toggle()
    {
        if (IsExpanded) Close(); else Open();
    }

    public void Open(bool instant = false)
    {
        if (IsExpanded && !instant) return;
        IsExpanded = true;
        PlayAnim(isOpen: true, instant: instant);
    }

    public void Close(bool instant = false)
    {
        if (!IsExpanded && !instant) return;
        IsExpanded = false;
        PlayAnim(isOpen: false, instant: instant);
    }

    public void SetExpanded(bool expanded, bool instant = false)
    {
        IsExpanded = expanded;
        PlayAnim(isOpen: expanded, instant: instant);
    }

    private void PlayAnim(bool isOpen, bool instant)
    {
        // 기존 트윈 정리
        currentSeq?.Kill(false);

        // 매 프레임 측정은 비효율이므로 필요할 때만 측정
        ForceRebuild(content);
        measuredHeight = GetContentPreferredHeight();

        // 콘텐츠 활성/비활성 & 입력 차단
        content.gameObject.SetActive(true);

        // 목표값 세팅
        float targetHeight = isOpen ? measuredHeight : 0f;
        float duration = isOpen ? openDuration : closeDuration;
        Ease ease = isOpen ? openEase : closeEase;

        if (instant)
        {
            contentLayout.preferredHeight = targetHeight;
            contentCanvasGroup.alpha = isOpen ? 1f : 0f;
            contentCanvasGroup.interactable = isOpen;
            contentCanvasGroup.blocksRaycasts = isOpen || !blockRaycastsWhenClosed ? true : false;
            UpdateLabelAndArrow(isOpen);

            // 닫힘 즉시 비활성화(레이아웃 안정 후)
            if (!isOpen)
                content.gameObject.SetActive(false);
            return;
        }

        // 메인 시퀀스
        currentSeq = DOTween.Sequence();

        // 높이 트윈 (Layout 친화)
        var hTween = DOTween.To(
            () => contentLayout.preferredHeight,
            v => contentLayout.preferredHeight = v,
            targetHeight,
            duration
        ).SetEase(ease);

        // 페이드
        var aTween = contentCanvasGroup.DOFade(isOpen ? 1f : 0f, duration * 0.9f).SetEase(ease);

        // 아이콘 회전(선택)
        if (arrowIcon)
        {
            float endZ = isOpen ? 180f : 0f; // ▼(0) ↔ ▲(180) 기준
            currentSeq.Join(arrowIcon.DOLocalRotate(new Vector3(0, 0, endZ), duration).SetEase(ease));
        }

        // 시퀀스 구성
        currentSeq.Join(hTween);
        currentSeq.Join(aTween);

        // 자식 스태거
        if (staggerChildren && isOpen)
        {
            int idx = 0;
            foreach (Transform child in content)
            {
                if (!child.gameObject.activeSelf) continue;
                var cg = child.GetComponent<CanvasGroup>() ?? child.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                child.localScale = Vector3.one * (1f - childScalePunch);

                currentSeq.Insert(idx * childStagger,
                    cg.DOFade(1f, Mathf.Max(0.12f, duration * 0.35f)));
                currentSeq.Insert(idx * childStagger,
                    child.DOScale(1f, Mathf.Max(0.12f, duration * 0.35f)).SetEase(Ease.OutCubic));

                idx++;
            }
        }

        // 상호작용/레이캐스트
        contentCanvasGroup.interactable = isOpen;
        contentCanvasGroup.blocksRaycasts = isOpen || !blockRaycastsWhenClosed ? true : false;

        // 라벨 갱신
        UpdateLabelAndArrow(isOpen);

        // 타임스케일 무시
        if (timeScaleIndependent) currentSeq.SetUpdate(true);

        // 닫힐 때 마지막에 비활성화
        currentSeq.OnComplete(() =>
        {
            if (!IsExpanded)
                content.gameObject.SetActive(false);
        });
    }

    private void UpdateLabelAndArrow(bool isOpen)
    {
        if (label)
            label.text = isOpen ? "닫기 ▲" : "더보기 ▼";
    }

    private static void ForceRebuild(RectTransform target)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        var parent = target.parent as RectTransform;
        if (parent) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
    }

    private float GetContentPreferredHeight()
    {
        // 콘텐츠의 실제 필요 높이(레이아웃 기반) 추출
        // ContentSizeFitter/VerticalLayoutGroup 조합과 잘 맞음
        var rt = content;
        return Mathf.Max(rt.rect.height, LayoutUtility.GetPreferredHeight(rt));
    }

    // --- 퍼포먼스 팁 ---
    // 1) content에는 VerticalLayoutGroup + ContentSizeFitter(preferredHeight) 권장
    // 2) 버튼 등 자식에는 LayoutElement로 최소/선호 높이 지정
    // 3) 빈번한 토글이 있을 경우 DOTween.SetTweensCapacity로 풀 여유 확보
}