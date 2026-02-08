using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/CustomScrollRect Profile", fileName = "CustomScrollRectProfile")]
public class CustomScrollRectProfile : ScriptableObject
{
    [MinMaxSlider(0.1f, 10f, true)]
    public Vector2 scaleRange = new(0.5f, 3f);
    [PropertyRange(0.01f, 0.5f)] public float wheelZoomSpeed = 0.08f;
    [PropertyRange(0.001f, 0.02f)] public float pinchZoomSpeed = 0.005f;
    [PropertyRange(1.2f, 4f)] public float toggleZoomFactor = 2f;
    [PropertyRange(0.15f, 0.5f)] public float doubleTapSeconds = 0.28f;
    [PropertyRange(8f, 80f)] public float doubleTapMaxPixels = 40f;
    public bool centerWhenSmaller = true;
}