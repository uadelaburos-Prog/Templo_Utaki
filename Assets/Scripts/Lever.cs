using UnityEngine;
using UnityEngine.Events;

// Setup:
//   - Layer "Hookeable" (mismo layer que ReactiveWall — GrappleScript lo detecta en hookMask)
//   - Tag "Hookable"
//   - Rigidbody2D: Dynamic, Constraints → FreezeAll (no se mueve con física, solo recibe callbacks)
//   - Collider2D sólido
//
// Interacción: el jugador lanza el gancho a la palanca → presiona S para jalar → toggle.
// Vincular alActivar / alDesactivar en el Inspector con los métodos del objeto objetivo.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour, IHookable
{
    [Header("Estado")]
    [Tooltip("Estado inicial al cargar la escena.")]
    [SerializeField] private bool estadoInicial = false;

    [Header("Eventos")]
    [Tooltip("Se invoca cuando la palanca pasa a estado ON.")]
    [SerializeField] private UnityEvent alActivar;
    [Tooltip("Se invoca cuando la palanca pasa a estado OFF.")]
    [SerializeField] private UnityEvent alDesactivar;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private Sprite         spriteOff;
    [SerializeField] private Sprite         spriteOn;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxPalanca;

    private Rigidbody2D _rb;
    private Transform   _player;
    private bool        _activado;
    private bool        _enganchado;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        _rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null) _player = go.transform;

        _activado = estadoInicial;
        ActualizarVisual();
    }

    private void Update()
    {
        if (_enganchado && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
            Toggle();
    }

    // ── IHookable ─────────────────────────────────────────────────

    public void OnHooked()   => _enganchado = true;
    public void OnReleased() => _enganchado = false;

    // ── PRIVADOS ──────────────────────────────────────────────────

    private void Toggle()
    {
        _activado = !_activado;

        if (_activado) alActivar?.Invoke();
        else           alDesactivar?.Invoke();

        AudioManager.instance?.FxSoundEffect(sfxPalanca, transform, 1f);
        ActualizarVisual();

        if (_sr != null && _player != null)
            _sr.flipX = _player.position.x > transform.position.x;
    }

    private void ActualizarVisual()
    {
        if (_sr == null) return;
        _sr.sprite = _activado ? spriteOn : spriteOff;
    }

    // ── GIZMOS ────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = _activado
            ? new Color(0f, 1f, 0.4f, 0.7f)
            : new Color(1f, 0.5f, 0f, 0.7f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}
