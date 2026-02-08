using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum OrthoRoute { HorizontalThenVertical, VerticalThenHorizontal }
public enum LineState { Inactive, Available, Active, Highlight }
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UILineBetween : MonoBehaviour
{
    // ─────────────── 연결 대상 ───────────────
    [TitleGroup("Connection", "라인 연결 대상", Alignment = TitleAlignments.Centered)]
    [HorizontalGroup("Connection/Split", Width = 0.5f)]
    [Required, LabelText("From (시작)")] public RectTransform from;

    [HorizontalGroup("Connection/Split", Width = 0.5f)]
    [Required, LabelText("To (끝)")] public RectTransform to;

    [LabelText("Container (좌표 기준)")]
    public RectTransform container;

    // ─────────────── 라인 설정 ───────────────
    [TitleGroup("Line Settings", "라인 스타일", Alignment = TitleAlignments.Centered)]
    [LabelText("경로 방향")] public OrthoRoute route = OrthoRoute.HorizontalThenVertical;
    [MinValue(1), LabelText("두께(px)")] public float thickness = 8f;
    [LabelText("시작 패딩(px)")] public float startPadding = 0f;
    [LabelText("끝 패딩(px)")] public float endPadding = 0f;
    [LabelText("코너 겹침(px) - 틈 방지")] public float jointOverlap = 1f;
    [LabelText("픽셀 스냅(권장)")] public bool pixelSnap = true;

    // ─────────────── 색상/상태 ───────────────
    [TitleGroup("Color States", "상태별 색상")]
    [LabelText("비활성")] public Color inactive = new (1, 1, 1, 0.25f);
    [LabelText("가능")] public Color available = new (1f, 0.7f, 0.2f, 1f);
    [LabelText("활성")] public Color active = new (0.2f, 1f, 0.4f, 1f);
    [LabelText("강조")] public Color highlight = Color.white;

    [TitleGroup("Test (Odin)")]
    [EnumToggleButtons, LabelText("현재 상태"), OnValueChanged(nameof(ApplyStateNow))]
    public LineState current = LineState.Inactive;

#if UNITY_EDITOR
    [Button(ButtonSizes.Large), GUIColor(0.2f, 0.8f, 1f)]
    [LabelText("현재 선택을 From 으로")]
    void AssignFrom()
    {
        if (Selection.activeTransform is RectTransform rt) from = rt;
        ApplyStateNow();
    }

    [Button(ButtonSizes.Large), GUIColor(0.2f, 1f, 0.5f)]
    [LabelText("현재 선택을 To 으로")]
    void AssignTo()
    {
        if (Selection.activeTransform is RectTransform rt) to = rt;
        ApplyStateNow();
    }

    [Button, GUIColor(1f, .6f, .2f)]
    [LabelText("위치/색 강제 갱신")]
    void ApplyStateNow()
    {
        SetState(current);
        LateUpdate();
        EditorUtility.SetDirty(this);
    }
#endif

    // ─────────────── 내부 변수 ───────────────
    RectTransform _rt;
    RectTransform _hSeg, _vSeg;
    Image _hImg, _vImg;
    Canvas _cachedCanvas;

    void Awake() => Ensure();
    void OnEnable() => Ensure();

    void Ensure()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (container == null && transform.parent is RectTransform p) container = p;

        if (_hSeg == null) _hSeg = CreateOrGet("HSeg", ref _hImg);
        if (_vSeg == null) _vSeg = CreateOrGet("VSeg", ref _vImg);

        ApplyColor(GetColor(current));
    }

    RectTransform CreateOrGet(string name, ref Image img)
    {
        var t = transform.Find(name) as RectTransform;
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            t = go.GetComponent<RectTransform>();
            t.SetParent(transform, false);
        }
        img = t.GetComponent<Image>();
        img.raycastTarget = false;
        if (img.sprite == null)
            img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        return t;
    }

    // ─────────────── 라인 그리기 ───────────────
    void LateUpdate()
    {
        if (from == null || to == null) return;
        Ensure();

        Vector2 A = WorldToLocal(from, container);
        Vector2 B = WorldToLocal(to, container);
        Vector2 C = (route == OrthoRoute.HorizontalThenVertical)
            ? new Vector2(B.x, A.y)
            : new Vector2(A.x, B.y);

        Vector2 A1 = A, B2 = B, C1 = C, C2 = C;

        if (route == OrthoRoute.HorizontalThenVertical)
        {
            float dx = Mathf.Sign(C.x - A.x);
            float dy = Mathf.Sign(B.y - C.y);

            A1.x += dx * startPadding;
            B2.y -= dy * endPadding;

            // 코너에서 서로 겹치기 (틈 방지)
            C1.x -= dx * jointOverlap * 0.5f;
            C2.y += dy * jointOverlap * 0.5f;

            DrawHorizontal(A1, C1, _hSeg);
            DrawVertical(C2, B2, _vSeg);
        }
        else
        {
            float dy = Mathf.Sign(C.y - A.y);
            float dx = Mathf.Sign(B.x - C.x);

            A1.y += dy * startPadding;
            B2.x -= dx * endPadding;

            C1.y -= dy * jointOverlap * 0.5f;
            C2.x += dx * jointOverlap * 0.5f;

            DrawVertical(A1, C1, _vSeg);
            DrawHorizontal(C2, B2, _hSeg);
        }

        // 부모 좌표 초기화
        _rt.SetParent(container, false);
        _rt.anchoredPosition = Vector2.zero;
        _rt.sizeDelta = Vector2.zero;
        _rt.localRotation = Quaternion.identity;
    }

    void DrawHorizontal(Vector2 p0, Vector2 p1, RectTransform seg)
    {
        float len = Mathf.Abs(p1.x - p0.x);
        Vector2 mid = new ((p0.x + p1.x) * 0.5f, p0.y);
        seg.anchoredPosition = Snap(mid);
        seg.sizeDelta = Snap(new Vector2(Mathf.Max(0f, len), thickness));
        seg.localRotation = Quaternion.identity;
    }

    void DrawVertical(Vector2 p0, Vector2 p1, RectTransform seg)
    {
        float len = Mathf.Abs(p1.y - p0.y);
        Vector2 mid = new (p0.x, (p0.y + p1.y) * 0.5f);
        seg.anchoredPosition = Snap(mid);
        seg.sizeDelta = Snap(new Vector2(thickness, Mathf.Max(0f, len)));
        seg.localRotation = Quaternion.identity;
    }

    // ─────────────── 상태/색상 ───────────────
    public void SetState(LineState s)
    {
        current = s;
        ApplyColor(GetColor(s));
    }

    Color GetColor(LineState s)
    {
        return s switch
        {
            LineState.Available => available,
            LineState.Active => active,
            LineState.Highlight => highlight,
            _ => inactive,
        };
    }

    void ApplyColor(Color c)
    {
        if (_hImg) _hImg.color = c;
        if (_vImg) _vImg.color = c;
    }

    // ─────────────── 도우미 ───────────────
    static Vector2 WorldToLocal(RectTransform t, RectTransform space)
    {
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, t.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(space, sp, null, out var lp);
        return lp;
    }

    float Scale()
    {
        if (_cachedCanvas == null) _cachedCanvas = GetComponentInParent<Canvas>();
        return _cachedCanvas ? _cachedCanvas.scaleFactor : 1f;
    }

    Vector2 Snap(Vector2 v)
    {
        if (!pixelSnap) return v;
        float s = Scale();
        if (s <= 0f) return v;
        return new Vector2(Mathf.Round(v.x * s) / s, Mathf.Round(v.y * s) / s);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Ensure();
        SetState(current);
        LateUpdate();
    }

    // 선택한 RectTransform 2개로 자동 생성
    [MenuItem("GameObject/UI/Ortho Line (Odin)", false, 10)]
    static void CreateFromTwoSelected()
    {
        if (Selection.transforms.Length < 2 ||
            !(Selection.transforms[0] is RectTransform) ||
            !(Selection.transforms[1] is RectTransform))
        {
            EditorUtility.DisplayDialog("Create Ortho Line",
                "RectTransform 2개를 선택해 주세요.", "OK");
            return;
        }

        var a = Selection.transforms[0] as RectTransform;
        var b = Selection.transforms[1] as RectTransform;

        RectTransform container = FindCommonParent(a, b) ?? a.parent as RectTransform;
        if (container == null)
        {
            EditorUtility.DisplayDialog("Create Ortho Line",
                "두 대상의 공통 부모(또는 첫 대상의 부모)가 RectTransform이 아닙니다.", "OK");
            return;
        }

        var go = new GameObject("OrthoLine", typeof(RectTransform), typeof(UILineBetween));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(container, false);

        var comp = go.GetComponent<UILineBetween>();
        comp.container = container;
        comp.from = a;
        comp.to = b;
        comp.SetState(LineState.Inactive);

        Selection.activeObject = go;
        EditorUtility.SetDirty(go);
    }

    static RectTransform FindCommonParent(RectTransform a, RectTransform b)
    {
        Transform t = a;
        while (t != null)
        {
            if (IsAncestorOf(t, b)) return t as RectTransform;
            t = t.parent;
        }
        return null;
    }

    static bool IsAncestorOf(Transform ancestor, Transform child)
    {
        Transform t = child;
        while (t != null)
        {
            if (t == ancestor) return true;
            t = t.parent;
        }
        return false;
    }
#endif
}