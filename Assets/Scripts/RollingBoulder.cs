using UnityEngine;

// Roca redonda estilo Indiana Jones (FallingRock del GDD, post-MVP adelantado).
// Permanece dormida (rb.simulated = false) hasta Activar(): cae por gravedad y luego
// rueda en 'direccion' hacia la ruta de escape del jugador. Contacto con el jugador =
// reinicio. También aplasta a la Momia (friendly fire), coherente con las trampas.
//
// Setup: Rigidbody2D (Dynamic) + CircleCollider2D sólido (no trigger). Un PhysicsMaterial2D
// con poca fricción ayuda a que ruede limpio. Layer recomendado: Obstacle o Hazard.
[RequireComponent(typeof(Rigidbody2D))]
public class RollingBoulder : MonoBehaviour
{
    [Header("Rodado")]
    [Tooltip("Dirección horizontal de rodado: -1 izquierda, +1 derecha.")]
    [SerializeField] private int   direccion       = -1;
    [Tooltip("Velocidad horizontal objetivo al rodar (u/s).")]
    [SerializeField] private float velocidadRodado = 6f;
    [Tooltip("Aceleración hacia la velocidad objetivo (u/s²).")]
    [SerializeField] private float aceleracion     = 12f;

    [Header("Activación")]
    [Tooltip("Si arranca activa al cargar la escena (solo debug). Normalmente se activa por evento.")]
    [SerializeField] private bool  activaInicial         = false;
    [Tooltip("Segundos hasta autodestruirse tras activarse. 0 = no se destruye.")]
    [SerializeField] private float tiempoVidaTrasActivar = 0f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxRuptura;
    [SerializeField] private AudioClip sfxRodando;

    private Rigidbody2D      rb;
    private CircleCollider2D circ;
    private bool             activa;
    private float            radio = 0.5f;

    public bool Activa => activa;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        circ = GetComponent<CircleCollider2D>();
        if (circ != null)
            radio = circ.radius * Mathf.Max(Mathf.Abs(transform.localScale.x), 0.01f);
    }

    private void Start()
    {
        rb.simulated = activaInicial;
        activa       = activaInicial;
        if (activaInicial) IniciarRodado();
    }

    private void FixedUpdate()
    {
        if (!activa) return;

        // Acelera la componente horizontal hacia la velocidad objetivo; conserva la vertical (gravedad).
        float objetivoX = Mathf.Sign(direccion) * velocidadRodado;
        float nuevaX    = Mathf.MoveTowards(rb.linearVelocity.x, objetivoX, aceleracion * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(nuevaX, rb.linearVelocity.y);

        // Rueda visualmente acorde a la velocidad horizontal (ω = v / r).
        if (radio > 0.001f)
            rb.angularVelocity = -nuevaX / radio * Mathf.Rad2Deg;
    }

    // ── PÚBLICOS ──────────────────────────────────────────────────

    public void Activar()
    {
        if (activa) return;
        activa       = true;
        rb.simulated = true;
        IniciarRodado();

        if (tiempoVidaTrasActivar > 0f)
            Destroy(gameObject, tiempoVidaTrasActivar);
    }

    // ── PRIVADOS ──────────────────────────────────────────────────

    private void IniciarRodado()
    {
        AudioManager.instance?.FxSoundEffect(sfxRuptura, transform, 1f);
        if (sfxRodando != null)
            AudioManager.instance?.FxSoundEffect(sfxRodando, transform, 1f);
    }

    private void Aplastar(Collider2D other)
    {
        if (!activa) return;

        if (other.CompareTag("Player"))
        {
            GameLoopManager.Instance?.PlayerDied();
            return;
        }

        if (other.CompareTag("MummyEnemy"))
        {
            var momia = other.GetComponent<MummyAI>();
            if (momia != null) momia.Morir();
        }
    }

    // ── EVENT HANDLERS ────────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D col) => Aplastar(col.collider);
    private void OnCollisionStay2D(Collision2D col)  => Aplastar(col.collider);

    // ── GIZMOS ────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 dir  = Vector3.right * Mathf.Sign(direccion);
        Gizmos.DrawLine(transform.position, transform.position + dir * 2f);
        Gizmos.DrawWireSphere(transform.position + dir * 2f, 0.15f);
    }
}
