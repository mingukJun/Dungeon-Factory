using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
[CustomEditor(typeof(CustomScrollRect))]
public class CustomScrollRect_OdinEditor : OdinEditor { }
#endif

[RequireComponent(typeof(RectTransform))]
public class CustomScrollRect : ScrollRect
{
    // ---------- Setup ----------
    [TitleGroup("Setup", Alignment = TitleAlignments.Split)]
    [LabelText("Viewport (생략 시 this.viewport)")]
    [SerializeField] private RectTransform viewportOverride;

    [TitleGroup("Setup", Alignment = TitleAlignments.Split)]
    [LabelText("Content (생략 시 this.content)")]
    [SerializeField] private RectTransform contentOverride;

    [TitleGroup("Setup")]
    [LabelText("공통 설정 프로필")]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [SerializeField] private CustomScrollRectProfile profile;

    // ---------- Profile Toggle ----------
    [TabGroup("Params", "Zoom"), PropertySpace(SpaceBefore = 8)]
    [ToggleLeft, LabelText("프로필 값 사용(ON) / 개별 세팅(OFF)")]
    public bool useProfile = true;

    // ---------- Params (개별 오버라이드용) ----------
    [TabGroup("Params", "Zoom"), ShowIf("@!useProfile")]
    [InfoBox("더블탭/더블클릭 시 toggleZoomFactor 배로 진입/복귀")]
    [MinMaxSlider(0.1f, 10f, true), LabelText("배율 범위 (min ~ max)")]
    public Vector2 scaleRange = new Vector2(0.5f, 3f);

    [TabGroup("Params", "Zoom"), ShowIf("@!useProfile")]
    [LabelText("휠 감도"), PropertyRange(0.01f, 0.5f)]
    public float wheelZoomSpeed = 0.08f;

    [TabGroup("Params", "Zoom"), ShowIf("@!useProfile")]
    [LabelText("핀치 감도"), PropertyRange(0.001f, 0.02f)]
    public float pinchZoomSpeed = 0.005f;

    [TabGroup("Params", "Zoom"), ShowIf("@!useProfile")]
    [LabelText("토글 배율"), PropertyRange(1.2f, 4f)]
    public float toggleZoomFactor = 2.0f;

    [TabGroup("Params", "Zoom"), ShowIf("@!useProfile")]
    [HorizontalGroup("Params/Zoom/DoubleTap", Width = 0.5f)]
    [LabelText("더블탭 시간(s)"), PropertyRange(0.15f, 0.5f)]
    public float doubleTapSeconds = 0.28f;

    [TabGroup("Params", "Zoom"), ShowIf("@!useProfile")]
    [HorizontalGroup("Params/Zoom/DoubleTap")]
    [LabelText("더블탭 픽셀"), PropertyRange(8f, 80f)]
    public float doubleTapMaxPixels = 40f;

    [TabGroup("Params", "Pan"), ShowIf("@!useProfile")]
    [InfoBox("ScrollRect의 드래그/관성/클램프는 그대로 사용합니다.", InfoMessageType = InfoMessageType.None)]
    [LabelText("작게 중앙정렬(자동)")]
    public bool centerWhenSmaller = true;

    // ---------- Debug ----------
    [TabGroup("Debug"), ShowInInspector, ReadOnly, LabelText("현재 배율")]
    public float Scale => _scale;

    [TabGroup("Debug"), ShowInInspector, ReadOnly, LabelText("Content sizeDelta")]
    public Vector2 ContentSize => _ct ? _ct.sizeDelta : default;

    [TabGroup("Debug")]
    [Button("배율 초기화(=1)"), GUIColor(0.2f, 0.6f, 1f)]
    public void ResetZoom()
    {
        if (!_ct) return;
        SetScale(1f);
        ClampAndCenterIfSmaller();
        UpdateBounds();
    }

    [TabGroup("Debug")]
    [Button("경계 보정(Clamp)"), GUIColor(0.6f, 0.9f, 0.5f)]
    public void ClampNow()
    {
        ClampAndCenterIfSmaller();
        UpdateBounds();
    }

    // ---------- 내부 상태 ----------
    private RectTransform _vp;
    private RectTransform _ct;
    private Vector2 _baseSize;
    private float _scale = 1f;
    private float _lastTapTime = -10f;
    private Vector2 _lastTapPos;
    private float _rememberedScale = -1f;

