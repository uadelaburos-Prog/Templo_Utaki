using System.Collections;
using UnityEngine;

// Componente genérico para activar/desactivar paredes, terreno o trampas mediante
// eventos (Lever.alActivar/alDesactivar, SecuenciaEventos, placas de presión, etc.).
// Habilita o deshabilita los Collider2D y Renderer del objeto y sus hijos, con opción
// de animación de aparición deslizándose desde abajo (terreno que "emerge").
//
// Setup: colocar en la raíz de la pared/terreno. Dejar los arrays vacíos para que tome
// automáticamente todos los Collider2D/Renderer del GO y sus hijos.
public class ObjetoActivable : MonoBehaviour
{
    [Header("Estado")]
    [Tooltip("Estado al cargar la escena. Si está desactivado, arranca oculto y sin colisión.")]
    [SerializeField] private bool activoInicial = false;

    [Header("Objetivo (vacío = este GO y sus hijos)")]
    [SerializeField] private Collider2D[] colliders;
    [SerializeField] private Renderer[]   renderers;

    [Header("Animación de aparición")]
    [Tooltip("Si está activo, al activarse se desliza hasta su posición en lugar de aparecer de golpe.")]
    [SerializeField] private bool    animarAparicion   = false;
    [Tooltip("Offset (u) desde el que emerge: (0,-1.5) abajo · (1.5,0) izquierda · (-1.5,0) derecha · (0,1.5) arriba.")]
    [SerializeField] private Vector2 offsetAparicion   = new Vector2(0f, -1.5f);
    [SerializeField] private float   duracionAnimacion = 0.35f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxActivar;
    [SerializeField] private AudioClip sfxDesactivar;

    private Vector3   posicionFinal;
    private bool      activo;
    private Coroutine rutinaAnim;

    public bool Activo => activo;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider2D>(true);
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        posicionFinal = transform.localPosition;
    }

    private void Start()
    {
        AplicarEstadoInmediato(activoInicial);
    }

    // ── PÚBLICOS ──────────────────────────────────────────────────

    public void Activar()
    {
        if (activo) return;
        activo = true;

        AudioManager.instance?.FxSoundEffect(sfxActivar, transform, 1f);

        if (animarAparicion && Application.isPlaying)
        {
            if (rutinaAnim != null) StopCoroutine(rutinaAnim);
            rutinaAnim = StartCoroutine(RutinaAparecer());
        }
        else
        {
            AplicarEstadoInmediato(true);
        }
    }

    public void Desactivar()
    {
        if (!activo) return;
        activo = false;

        AudioManager.instance?.FxSoundEffect(sfxDesactivar, transform, 1f);

        if (rutinaAnim != null) { StopCoroutine(rutinaAnim); rutinaAnim = null; }
        AplicarEstadoInmediato(false);
    }

    public void Alternar()
    {
        if (activo) Desactivar();
        else        Activar();
    }

    // ── PRIVADOS ──────────────────────────────────────────────────

    private void AplicarEstadoInmediato(bool estado)
    {
        activo                  = estado;
        transform.localPosition = posicionFinal;
        SetColliders(estado);
        SetRenderers(estado);
    }

    private IEnumerator RutinaAparecer()
    {
        SetColliders(true);
        SetRenderers(true);

        Vector3 inicio = posicionFinal + (Vector3)offsetAparicion;
        float   t      = 0f;

        while (t < duracionAnimacion)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionAnimacion);
            transform.localPosition = Vector3.Lerp(inicio, posicionFinal, k);
            yield return null;
        }

        transform.localPosition = posicionFinal;
        rutinaAnim              = null;
    }

    private void SetColliders(bool estado)
    {
        if (colliders == null) return;
        foreach (var c in colliders) if (c != null) c.enabled = estado;
    }

    private void SetRenderers(bool estado)
    {
        if (renderers == null) return;
        foreach (var r in renderers) if (r != null) r.enabled = estado;
    }
}
