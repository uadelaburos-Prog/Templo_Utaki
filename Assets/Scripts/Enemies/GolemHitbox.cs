using UnityEngine;

// Hitbox de daño del Golem. Un Collider2D (Trigger) que mata al jugador al contacto.
// El GolemBoss la enciende/apaga según la ventana de cada ataque:
//   · Cuerpo del Golem  → activa durante todo el combate (contacto = reinicio, GDD).
//   · Martillo / aterrizaje → activa solo durante la ventana de impacto del ataque.
// Setup: hijo del Golem, ubicado y dimensionado en el editor para matchear la animación.
// Empieza desactivada.
[RequireComponent(typeof(Collider2D))]
public class GolemHitbox : MonoBehaviour
{
    [Header("Visual (opcional)")]
    [Tooltip("SpriteRenderer de debug/impacto. Se enciende junto con la hitbox si está asignado.")]
    [SerializeField] private SpriteRenderer visual;

    [Header("Estado inicial")]
    [SerializeField] private bool activaAlInicio = false;

    private Collider2D _col;
    private bool       _activa;

    public bool Activa => _activa;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        SetActiva(activaAlInicio);
    }

    // Enciende o apaga la hitbox. Llamado por GolemBoss al abrir/cerrar la ventana de daño.
    public void SetActiva(bool estado)
    {
        _activa      = estado;
        _col.enabled = estado;
        if (visual != null) visual.enabled = estado;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_activa) return;
        if (other.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Cubre el caso de que el jugador ya esté dentro cuando la hitbox se enciende
        // (p. ej. el aterrizaje del Golem cae sobre el jugador quieto).
        if (!_activa) return;
        if (other.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }
}
