using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PopupButton : MonoBehaviour
{
    public PopupType popupType = PopupType.None;
    public RectTransform anchor; // 비우면 자기 transform

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            var a = anchor ? anchor : transform as RectTransform;
            PopupService.Instance.Open(popupType, a);
        });
    }
}
