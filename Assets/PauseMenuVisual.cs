using UnityEngine;
using UnityEngine.UI;

public class PauseMenuVisual : MonoBehaviour
{
    [Header("Referencia")]
    [SerializeField] private Image menuImage;

    [Header("Sprite por defecto")]
    [SerializeField] private Sprite defaultSprite;

    [Header("Continuar")]
    [SerializeField] private Sprite continueHover;
    [SerializeField] private Sprite continuePressed;

    [Header("Opciones")]
    [SerializeField] private Sprite optionsHover;
    [SerializeField] private Sprite optionsPressed;

    [Header("Salir")]
    [SerializeField] private Sprite exitHover;
    [SerializeField] private Sprite exitPressed;

    private void Awake()
    {
        ResetVisual();
    }

    public void HoverContinue()
    {
        menuImage.sprite = continueHover;
    }

    public void PressContinue()
    {
        menuImage.sprite = continuePressed;
    }

    public void HoverOptions()
    {
        menuImage.sprite = optionsHover;
    }

    public void PressOptions()
    {
        menuImage.sprite = optionsPressed;
    }

    public void HoverExit()
    {
        menuImage.sprite = exitHover;
    }

    public void PressExit()
    {
        menuImage.sprite = exitPressed;
    }

    public void ResetVisual()
    {
        menuImage.sprite = defaultSprite;
    }
}