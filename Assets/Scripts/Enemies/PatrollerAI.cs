using UnityEngine;

// Setup: Layer "Enemy" + Rigidbody2D (Kinematic, Freeze Rotation) + Collider2D.
// Hijos vacíos PuntoA y PuntoB definen el recorrido de patrulla.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PatrollerAI : MonoBehaviour
{
    [Header("Patrulla")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;
    [SerializeField] private float     velocidadPatrulla    = 2f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 4f;
    [SerializeField] private float radioDeteccion        = 5f;
    [Tooltip("Radio al que el jugador debe alejarse para que el enemigo abandone la persecución.")]
    [SerializeField] private float radioAbandonar        = 7f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxAlerta;

    private enum Estado { Patrulla, Persecucion, Regreso }
    private Estado estado = Estado.Patrulla;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Transform      player;
    private Transform      objetivoPatrulla;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null) player = go.transform;

        objetivoPatrulla = puntoA;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        switch (estado)
        {
            case Estado.Patrulla:
                Patrullar();
                if (dist <= radioDeteccion)
                {
                    AudioManager.instance?.FxSoundEffect(sfxAlerta, transform, 1f);
                    estado = Estado.Persecucion;
                }
                break;

            case Estado.Persecucion:
                Perseguir();
                if (dist > radioAbandonar)
                    estado = Estado.Regreso;
                break;

            case Estado.Regreso:
                Regresar();
                if (dist <= radioDeteccion)
                {
                    AudioManager.instance?.FxSoundEffect(sfxAlerta, transform, 1f);
                    estado = Estado.Persecucion;
                }
                break;
        }
    }

    // ── Comportamientos ───────────────────────────────────────────

    private void Patrullar()
    {
        MoverHacia(objetivoPatrulla.position, velocidadPatrulla);

        if (Vector2.Distance(transform.position, objetivoPatrulla.position) < 0.1f)
            objetivoPatrulla = objetivoPatrulla == puntoA ? puntoB : puntoA;
    }

    private void Perseguir()
    {
        MoverHacia(player.position, velocidadPersecucion);
    }

    private void Regresar()
    {
        MoverHacia(objetivoPatrulla.position, velocidadPatrulla);

        if (Vector2.Distance(transform.position, objetivoPatrulla.position) < 0.1f)
            estado = Estado.Patrulla;
    }

    // ── Movimiento ────────────────────────────────────────────────

    private void MoverHacia(Vector3 destino, float velocidad)
    {
        Vector2 dir = ((Vector2)destino - rb.position).normalized;
        rb.MovePosition(rb.position + dir * velocidad * Time.fixedDeltaTime);

        if (Mathf.Abs(dir.x) > 0.01f)
            sr.flipX = dir.x < 0;
    }

    // ── Contacto con jugador ──────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }

    // ── Gizmos ────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        // Radio de detección (rojo) y abandono (naranja)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.12f);
        Gizmos.DrawWireSphere(transform.position, radioAbandonar);

        // Ruta de patrulla (amarillo)
        if (puntoA == null || puntoB == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(puntoA.position, puntoB.position);
        Gizmos.DrawWireSphere(puntoA.position, 0.2f);
        Gizmos.DrawWireSphere(puntoB.position, 0.2f);
    }
}
