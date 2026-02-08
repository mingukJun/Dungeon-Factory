using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PopupHost : MonoBehaviour
{
    [Header("식별 (로그용)")]
    public string contextId = "Default";

    [Header("레이어 (없으면 자동 생성)")]
    public RectTransform popupLayer;

    [Header("스타일")]
    public Vector4 viewportPadding = new(16, 16, 16, 16); // L T R B
    public Vector2 popupOffset = new(12, 12);
    public Vector2 minSize = new(260, 140);
    public Vector2 maxSize = new(520, 480);
    public bool clampToViewport = true;
    public bool useDim = true;
    [Range(0, 0.95f)] public float dimAlpha = 0.5f;
    [Range(0, 0.5f)] public float inSeconds = 0.12f;
    [Range(0, 0.5f)] public float outSeconds = 0.10f;

    [Serializable]
    public class Mapping { public PopupType type; public GameObject prefab; }
    [Header("팝업 매핑 (이 Host 전용)")]
    public List<Mapping> mappings = new();

    void Awake()
    {
        if (!popupLayer)
        {
            var go = new GameObject("PopupLayer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            popupLayer = go.GetComponent<RectTransform>();
            popupLayer.SetParent(transform, false);
            popupLayer.anchorMin = Vector2.zero;
            popupLayer.anchorMax = Vector2.one;
            popupLayer.offsetMin = popupLayer.offsetMax = Vector2.zero;
        }

        // 최상단 정렬 (모달 전용 캔버스)
        var cv = popupLayer.GetComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 5000; // 충분히 높은 값(필요 시 조절)

        PopupService.Instance.RegisterHost(this);
    }


    public GameObject GetPrefab(PopupType type)
    {
        var m = mappings.Find(x => x.type == type);
        if (m == null || m.prefab == null) return null;
        return m.prefab;
    }


    void OnDestroy()
    {
        if (PopupService.HasInstance) PopupService.Instance.UnregisterHost(this);
    }
}
