using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddMapItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private Button addButton;

    private C_M_Map mapData;

    public System.Action<C_M_Map> OnClickAdd;

    public void Setup(C_M_Map map, int index, int totalCount, Action<C_M_Map> onClickAdd)
    {
        mapData = map;
        OnClickAdd = onClickAdd;

        // map name
        string localizedName = map.F_name;

        if (LocManager.Instance != null)
        {
            localizedName = LocManager.Instance.Get(map.F_nameKey);
        }

        mapNameText.text = localizedName;

        // (1 / 10)
        indexText.text = $"({index} / {totalCount})";

        // ICON 로드
        icon.sprite = null; // 로딩 전 초기화

        if (!string.IsNullOrEmpty(map.F_iconAddrKey))
        {
            _ = LoadIconAsync(map.F_iconAddrKey);
        }

        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(() =>
        {
            OnClickAdd?.Invoke(mapData);
        });
    }

    private async Task LoadIconAsync(string addrKey)
    {
        // IconCacheManager로 Addressable Sprite 로드
        var sprite = await IconCacheManager.Instance.GetIconAsync(addrKey);

        if (sprite != null)
        {
            icon.sprite = sprite;
        }
    }
}
