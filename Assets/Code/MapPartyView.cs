using System.Collections.Generic;
using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MapPartyView : MonoBehaviour
{
    [Required] public Transform content; // Grid/Horizontal Layout
    [AssetsOnly, Required] public AssetReferenceGameObject partySlotPrefab;

    readonly List<GameObject> _pool = new();

    public void Setup(MapPopupProfile profile, MapPopupData data)
    {
        EnsurePool(data.party.Count);
        for (int i = 0; i < _pool.Count; i++)
        {
            var go = _pool[i];
            go.SetActive(i < data.party.Count);
            if (i >= data.party.Count) continue;

            var slot = go.GetComponent<MapPartySlotEntry>();
            var p = data.party[i];
            slot.SetTexts(p.displayName, p.role, p.level);

            slot.SetPortrait(null);
            Addressables.LoadAssetAsync<Sprite>(p.portraitAddressKey).Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    slot.SetPortrait(op.Result);
            };
        }
    }

    void EnsurePool(int need)
    {
        while (_pool.Count < need)
        {
            var op = partySlotPrefab.InstantiateAsync(content);
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

public class MapPartySlotEntry : MonoBehaviour
{
    public Image portrait;
#if TMP_PRESENT
    public TextMeshProUGUI nameText, roleText, levelText;
#else
    public Text nameText, roleText, levelText;
#endif
    public void SetPortrait(Sprite sp) { if (portrait) portrait.sprite = sp; }
    public void SetTexts(string name, string role, int level)
    {
        nameText.text = name;
        roleText.text = role;
        levelText.text = $"Lv.{level}";
    }
}
