using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocText : MonoBehaviour
{
    [SerializeField] private string locKey;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (LocManager.Instance != null)
        {
            LocManager.Instance.OnLanguageChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (LocManager.Instance != null)
            LocManager.Instance.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        _text.text = LocManager.Instance.Get(locKey);
    }
}