    // 실효값(프로필/개별 중 실제로 쓰는 값)
    Vector2 _scaleRangeEff; float _wheelEff, _pinchEff, _toggleEff, _dtEff, _pxEff; bool _centerEff;

    protected override void OnEnable()
    {
        base.OnEnable();

        // 기본 슬롯 → override에 자동 복사
        if (!viewportOverride && viewport) viewportOverride = viewport;
        if (!contentOverride && content) contentOverride = content;

        _vp = viewportOverride ? viewportOverride : viewport;
        _ct = contentOverride ? contentOverride : content;

        if (!_vp || !_ct)
        {
            Debug.LogError("[CustomScrollRect] viewport/content가 필요합니다.");
            enabled = false; return;
        }

        ApplyProfileIfAny();

        _baseSize = _ct.sizeDelta;
        _scale = 1f;

        if (movementType == MovementType.Unrestricted)
            movementType = MovementType.Clamped;

        ClampAndCenterIfSmaller();
        UpdateBounds();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        // 에디터에서 즉시 동기화
        if (viewportOverride) viewport = viewportOverride;
        if (contentOverride) content = contentOverride;
        ApplyProfileIfAny();
    }
#endif

    // ---------- Profile -> 실효값 ----------
    void ApplyProfileIfAny()
    {
        if (useProfile && profile)
        {
            _scaleRangeEff = profile.scaleRange;
            _wheelEff = profile.wheelZoomSpeed;
            _pinchEff = profile.pinchZoomSpeed;
            _toggleEff = profile.toggleZoomFactor;
            _dtEff = profile.doubleTapSeconds;
            _pxEff = profile.doubleTapMaxPixels;
            _centerEff = profile.centerWhenSmaller;
        }
        else
        {
            _scaleRangeEff = scaleRange;
            _wheelEff = wheelZoomSpeed;
            _pinchEff = pinchZoomSpeed;
            _toggleEff = toggleZoomFactor;
            _dtEff = doubleTapSeconds;
            _pxEff = doubleTapMaxPixels;
            _centerEff = centerWhenSmaller;
        }
    }

    // ---------- Input ----------
    void Update()
    {
        HandleMouse();
        HandleTouches();
    }

    void HandleMouse()
    {
        var mouse = Mouse.current; if (mouse == null) return;
        if (mouse.leftButton.wasPressedThisFrame)
            TryDoubleTap(mouse.position.ReadValue(), true);
    }

    void HandleTouches()
    {
        var t = Touchscreen.current; if (t == null) return;

        int active = 0; int i0 = -1, i1 = -1;
        for (int i = 0; i < t.touches.Count; i++)
            if (t.touches[i].press.isPressed) { if (i0 < 0) i0 = i; else i1 = i; active++; }

        if (active == 1)
        {
            var touch = t.touches[i0];
            if (touch.press.wasPressedThisFrame)
                TryDoubleTap(touch.position.ReadValue(), false);
        }
        else if (active >= 2)
        {
            var a = t.touches[i0]; var b = t.touches[i1];
            if (!a.press.isPressed || !b.press.isPressed) return;

            Vector2 p0 = a.position.ReadValue(), p1 = b.position.ReadValue();
            Vector2 prev0 = p0 - a.delta.ReadValue(), prev1 = p1 - b.delta.ReadValue();
            float delta = (p0 - p1).magnitude - (prev0 - prev1).magnitude;
            if (Mathf.Abs(delta) > 0.1f)
            {
                Vector2 center = (p0 + p1) * 0.5f;
                float target = _scale * (1f + delta * _pinchEff);
                ZoomAtScreenPoint(center, target, false);
            }
        }
    }

    // ---------- 휠 => 줌 ----------
    public override void OnScroll(PointerEventData data)
    {
        if (!_vp || !_ct) return;
        float wheelY = data.scrollDelta.y;
        if (Mathf.Approximately(wheelY, 0f)) return;

        float target = _scale * (1f + wheelY * _wheelEff);
        ZoomAtScreenPoint(data.position, target, false);
        // base.OnScroll(data); // 스크롤 이동 막기
    }

