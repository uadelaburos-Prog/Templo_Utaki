using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Adjuntar a cada botón del menú de pausa (botones solo-texto).
[RequireComponent(typeof(RectTransform))]
public class PauseButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,  IPointerUpHandler
{
    [SerializeField] private TMP_Text etiqueta;
    [SerializeField] private Image    cursorFlecha;  // Image triángulo dorado, oculta por defecto
    [SerializeField] private bool     esPeligro;

    static readonly Color NormalTexto  = new Color32(224, 204, 170, 255); // #E0CCAA
    static readonly Color HoverTexto   = new Color32(240, 224, 128, 255); // #F0E080
    static readonly Color PeligroTexto = new Color32(224,  96,  80, 255); // #E06050

    private const float BlinkCiclo = 0.7f;
    private float blinkTimer;
    private bool  enHover;

    private void Awake()
    {
        if (etiqueta     != null) etiqueta.color   = NormalTexto;
        if (cursorFlecha != null) cursorFlecha.enabled = false;
    }

    private void Update()
    {
        if (!enHover || cursorFlecha == null) return;
        blinkTimer = (blinkTimer + Time.unscaledDeltaTime) % BlinkCiclo;
        cursorFlecha.enabled = blinkTimer < BlinkCiclo * 0.5f;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        enHover    = true;
        blinkTimer = 0f;
        if (etiqueta     != null) etiqueta.color      = esPeligro ? PeligroTexto : HoverTexto;
        if (cursorFlecha != null) cursorFlecha.enabled = true;
    }

    public void OnPointerExit(PointerEventData _)
    {
        enHover = false;
        if (etiqueta     != null) etiqueta.color      = NormalTexto;
        if (cursorFlecha != null) cursorFlecha.enabled = false;
    }

    public void OnPointerDown(PointerEventData _) => transform.localPosition += new Vector3(0, -2, 0);
    public void OnPointerUp  (PointerEventData _) => transform.localPosition += new Vector3(0,  2, 0);
}
