using UnityEngine;
using UnityEngine.EventSystems;

public class PauseButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler
{
    public enum ButtonType
    {
        Continue,
        Options,
        Exit
    }

    [SerializeField] private PauseMenuVisual menu;
    [SerializeField] private ButtonType buttonType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        switch (buttonType)
        {
            case ButtonType.Continue:
                menu.HoverContinue();
                break;

            case ButtonType.Options:
                menu.HoverOptions();
                break;

            case ButtonType.Exit:
                menu.HoverExit();
                break;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        switch (buttonType)
        {
            case ButtonType.Continue:
                menu.PressContinue();
                break;

            case ButtonType.Options:
                menu.PressOptions();
                break;

            case ButtonType.Exit:
                menu.PressExit();
                break;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Entró al botón");
        menu.ResetVisual();
    }
}