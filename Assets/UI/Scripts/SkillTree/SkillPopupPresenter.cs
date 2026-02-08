using UnityEngine;

public class SkillPopupPresenter : MonoBehaviour
{
    [SerializeField] private RectTransform popupRoot; // 팝업 패널 루트
    [SerializeField] private TMPro.TextMeshProUGUI title;
    [SerializeField] private TMPro.TextMeshProUGUI desc;

    public void ShowForNode(SkillNodeView node, Vector2 screenPos)
    {
        // 내용 채우기 (여기서 DB/설명 불러오면 됨)
        if (title) title.text = node.name;
        if (desc) desc.text = $"NodeId: {node.NodeId}\n상태: {node.State}";

        // 위치 잡기 (Canvas가 Screen Space - Overlay 가정)
        popupRoot.gameObject.SetActive(true);
        popupRoot.position = screenPos;
        // 필요 시 화면 밖이면 반대쪽으로 밀기 등 보정
    }

    public void Hide() => popupRoot.gameObject.SetActive(false);
}
