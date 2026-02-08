using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif
using Sirenix.OdinInspector;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MapPopupController : MonoBehaviour
{
    [Title("참조")]
    [Required] public CanvasGroup panelGroup;
    [Required] public Image dim;
#if TMP_PRESENT
    [Required] public TextMeshProUGUI headerTitle;
    public TextMeshProUGUI headerSub;
#else
    [Required] public Text headerTitle;
    public Text headerSub;
#endif
    [Required] public Toggle tabInfo;
    [Required] public Toggle tabDrops;
    [Required] public Toggle tabParty;

    [Required] public GameObject pageInfo;
    [Required] public GameObject pageDrops;
    [Required] public GameObject pageParty;

    [Title("하위 뷰")]
    [Required] public MapInfoView infoView;
    [Required] public MapDropListView dropListView;
    [Required] public MapPartyView partyView;

    [Title("설정")]
    public MapPopupProfile profile;
    [SerializeField, Required] MonoBehaviour repositoryBehaviour; // DummyMapRepository 등
    IMapRepository repo;

    bool _visible;
    Coroutine _animCo;

    void Awake()
    {
        repo = repositoryBehaviour as IMapRepository;
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        dim.canvasRenderer.SetAlpha(0f);

        tabInfo.onValueChanged.AddListener(OnTabInfo);
        tabDrops.onValueChanged.AddListener(OnTabDrops);
        tabParty.onValueChanged.AddListener(OnTabParty);

        ShowOnly(pageInfo);
    }

    public void Open(string mapId)
    {
        var data = repo.GetMapPopupData(mapId);
        Bind(data);

        if (_animCo != null) StopCoroutine(_animCo);
        _animCo = StartCoroutine(CoShow(true));
    }

    public void Close()
    {
        if (_animCo != null) StopCoroutine(_animCo);
        _animCo = StartCoroutine(CoShow(false));
    }

    void Bind(MapPopupData data)
    {
        headerTitle.text = data.mapName;
        if (headerSub) headerSub.text = data.subTitle;

        infoView.Setup(profile, data);
        dropListView.Setup(profile, data);
        partyView.Setup(profile, data);
    }

    IEnumerator CoShow(bool show)
    {
        _visible = show;
        float t = 0f;
        float dur = show ? profile.showAnimTime : profile.hideAnimTime;
        float fromA = panelGroup.alpha;
        float toA = show ? 1f : 0f;

        panelGroup.blocksRaycasts = true;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = dur <= 0f ? 1f : Mathf.Clamp01(t / dur);
            float a = Mathf.Lerp(fromA, toA, k);
            panelGroup.alpha = a;
            dim.canvasRenderer.SetAlpha(a * profile.dimAlpha);
            yield return null;
        }
        panelGroup.alpha = toA;
        dim.canvasRenderer.SetAlpha(toA * profile.dimAlpha);
        panelGroup.interactable = show;
        panelGroup.blocksRaycasts = show;
    }

    void ShowOnly(GameObject go)
    {
        pageInfo.SetActive(go == pageInfo);
        pageDrops.SetActive(go == pageDrops);
        pageParty.SetActive(go == pageParty);
    }

    void OnTabInfo(bool on) { if (on) ShowOnly(pageInfo); }
    void OnTabDrops(bool on) { if (on) ShowOnly(pageDrops); }
    void OnTabParty(bool on) { if (on) ShowOnly(pageParty); }

    // Dim 클릭에 연결
    public void OnClickDim() { Close(); }
}