    // ---------- Zoom / DoubleTap ----------
    void TryDoubleTap(Vector2 screenPos, bool isMouse)
    {
        float now = Time.unscaledTime;
        if (now - _lastTapTime <= _dtEff &&
            Vector2.Distance(screenPos, _lastTapPos) <= _pxEff)
        {
            float min = _scaleRangeEff.x, max = _scaleRangeEff.y;
            float baseScale = (_rememberedScale > 0f) ? _rememberedScale : Mathf.Clamp01((min + max) * 0.5f);
            bool goIn = _scale < baseScale * _toggleEff * 0.95f;
            float target = goIn ? baseScale * _toggleEff : baseScale;
            if (goIn && _rememberedScale < 0f) _rememberedScale = _scale;

            ZoomAtScreenPoint(screenPos, target, true);
        }
        else
        {
            _lastTapTime = now; _lastTapPos = screenPos;
        }
    }

    Camera GetCanvasEventCamera()
    {
        if (!_vp) return null;
        var canvas = _vp.GetComponentInParent<Canvas>();
        if (!canvas) return null;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    // --- 커서/손가락 아래의 "콘텐츠 로컬 포인트"를 기준으로 고정 ---
    void ZoomAtScreenPoint(Vector2 screenPoint, float targetScale, bool clampFocusWhenClamped)
    {
        float min = _scaleRangeEff.x, max = _scaleRangeEff.y;
        float newScale = Mathf.Clamp(targetScale, min, max);
        bool atLimit = Mathf.Approximately(newScale, _scale);
        if (atLimit && !clampFocusWhenClamped)
        {
            ClampAndCenterIfSmaller();
            UpdateBounds();
            return;
        }

        var cam = GetCanvasEventCamera();

        // 1) Viewport 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_vp, screenPoint, cam, out var vpLocal);

        // 2) "포인터가 가리키는 콘텐츠의 로컬 좌표" 확보 (스케일 적용 전)
        //    - 뷰포트 로컬 → 월드 → 콘텐츠 로컬(피벗 기준)
        Vector3 worldOnVP = _vp.TransformPoint(vpLocal);
        Vector2 ctLocalUnderCursor_Before = _ct.InverseTransformPoint(worldOnVP);

        // 3) 스케일 적용
        SetScale(newScale); // localScale 기반 (아래 SetScale 참고)

        // 4) 위에서 잡은 콘텐츠 로컬 포인트가 "다시" 포인터 아래로 오도록 content를 이동
        //    - 방금 스케일된 콘텐츠에서 그 로컬 포인트의 월드 좌표
        Vector3 worldOfThatLocal_After = _ct.TransformPoint(ctLocalUnderCursor_Before);
        //    - 포인터가 가리키는 월드 좌표(그대로)
        Vector3 worldTarget = worldOnVP;
        //    - 그 차이만큼 content.position을 반대로 이동 → 포인터 아래 고정
        Vector3 worldDelta = worldOfThatLocal_After - worldTarget;
        _ct.position -= worldDelta;

        // 5) 경계/중앙 보정
        ClampAndCenterIfSmaller();
        UpdateBounds();
    }

    // ---------- Math / Clamp ----------
    void SetScale(float s)
    {
        _scale = s;
        if (_ct) _ct.localScale = Vector3.one * _scale;
    }

    Vector2 ViewportLocalToContentLocal(Vector2 vpLocal)
    {
        return (vpLocal - _ct.anchoredPosition) + (_ct.rect.size * 0.5f);
    }

    void ClampAndCenterIfSmaller()
    {
        if (!_vp || !_ct) return;
        Vector2 vp = _vp.rect.size, ct = _ct.rect.size;
        Vector2 pos = _ct.anchoredPosition;
        Vector2 halfVP = vp * 0.5f, halfCT = ct * 0.5f;

        if (_centerEff)
        {
            pos.x = (halfCT.x <= halfVP.x) ? 0f : Mathf.Clamp(pos.x, -(halfCT.x - halfVP.x), +(halfCT.x - halfVP.x));
            pos.y = (halfCT.y <= halfVP.y) ? 0f : Mathf.Clamp(pos.y, -(halfCT.y - halfVP.y), +(halfCT.y - halfVP.y));
        }
        else
        {
            float limitX = Mathf.Max(0, halfCT.x - halfVP.x);
            float limitY = Mathf.Max(0, halfCT.y - halfVP.y);
            pos.x = Mathf.Clamp(pos.x, -limitX, +limitX);
            pos.y = Mathf.Clamp(pos.y, -limitY, +limitY);
        }

        _ct.anchoredPosition = pos;
    }
}