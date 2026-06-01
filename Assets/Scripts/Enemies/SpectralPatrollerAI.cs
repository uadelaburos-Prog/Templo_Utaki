using System.Collections;
using UnityEngine;

// Setup:
//   - Layer "Enemy" + Tag "SpectralEnemy"
//   - Rigidbody2D: Kinematic, gravityScale=0, Freeze Rotation
//   - Collider2D: Is Trigger = true (atraviesa geometría normal)
//   - SpectralWallMask: asignar layer "SpectralWall" en Inspector
//   - Hijos: PuntoA, PuntoB (recorrido), iconoAlerta (SpriteRenderer "!")
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SpectralPatrollerAI : MonoBehaviour
{
    [Header("Patrulla")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;
    [SerializeField] private float velocidadPatrulla = 2f;
    [Tooltip("Pausa en cada extremo de la ruta (segundos).")]
    [SerializeField] private float duracionIdle = 0.6f;

    [Header("Detección")]
    [SerializeField] private float radioDeteccion = 5f;
    [Tooltip("Radio al que el jugador debe alejarse para que el Espectral abandone la persecución.")]
    [SerializeField] private float radioAbandonar = 7.5f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 4f;
    [SerializeField] private float velocidadRegreso     = 3f;

    [Header("SpectralWall")]
    [Tooltip("Layer que bloquea físicamente el movimiento del Espectral.")]
    [SerializeField] private LayerMask spectralWallMask;

    [Header("Feedback")]
    [SerializeField] private GameObject iconoAlerta;
    [SerializeField] private AudioClip  sfxAlerta;

    private enum Estado { Idle, Patrulla, Persecucion, Regreso, Muerto }
    private Estado _estado = Estado.Patrulla;

    private Rigidbody2D    _rb;
    private SpriteRenderer _sr;
    private Transform      _player;
    private Transform      _objetivoPatrulla;
    private float          _idleTimer;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _rb              = GetComponent<Rigidbody2D>();
        _sr              = GetComponent<SpriteRenderer>();
        _rb.bodyType     = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        if (iconoAlerta != null) iconoAlerta.SetActive(false);
    }

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null) _player = go.transform;
        _objetivoPatrulla = puntoA;
    }

    private void Update()
    {
        if (_estado == Estado.Muerto || _player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        switch (_estado)
        {
            case Estado.Idle:
                _idleTimer -= Time.deltaTime;
                if (dist <= radioDeteccion) TransicionPersecucion();
                else if (_idleTimer <= 0f)  _estado = Estado.Patrulla;
                break;

            case Estado.Patrulla:
                Patrullar();
                if (dist <= radioDeteccion) TransicionPersecucion();
                break;

            case Estado.Persecucion:
                Perseguir();
                if (dist > radioAbandonar)
                {
                    _estado = Estado.Regreso;
                    OcultarIconoAlerta();
                }
                break;

            case Estado.Regreso:
                Regresar();
                if (dist <= radioDeteccion) TransicionPersecucion();
                break;
        }
    }

    // ── Transiciones ──────────────────────────────────────────────

    private void TransicionPersecucion()
    {
        AudioManager.instance?.FxSoundEffect(sfxAlerta, transform, 1f);
        _estado = Estado.Persecucion;
        MostrarIconoAlerta();
    }

    // ── Comportamientos ───────────────────────────────────────────

    private void Patrullar()
    {
        MoverHacia(_objetivoPatrulla.position, velocidadPatrulla);
        if (Vector2.Distance(transform.position, _objetivoPatrulla.position) < 0.1f)
        {
            _objetivoPatrulla = _objetivoPatrulla == puntoA ? puntoB : puntoA;
            _idleTimer        = duracionIdle;
            _estado           = Estado.Idle;
        }
    }

    private void Perseguir() => MoverHacia(_player.position, velocidadPersecucion);

    private void Regresar()
    {
        MoverHacia(_objetivoPatrulla.position, velocidadRegreso);
        if (Vector2.Distance(transform.position, _objetivoPatrulla.position) < 0.1f)
            _estado = Estado.Patrulla;
    }

    // ── Movimiento ────────────────────────────────────────────────

    private void MoverHacia(Vector3 destino, float velocidad)
    {
        Vector2 dir  = ((Vector2)destino - _rb.position).normalized;
        float   paso = velocidad * Time.fixedDeltaTime;

        // SpectralWall bloquea el avance — el Espectral respeta estas superficies
        if (Physics2D.Raycast(_rb.position, dir, paso + 0.15f, spectralWallMask))
            return;

        _rb.MovePosition(_rb.position + dir * paso);

        if (Mathf.Abs(dir.x) > 0.01f)
            _sr.flipX = dir.x < 0;
    }

    // ── Muerte (llamado por HazardFire) ───────────────────────────

    public void Morir()
    {
        if (_estado == Estado.Muerto) return;
        _estado = Estado.Muerto;
        OcultarIconoAlerta();
        // TODO: reemplazar con corrutina de desintegración cuando Arte entregue sprite
        Destroy(gameObject);
    }

    // ── Contacto ──────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }

    // ── Feedback visual ───────────────────────────────────────────

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
        const float duracion = 0.12f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            iconoAlerta.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / duracion);
            yield return null;
        }
        iconoAlerta.transform.localScale = Vector3.one;
    }

    // ── Gizmos ────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radioAbandonar);

        if (puntoA == null || puntoB == null) return;
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.8f);
        Gizmos.DrawLine(puntoA.position, puntoB.position);
        Gizmos.DrawWireSphere(puntoA.position, 0.2f);
        Gizmos.DrawWireSphere(puntoB.position, 0.2f);
    }
}
