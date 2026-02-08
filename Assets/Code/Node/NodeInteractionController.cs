using System.Collections.Generic;
using UnityEngine;

public class NodeInteractionController : MonoBehaviour
{
    public static NodeInteractionController Instance { get; private set; }

    [SerializeField] private NodeActionPanel actionPanel; // UI 프리팹/싱글톤

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OnNodeClicked(NodeView node)
    {
        // 1) 이 노드의 상태 조회 (영웅 배치 여부, 확장 가능 맵 등)
        var actions = BuildActionsForNode(node);

        // 2) 액션 패널 열기
        actionPanel.Show(actions);
    }

    private List<NodeAction> BuildActionsForNode(NodeView node)
    {
        var list = new List<NodeAction>();

        switch (node.Type)
        {
            case NodeType.Map:
                BuildMapNodeActions(node, list);
                break;

            case NodeType.Building:
                BuildBuildingNodeActions(node, list);
                break;
        }

        return list;
    }

    // ------------------------------
    // Map Node용 액션 구성
    // ------------------------------
    private void BuildMapNodeActions(NodeView node, List<NodeAction> list)
    {
        //bool hasHero = HeroPlacementService.HasHeroOnMap(node.NodeId);
        //var expandableMaps = MapExpansionService.GetExpandableMapIds(node.NodeId);

        //if (hasHero)
        //{
        //    // 1) 영웅 관리
        //    list.Add(new NodeAction("영웅 관리", () =>
        //    {
        //        HeroUIManager.OpenHeroManageUI(node.NodeId);
        //    }));

        //    // 2) 확장 가능한 맵 버튼들
        //    foreach (var mapId in expandableMaps)
        //    {
        //        string localMapId = mapId;
        //        list.Add(new NodeAction($"맵 확장: {localMapId}", () =>
        //        {
        //            MapExpansionService.ExpandMap(localMapId);
        //        }));
        //    }
        //}
        //else
        //{
        //    // 영웅을 배치하는 버튼만
        //    list.Add(new NodeAction("영웅 배치", () =>
        //    {
        //        HeroUIManager.OpenHeroPlacementUI(node.NodeId);
        //    }));
        //}
    }

    // ------------------------------
    // Building Node용 액션 구성
    // ------------------------------
    private void BuildBuildingNodeActions(NodeView node, List<NodeAction> list)
    {
        //bool hasHero = HeroPlacementService.HasHeroOnBuilding(node.NodeId);

        //// 1) 건물 업그레이드
        //list.Add(new NodeAction("건물 업그레이드", () =>
        //{
        //    BuildingService.UpgradeBuilding(node.NodeId);
        //}));

        //// 2) 영웅 관련
        //if (hasHero)
        //{
        //    list.Add(new NodeAction("영웅 관리", () =>
        //    {
        //        HeroUIManager.OpenHeroManageUI(node.NodeId);
        //    }));
        //}
        //else
        //{
        //    list.Add(new NodeAction("영웅 배치", () =>
        //    {
        //        HeroUIManager.OpenHeroPlacementUI(node.NodeId);
        //    }));
        //}
    }
}
