using UnityEngine;
using UnityEngine.EventSystems;

public class NodeView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private NodeType nodeType;
    [SerializeField] private string nodeId;   // BGDatabase row ID ¶Ç´Â key

    public NodeType Type => nodeType;
    public string NodeId => nodeId;

    public void OnPointerClick(PointerEventData eventData)
    {
        NodeInteractionController.Instance.OnNodeClicked(this);
    }
}
