using UnityEngine;

// Colocar en un GameObject con BoxCollider2D (isTrigger: true) y tag "Checkpoint"
// El jugador debe tener el tag "Player"
[RequireComponent(typeof(Collider2D))]
public class CheckpointZone : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private Animator  _animator;
    [SerializeField] private AudioClip _sfxActivacion;

    private static readonly int _paramActivado = Animator.StringToHash("Activado");

    private bool _activado;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Start()
    {
        // Restaurar estado visual si este fue el último checkpoint activado
        if (GameLoopManager.Instance != null && GameLoopManager.Instance.EsEsteCheckpoint(transform.position))
        {
            _activado = true;
            SetEstadoVisual(conEfectos: false);
        }
    }

    // ── EVENT HANDLERS ────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_activado) return;
        if (!other.CompareTag("Player")) return;

        _activado = true;
        GameLoopManager.Instance?.GuardarCheckpoint(transform.position);
        SetEstadoVisual(conEfectos: true);
    }

    // ── MÉTODOS PRIVADOS ──────────────────────────────────────────

    private void SetEstadoVisual(bool conEfectos)
    {
        if (_animator != null)
            _animator.SetBool(_paramActivado, true);

        if (!conEfectos) return;

        AudioManager.instance?.FxSoundEffect(_sfxActivacion, transform, 1f);
    }
}
