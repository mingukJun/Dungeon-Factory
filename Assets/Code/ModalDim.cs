using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModalDim : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public System.Action onRequestClose;

    // 다운에서 일단 이벤트 소비 (드래그/업 전파 방지)
    public void OnPointerDown(PointerEventData eventData) { /* 소비만 */ }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 같은 프레임에 뒤 UI로 전파되는 걸 막기 위해 프레임 끝에 닫기
        StartCoroutine(CloseNextFrame());
    }

    IEnumerator CloseNextFrame()
    {
        yield return new WaitForEndOfFrame();
        onRequestClose?.Invoke();
    }
}
