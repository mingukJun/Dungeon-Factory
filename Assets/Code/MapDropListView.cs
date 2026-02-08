using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif
using Sirenix.OdinInspector;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MapDropListView : MonoBehaviour
{
    [Required] public Transform content; // Vertical Layout Group
    [AssetsOnly, Required] public AssetReferenceGameObject dropEntryPrefab; // Addressables 프리팹

    readonly List<GameObject> _pool = new();

    public void Setup(MapPopupProfile profile, MapPopupData data)
    {
        EnsurePool(data.drops.Count);
        for (int i = 0; i < _pool.Count; i++)
        {
            var go = _pool[i];
            go.SetActive(i < data.drops.Count);
            if (i >= data.drops.Count) continue;

            var entry = go.GetComponent<MapDropItemEntry>();
            var d = data.drops[i];
            entry.SetNameAndProb(d.itemName, d.probability);

            // 아이콘 Addressables 로드
            entry.SetIcon(null);
            Addressables.LoadAssetAsync<Sprite>(d.iconAddressKey).Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    entry.SetIcon(op.Result);
            };
        }
    }

    void EnsurePool(int need)
    {
        // 부족분만큼 인스턴스
        while (_pool.Count < need)
        {
            var op = dropEntryPrefab.InstantiateAsync(content);
            op.Completed += (h) =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    h.Result.SetActive(true);
                    _pool.Add(h.Result);
                }
            };
        }
    }
}

public class MapDropItemEntry : MonoBehaviour
{
    public Image icon;
#if TMP_PRESENT
    public TextMeshProUGUI nameText, probText;
#else
    public Text nameText, probText;
#endif

    public void SetNameAndProb(string name, float p01)
    {
        nameText.text = name;
        probText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(p01) * 100f)}%";
    }
    public void SetIcon(Sprite sp) { if (icon) icon.sprite = sp; }
}
