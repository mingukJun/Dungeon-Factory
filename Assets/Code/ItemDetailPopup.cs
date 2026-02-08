using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class ItemDetailPopup : MonoBehaviour
{
#if TMP_PRESENT
    public TMP_Text title;
    public TMP_Text desc;
#else
    public Text title;
    public Text desc;
#endif
    public Button closeButton;

    void Awake()
    {
        if (closeButton)
            closeButton.onClick.AddListener(() => PopupService.Instance.Close(gameObject));
    }

    /// <summary>
    /// 외부에서 아이템 정보를 넘겨받을 때 호출 (선택사항)
    /// </summary>
    public void SetData(string itemName, string itemDesc)
    {
        if (title) title.text = itemName;
        if (desc) desc.text = itemDesc;
    }
}
