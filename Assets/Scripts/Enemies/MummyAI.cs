/*
 * SETUP DEL PREFAB — MummyAI
 * ─────────────────────────────────────────────────────────────────
 * Componentes requeridos:
 *   - Rigidbody2D  : Dynamic, Gravity Scale 1, Freeze Rotation Z, Interpolate
 *   - Collider2D   : sólido (no trigger) — el jugador no lo atraviesa
 *   - SpriteRenderer
 *   - Animator     : parámetros Walking (bool), Chasing (bool), Dead (trigger),
 *                    VelocityY (float)
 *
 * Hijos del GameObject:
 *   - iconoAlerta : hijo con SpriteRenderer "!" sobre la cabeza (opcional)
 *
 * Tags y Layers:
 *   - Tag del GameObject : "MummyEnemy"
 *   - Layer              : "Enemy"
 *
 * Patrulla por rebote (sin waypoints):
 *   - La momia camina en dirPatrullaInicial hasta encontrar un borde o una pared
 *     demasiado alta, entonces gira y repite indefinidamente.
 *   - Si la pared puede ser superada con el salto, salta en lugar de girar.
 *   - Asignar maskSuelo con los layers de suelo del proyecto.
 *
 * Salto:
 *   - jumpClearanceHeight : altura (desde el centro) a la que se comprueba si la
 *     pared continúa. Si no hay pared a esa altura, el salto la supera.
 *   - wallCheckDistance   : alcance horizontal del raycast de pared.
 *   - fuerzaSalto         : impulso vertical.
 *   - jumpCooldown        : tiempo mínimo entre saltos consecutivos.
 *
 * Muerte:
 *   - Al recibir Morir(), el gameplay se desactiva de inmediato.
 *   - La animación "Dead" corre completa antes de que el objeto sea destruido.
 *   - Ajustar deathAnimDuration para que coincida con el clip del Animator.
 *
 * Hazards compatibles: HazardFire, SpikeHazard, VoidScript.
 */

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class MummyAI : MonoBehaviour
{
    private enum Estado { Idle, Patrulla, Persecucion, Muerto }

    [Header("Patrulla")]
    [Tooltip("Dirección inicial de patrulla. 1 = derecha, -1 = izquierda.")]
    [SerializeField, Range(-1f, 1f)] private float dirPatrullaInicial = 1f;
    [SerializeField] private float velocidadPatrulla = 2f;
    [Tooltip("Pausa breve (s) al girar.")]
    [SerializeField] private float duracionIdle      = 0.4f;

    [Header("Detección")]
    [SerializeField] private float radioDeteccion = 5f;
    [Tooltip("Radio al que el jugador debe alejarse para abandonar la persecución.")]
    [SerializeField] private float radioAbandonar = 7f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 3.5f;

    [Header("Detección de borde")]
    [Tooltip("LayerMask del suelo.")]
    [SerializeField] private LayerMask maskSuelo;
    [Tooltip("Desplazamiento horizontal hacia adelante para el origen del raycast de borde.")]
    [SerializeField] private float edgeCheckOffsetX  = 0.35f;
    [Tooltip("Desplazamiento hacia abajo desde el centro para el origen del raycast de borde.")]
    [SerializeField] private float edgeCheckOffsetY  = 0.45f;
    [Tooltip("Longitud del raycast hacia abajo para detectar suelo adelante.")]
    [SerializeField] private float edgeCheckDistance = 0.4f;

    [Header("Salto")]
    [Tooltip("Impulso vertical al saltar.")]
    [SerializeField] private float fuerzaSalto          = 8f;
    [Tooltip("Longitud del raycast horizontal para detectar paredes.")]
    [SerializeField] private float wallCheckDistance    = 0.4f;
    [Tooltip("Offset vertical desde el centro donde comienza la detección de pared (raycast bajo). " +
             "Negativo = más abajo, positivo = más arriba.")]
    [SerializeField] private float wallCheckOriginY     = 0f;
    [Tooltip("Altura desde el centro hasta donde debe estar despejado para que el salto supere la pared (raycast alto).")]
    [SerializeField] private float jumpClearanceHeight  = 1.0f;
    [Tooltip("Tiempo mínimo (s) entre saltos consecutivos.")]
    [SerializeField] private float jumpCooldown         = 0.6f;

    [Header("Muerte")]
    [Tooltip("Duración de la animación de muerte (s).")]
    [SerializeField] private float deathAnimDuration = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxAlerta;

    [Header("Feedback Visual")]
    [Tooltip("Hijo con SpriteRenderer '!' sobre la cabeza.")]
    [SerializeField] private GameObject iconoAlerta;

    // ── Animator hashes ───────────────────────────────────────────
    private static readonly int ParamWalking   = Animator.StringToHash("Walking");
    private static readonly int ParamChasing   = Animator.StringToHash("Chasing");
    private static readonly int ParamDead      = Animator.StringToHash("Dead");
    private static readonly int ParamVelocityY = Animator.StringToHash("VelocityY");

    // ── Referencias ───────────────────────────────────────────────
    private Rigidbody2D    _rb;
    private Collider2D     _col;
    private SpriteRenderer _sr;
    private Animator       _animator;
    private Transform      _player;

    // ── Estado interno ────────────────────────────────────────────
    private Estado _estado          = Estado.Patrulla;
    private float  _idleTimer;
    private float  _jumpCooldownTimer;
    private float  _dirPatrulla;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _rb       = GetComponent<Rigidbody2D>();
        _col      = GetComponent<Collider2D>();
        _sr       = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _rb.gravityScale   = 1f;
        _rb.freezeRotation = true;
        _rb.interpolation  = RigidbodyInterpolation2D.Interpolate;

        if (iconoAlerta != null) iconoAlerta.SetActive(false);
    }

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null) _player = go.transform;

        _dirPatrulla = dirPatrullaInicial >= 0f ? 1f : -1f;
    }

    private void FixedUpdate()
    {
        if (_estado == Estado.Muerto || _player == null) return;

        _jumpCooldownTimer -= Time.fixedDeltaTime;

        float dist = Vector2.Distance(transform.position, _player.position);

        ActualizarAnimator();

        switch (_estado)
        {
            case Estado.Idle:
                _idleTimer -= Time.fixedDeltaTime;
                if (dist <= radioDeteccion)
                    TransicionAPersecucion();
                else if (_idleTimer <= 0f)
                    _estado = Estado.Patrulla;
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
                    _estado = Estado.Patrulla;
                    OcultarIconoAlerta();
                }
                break;
        }
    }

    // ── Transición ────────────────────────────────────────────────

    private void TransicionAPersecucion()
    {
        AudioManager.instance?.FxSoundEffect(sfxAlerta, transform, 1f);
        _estado = Estado.Persecucion;
        MostrarIconoAlerta();
    }

    // ── Comportamientos ───────────────────────────────────────────

    private void Patrullar()
    {
        bool hayBorde     = !HaySueloAdelante(_dirPatrulla);
        bool hayPared     = HayParedAdelante(_dirPatrulla);
        bool paredSaltable = hayPared && !ParedDemasiadoAlta(_dirPatrulla);

        // Borde o pared insuperable → girar
        if (hayBorde || (hayPared && !paredSaltable))
        {
            Girar();
            return;
        }

        // Pared superable → saltar
        if (paredSaltable && Mathf.Abs(_rb.linearVelocity.y) < 0.5f && _jumpCooldownTimer <= 0f)
            Saltar();

        MoverHorizontal(velocidadPatrulla, _dirPatrulla);
    }

    private void Perseguir()
    {
        float dirX = Mathf.Sign(_player.position.x - transform.position.x);

        // Saltar si hay pared superable durante la persecución
        bool hayPared      = HayParedAdelante(dirX);
        bool paredSaltable = hayPared && !ParedDemasiadoAlta(dirX);
        if (paredSaltable && Mathf.Abs(_rb.linearVelocity.y) < 0.5f && _jumpCooldownTimer <= 0f)
            Saltar();

        MoverHorizontal(velocidadPersecucion, dirX);
    }

    // ── Movimiento ────────────────────────────────────────────────

    private void Girar()
    {
        _dirPatrulla       *= -1f;
        _rb.linearVelocity  = new Vector2(0f, _rb.linearVelocity.y);
        _idleTimer          = duracionIdle;
        _estado             = Estado.Idle;
    }

    /// <summary>Mueve la Momia horizontalmente. Durante el salto omite el edge check para mantener momentum.</summary>
    private void MoverHorizontal(float velocidad, float dirX)
    {
        bool subiendo = _rb.linearVelocity.y > 0.5f;
        if (!subiendo && !HaySueloAdelante(dirX))
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        _rb.linearVelocity = new Vector2(dirX * velocidad, _rb.linearVelocity.y);

        if (Mathf.Abs(dirX) > 0.01f)
            _sr.flipX = dirX > 0;
    }

    private void Saltar()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, fuerzaSalto);
        _jumpCooldownTimer = jumpCooldown;
    }

    // ── Raycasts ──────────────────────────────────────────────────

    private bool HaySueloAdelante(float dirX)
    {
        if (maskSuelo == 0) return true;

        Vector2 origen = _rb.position + new Vector2(dirX * edgeCheckOffsetX, -edgeCheckOffsetY);
        return Physics2D.Raycast(origen, Vector2.down, edgeCheckDistance, maskSuelo).collider != null;
    }

    private bool HayParedAdelante(float dirX)
    {
        if (maskSuelo == 0) return false;

        Vector2 origen = _rb.position + new Vector2(0f, wallCheckOriginY);
        return Physics2D.Raycast(origen, Vector2.right * dirX, wallCheckDistance, maskSuelo).collider != null;
    }

    private bool ParedDemasiadoAlta(float dirX)
    {
        if (maskSuelo == 0) return false;

        // Si hay pared también a jumpClearanceHeight sobre el centro, el salto no la supera
        Vector2 origen = _rb.position + new Vector2(0f, jumpClearanceHeight);
        return Physics2D.Raycast(origen, Vector2.right * dirX, wallCheckDistance, maskSuelo).collider != null;
    }

    // ── Muerte ────────────────────────────────────────────────────

    public void Morir()
    {
        if (_estado == Estado.Muerto) return;
        _estado = Estado.Muerto;

        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType       = RigidbodyType2D.Kinematic;
        _col.enabled       = false;
        OcultarIconoAlerta();

        _animator.SetTrigger(ParamDead);
        StartCoroutine(DestruirTrasAnimacion());
    }

    private IEnumerator DestruirTrasAnimacion()
    {
        yield return new WaitForSeconds(deathAnimDuration);
        Destroy(gameObject);
    }

    // ── Contacto ──────────────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }

    // ── Animator ──────────────────────────────────────────────────

    private void ActualizarAnimator()
    {
        if (_animator == null) return;
        _animator.SetBool (ParamWalking,   _estado == Estado.Patrulla);
        _animator.SetBool (ParamChasing,   _estado == Estado.Persecucion);
        _animator.SetFloat(ParamVelocityY, _rb.linearVelocity.y);
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
        // Radios de detección
        Gizmos.color = new Color(0.8f, 0.55f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.color = new Color(0.8f, 0.55f, 0.1f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radioAbandonar);

        // Wall check — raycast bajo (desde donde detecta) y alto (hasta donde salta)
        Vector3 posBaja = transform.position + Vector3.up * wallCheckOriginY;
        Vector3 posAlta = transform.position + Vector3.up * jumpClearanceHeight;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        Gizmos.DrawLine(posBaja, posBaja + Vector3.right * wallCheckDistance);
        Gizmos.DrawLine(posBaja, posBaja + Vector3.left  * wallCheckDistance);

        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.7f);
        Gizmos.DrawLine(posAlta, posAlta + Vector3.right * wallCheckDistance);
        Gizmos.DrawLine(posAlta, posAlta + Vector3.left  * wallCheckDistance);

        // Línea vertical entre ambos raycasts — visualiza la zona "saltable"
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.4f);
        Gizmos.DrawLine(posBaja, posAlta);
    }
}
