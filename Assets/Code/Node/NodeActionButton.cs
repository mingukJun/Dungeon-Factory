using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NodeActionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;

    private Action onClick;

    public void Setup(string label, Action onClick)
    {
        labelText.text = label;
        this.onClick = onClick;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            this.onClick?.Invoke();
            // 액션 수행 후 패널 닫고 싶으면
            NodeActionPanel.Current?.Hide();
        });
    }
}
