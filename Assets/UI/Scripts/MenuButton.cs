using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour
{
    [LabelText("Target Panel"), EnumToggleButtons]
    public PanelType target;

    [Required, SceneObjectsOnly]
    public MenuController controller;

    [PropertySpace, InfoBox("오브젝트 이름을 Btn_Panel로 자동 변경", InfoMessageType.None)]
    [Button("Rename To Convention"), DisableInPlayMode]
    private void RenameByConvention() => gameObject.name = $"Btn_{target}";

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => controller.Show(target));
    }
}