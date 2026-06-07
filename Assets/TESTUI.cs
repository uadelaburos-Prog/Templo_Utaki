using UnityEngine;
using UnityEngine.EventSystems;

public class TestUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("ENTER");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLICK");
    }
}