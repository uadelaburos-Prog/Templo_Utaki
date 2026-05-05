using System.Collections;
using UnityEngine;

// Setup: Layer "Enemy" + Rigidbody2D (Kinematic, Freeze Rotation) + Collider2D.
// Hijos vacíos PuntoA y PuntoB definen el recorrido de patrulla.
// Feedback "!": crear hijo con SpriteRenderer (sprite "!") posicionado sobre la cabeza → asignar a iconoAlerta.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PatrollerAI : MonoBehaviour
{
    [Header("Patrulla")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;
    [SerializeField] private float     velocidadPatrulla = 2f;
    [Tooltip("Pausa en cada extremo de la ruta (segundos).")]
    [SerializeField] private float     duracionIdle      = 0.6f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 4f;
    [SerializeField] private float velocidadRegreso     = 3f;
    [SerializeField] private float radioDeteccion        = 5f;
    [Tooltip("Radio al que el jugador debe alejarse para que el enemigo abandone la persecución.")]
    [SerializeField] private float radioAbandonar        = 7f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxAlerta;

    [Header("Feedback Visual")]
    [Tooltip("Hijo con SpriteRenderer '!' posicionado sobre la cabeza del enemigo.")]
    [SerializeField] private GameObject iconoAlerta;

    private enum Estado { Idle, Patrulla, Persecucion, Regreso }
    private Estado estado    = Estado.Patrulla;
    private float  _idleTimer;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Transform      player;
    private Transform      objetivoPatrulla;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (iconoAlerta != null) iconoAlerta.SetActive(false);
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
            case Estado.Idle:
                _idleTimer -= Time.deltaTime;
                if (dist <= radioDeteccion)
                    TransicionAPersecucion();
                else if (_idleTimer <= 0f)
                    estado = Estado.Patrulla;
                break;

            case Estado.Patrulla:
                Patrullar();
                if (dist <= radioDeteccion)
                    TransicionAPersecucion();
                break;

            case Estado.Persecucion:
                Perseguir();
                if (dist > radioAbandonar)
                {
                    estado = Estado.Regreso;
                    OcultarIconoAlerta();
                }
                break;

            case Estado.Regreso:
                Regresar();
                if (dist <= radioDeteccion)
                    TransicionAPersecucion();
                break;
        }
    }

    private void TransicionAPersecucion()
    {
        AudioManager.instance?.FxSoundEffect(sfxAlerta, transform, 1f);
        estado = Estado.Persecucion;
        MostrarIconoAlerta();
    }

    private void MostrarIconoAlerta()
    {
        if (iconoAlerta == null) return;
        StopCoroutine(nameof(PopIcono));
        StartCoroutine(nameof(PopIcono));
    }

    private void OcultarIconoAlerta()
    {
        if (iconoAlerta == null) return;
        StopCoroutine(nameof(PopIcono));
        iconoAlerta.SetActive(false);
    }

    private IEnumerator PopIcono()
    {
        iconoAlerta.SetActive(true);
        iconoAlerta.transform.localScale = Vector3.zero;

        float t = 0f;
        const float popDuration = 0.12f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            iconoAlerta.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / popDuration);
            yield return null;
        }
        iconoAlerta.transform.localScale = Vector3.one;
        // queda visible hasta que OcultarIconoAlerta() lo desactive
    }

    // ── Comportamientos ───────────────────────────────────────────

    private void Patrullar()
    {
        MoverHacia(objetivoPatrulla.position, velocidadPatrulla);

        if (Vector2.Distance(transform.position, objetivoPatrulla.position) < 0.1f)
        {
            objetivoPatrulla = objetivoPatrulla == puntoA ? puntoB : puntoA;
            _idleTimer       = duracionIdle;
            estado           = Estado.Idle;
        }
    }

    private void Perseguir()
    {
        MoverHacia(player.position, velocidadPersecucion);
    }

    private void Regresar()
    {
        MoverHacia(objetivoPatrulla.position, velocidadRegreso);

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
