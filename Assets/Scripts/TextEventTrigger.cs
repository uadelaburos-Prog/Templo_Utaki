using UnityEngine;

// Colocar en un GO con Collider2D (isTrigger: true).
// Crear un GO hijo con TextMeshPro + WorldTextNotification para el texto en mundo.
// Al entrar el jugador (tag "Player"), muestra el mensaje configurado.
[RequireComponent(typeof(Collider2D))]
public class TextEventTrigger : MonoBehaviour
{
    [Header("Mensaje")]
    [SerializeField] private string mensaje    = "Mantén clic izquierdo para cargar el gancho";
    [SerializeField] private float  duracion   = 3f;
    [SerializeField] private bool   soloUnaVez = true;

    [Header("Notificación")]
    [SerializeField] private WorldTextNotification notificacion;

    [Header("Audio (opcional)")]
    [SerializeField] private AudioClip sfxNotificacion;

    private bool _yaActivado;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Start()
    {
        // Fallback: busca automáticamente en hijos si no se asignó en Inspector
        if (notificacion == null)
            notificacion = GetComponentInChildren<WorldTextNotification>();
    }

    // ── EVENT HANDLERS ────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_yaActivado && soloUnaVez) return;
        if (!other.CompareTag("Player")) return;

        _yaActivado = true;
        notificacion?.Mostrar(mensaje, duracion);

        if (sfxNotificacion != null)
            AudioManager.instance?.FxSoundEffect(sfxNotificacion, transform, 1f);
    }
}
