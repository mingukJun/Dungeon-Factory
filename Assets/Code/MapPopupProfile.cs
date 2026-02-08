using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "UI/Map/Map Popup Profile", fileName = "MapPopupProfile")]
public class MapPopupProfile : ScriptableObject
{
    [Title("레이아웃")]
    public Vector2 panelMinMaxWidth = new Vector2(680, 920);

    [Title("동작")]
    [Range(0f, 1f)] public float dimAlpha = 0.6f;
    [Range(0f, 1f)] public float showAnimTime = 0.12f;
    [Range(0f, 1f)] public float hideAnimTime = 0.10f;

    [Title("표현")]
    [Range(0f, 1f)] public float deathProbWarnThreshold = 0.35f; // 35% 이상이면 경고색
}
