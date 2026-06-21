using System.Collections;
using TMPro;
using UnityEngine;

// Colocar en un GO hijo del checkpoint o zona de tutorial.
// Requiere un componente TextMeshPro (no UGUI) en el mismo GO.
// El texto aparece en el mundo, con fade-in y fade-out.
[RequireComponent(typeof(TextMeshPro))]
public class WorldTextNotification : MonoBehaviour
{
    [Header("Tiempos")]
    [SerializeField] private float tiempoFadeIn  = 0.25f;
    [SerializeField] private float tiempoFadeOut = 0.40f;

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int    sortingOrder     = 100;

    private TextMeshPro _tmp;
    private Coroutine   _rutinaActiva;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();

        var r = GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sortingLayerName = sortingLayerName;
            r.sortingOrder     = sortingOrder;
        }

        // Empieza invisible
        Color c = _tmp.color;
        c.a       = 0f;
        _tmp.color = c;
    }

    // ── PÚBLICO ───────────────────────────────────────────────────

    public void Mostrar(string texto, float duracion = 2f)
    {
        if (_rutinaActiva != null) StopCoroutine(_rutinaActiva);
        _tmp.text     = texto;
        _rutinaActiva = StartCoroutine(RutinaMostrar(duracion));
    }

    // ── PRIVADOS ──────────────────────────────────────────────────

    private IEnumerator RutinaMostrar(float duracion)
    {
        yield return StartCoroutine(Fade(0f, 1f, tiempoFadeIn));
        yield return new WaitForSeconds(duracion);
        yield return StartCoroutine(Fade(1f, 0f, tiempoFadeOut));
        _rutinaActiva = null;
    }

    private IEnumerator Fade(float desde, float hasta, float tiempo)
    {
        float t = 0f;
        Color c = _tmp.color;
        while (t < tiempo)
        {
            t    += Time.unscaledDeltaTime;
            c.a   = Mathf.Lerp(desde, hasta, t / tiempo);
            _tmp.color = c;
            yield return null;
        }
        c.a        = hasta;
        _tmp.color = c;
    }
}
