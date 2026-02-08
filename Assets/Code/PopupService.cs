using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupService : MonoBehaviour
{
    static PopupService _inst;
    public static PopupService Instance
    {
        get
        {
            if (_inst) return _inst;
            var go = new GameObject("[PopupService]");
            _inst = go.AddComponent<PopupService>();
            DontDestroyOnLoad(go);
            return _inst;
        }
    }
    public static bool HasInstance => _inst != null;

    readonly Dictionary<string, PopupHost> _hosts = new Dictionary<string, PopupHost>();

    public void RegisterHost(PopupHost host)
    {
        if (!host) return;
        _hosts[host.GetInstanceID().ToString()] = host;
    }
    public void UnregisterHost(PopupHost host)
    {
        if (!host) return;
        _hosts.Remove(host.GetInstanceID().ToString());
    }

    public void Open(PopupType type, RectTransform anchor)
    {
        // 1) Host 찾기
        PopupHost host = anchor ? anchor.GetComponentInParent<PopupHost>(true) : null;
        if (!host) { Debug.LogWarning("PopupHost를 찾을 수 없습니다."); return; }

        // 2) 프리팹 찾기
        var prefab = host.GetPrefab(type);
        if (!prefab) { Debug.LogWarning($"[{host.contextId}] {type} 매핑된 프리팹이 없습니다."); return; }

        // 3) 팝업 인스턴스 생성 (먼저 생성해야 go 변수가 생김)
        GameObject go = GameObject.Instantiate(prefab, host.popupLayer);
        RectTransform root = go.transform as RectTransform;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();

        // 4) Dim 생성 (go 변수 이후에 사용 가능)
        Image dim = null;
        if (host.useDim)
        {
            GameObject dimGO = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(ModalDim), typeof(CanvasGroup));
            dim = dimGO.GetComponent<Image>();
            RectTransform rt = dim.transform as RectTransform;
            dim.transform.SetParent(host.popupLayer, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            dim.color = new Color(0, 0, 0, host.dimAlpha);
            dim.raycastTarget = true;

            var dimCg = dimGO.GetComponent<CanvasGroup>();
            dimCg.interactable = true;
            dimCg.blocksRaycasts = true;

            // Dim 클릭 시 팝업 닫기 (프레임 끝에서 닫히게)
            ModalDim modal = dimGO.GetComponent<ModalDim>();
            modal.onRequestClose = () => Close(go, dim, host.outSeconds);
        }

        if (dim) dim.transform.SetSiblingIndex(0); // Dim을 맨 아래로
        root.SetAsLastSibling();                   // 팝업을 맨 위로 (가장 앞)

        // 5) 팝업 크기 제한
        Vector2 size = root.sizeDelta;
        size.x = Mathf.Clamp(size.x, host.minSize.x, host.maxSize.x);
        size.y = Mathf.Clamp(size.y, host.minSize.y, host.maxSize.y);
        root.sizeDelta = size;

        // 6) 위치 계산
        Vector2 target = host.popupLayer.rect.center;
        if (anchor)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                host.popupLayer,
                RectTransformUtility.WorldToScreenPoint(null, anchor.position),
                null, out target);
            target += host.popupOffset;
        }
        root.anchoredPosition = target;

        // 7) 화면 클램프
        if (host.clampToViewport)
        {
            Vector4 pad = host.viewportPadding;
            Rect r = host.popupLayer.rect;
            Vector2 half = root.sizeDelta * 0.5f;
            Vector2 min = new Vector2(r.xMin + pad.x + half.x, r.yMin + pad.w + half.y);
            Vector2 max = new Vector2(r.xMax - pad.z - half.x, r.yMax - pad.y - half.y);
            Vector2 p = root.anchoredPosition;
            p.x = Mathf.Clamp(p.x, min.x, max.x);
            p.y = Mathf.Clamp(p.y, min.y, max.y);
            root.anchoredPosition = p;
        }

        // 8) 인 애니메이션
        StartCoroutine(FadeScale(cg, host.inSeconds, true));
    }


    public void Close(GameObject popupGO, Image dim = null, float outSeconds = 0.1f)
    {
        if (!popupGO) return;
        var cg = popupGO.GetComponent<CanvasGroup>();
        StartCoroutine(FadeOutAndDestroy(popupGO, cg, dim, outSeconds));
    }

    IEnumerator FadeScale(CanvasGroup cg, float seconds, bool opening)
    {
        if (!cg) yield break;
        float t = 0f;
        var tr = cg.transform;
        float fromA = opening ? 0f : 1f, toA = opening ? 1f : 0f;
        float fromS = opening ? 0.92f : 1f, toS = opening ? 1f : 0.92f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = seconds > 0 ? Mathf.Clamp01(t / seconds) : 1f;
            cg.alpha = Mathf.Lerp(fromA, toA, k);
            tr.localScale = Vector3.one * Mathf.Lerp(fromS, toS, k);
            yield return null;
        }
        cg.alpha = toA; tr.localScale = Vector3.one * toS;
    }

    IEnumerator FadeOutAndDestroy(GameObject go, CanvasGroup cg, Image dim, float seconds)
    {
        yield return FadeScale(cg, seconds, false);
        if (dim) Destroy(dim.gameObject);
        Destroy(go);
    }
}
