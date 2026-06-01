/*
 * SETUP DEL PREFAB — SpectralPatrollerAI
 * ─────────────────────────────────────────────────────────────────
 * Componentes requeridos:
 *   - Rigidbody2D     : Kinematic, Gravity Scale 0, Freeze Rotation Z
 *   - Collider2D      : Is Trigger = true
 *   - SpriteRenderer
 *   - AfterimageTrail : asignar este SpriteRenderer en sourceRenderer
 *   - Animator        : parámetros Chasing (bool), Attacking (bool)
 *
 * Hijos del GameObject:
 *   - PuntoA, PuntoB : extremos de la ruta de patrulla
 *   - iconoAlerta    : SpriteRenderer "!" sobre la cabeza
 *
 * SpectralWallMask: asignar layer "SpectralWall".
 * El fantasma atraviesa toda geometría normal; solo SpectralWalls lo bloquean.
 *
 * GhostDashAI.cs puede eliminarse — este script reemplaza ambos.
 *
 * Comportamiento:
 *   Patrulla entre PuntoA/B → al detectar jugador, orbita a su alrededor
 *   (estilo Jacob & Esau en Binding of Isaac). Si el jugador permanece
 *   quieto durante playerStillTime segundos, ejecuta un dash hacia él.
 */

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AfterimageTrail))]
[RequireComponent(typeof(Animator))]
public class SpectralPatrollerAI : MonoBehaviour
{
    private enum Estado { Patrulla, Idle, Regreso, Orbita, Windup, Dash, Recover, Muerto }

    [Header("Patrulla")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;
    [SerializeField] private float     velocidadPatrulla = 2f;
    [Tooltip("Pausa en cada extremo de la ruta (segundos).")]
    [SerializeField] private float     duracionIdle      = 0.6f;

    [Header("Detección")]
    [SerializeField] private float radioDeteccion = 6f;
    [Tooltip("Radio al que el jugador debe alejarse para que el Espectral abandone la órbita.")]
    [SerializeField] private float radioAbandonar = 8.5f;

    [Header("Órbita")]
    [Tooltip("Distancia de órbita alrededor del jugador en unidades.")]
    [SerializeField] private float orbitRadius   = 3f;
    [Tooltip("Velocidad angular de la órbita en radianes por segundo.")]
    [SerializeField] private float orbitVelocity = 1.5f;
    [Tooltip("Velocidad de desplazamiento hacia el punto objetivo de órbita.")]
    [SerializeField] private float chaseSpeed    = 5f;

    [Header("Flotación")]
    [Tooltip("Frecuencia de la variación sinusoidal del radio de órbita.")]
    [SerializeField] private float floatSpeed     = 1.2f;
    [Tooltip("Amplitud de la variación sinusoidal del radio (unidades).")]
    [SerializeField] private float floatAmplitude = 0.4f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed    = 18f;
    [Tooltip("Distancia total del dash en unidades (incluyendo el overshoot tras el jugador).")]
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float cooldown     = 2f;

    [Header("Quietud del jugador")]
    [Tooltip("Velocidad mínima (u/s) por debajo de la cual el jugador se considera quieto.")]
    [SerializeField] private float playerStillThreshold = 0.5f;
    [Tooltip("Segundos quieto para disparar el dash.")]
    [SerializeField] private float playerStillTime      = 1.5f;

    [Header("SpectralWall")]
    [Tooltip("Layer que bloquea físicamente el movimiento del Espectral.")]
    [SerializeField] private LayerMask spectralWallMask;

    [Header("Feedback")]
    [SerializeField] private GameObject iconoAlerta;
    [SerializeField] private AudioClip  sfxAlerta;

    // ── Referencias ───────────────────────────────────────────────

    private Rigidbody2D     _rb;
    private SpriteRenderer  _sr;
    private Animator        _animator;
    private AfterimageTrail _trail;
    private Transform       _player;
    private Transform       _objetivoPatrulla;

    // ── Estado ────────────────────────────────────────────────────

    private Estado  _estado = Estado.Patrulla;
    private float   _stateTimer;
    private float   _cooldownTimer;
    private float   _idleTimer;
    private Vector2 _dashDir;
    private Vector2 _recoilTarget;

    // ── Órbita ────────────────────────────────────────────────────

    private float     _orbitAngle;
    private Coroutine _parpadeoRoutine;
    private Color     _colorOriginal;

    // ── Quietud del jugador ───────────────────────────────────────

    private Vector3 _lastPlayerPos;
    private float   _playerStillTimer;

    private static readonly int ParamChasing   = Animator.StringToHash("Chasing");
    private static readonly int ParamAttacking = Animator.StringToHash("Attacking");
    private static readonly int ParamMoving    = Animator.StringToHash("Moving");

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _rb    = GetComponent<Rigidbody2D>();
        _sr    = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _trail = GetComponent<AfterimageTrail>();
        _colorOriginal = _sr.color;

        _rb.bodyType       = RigidbodyType2D.Kinematic;
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        // El trigger debe estar activo para que OnTriggerEnter2D detecte al jugador
        GetComponent<Collider2D>().isTrigger = true;

        if (iconoAlerta != null) iconoAlerta.SetActive(false);
    }

    private void Start()
    {
        if (puntoA == null || puntoB == null)
        {
            enabled = false;
            return;
        }

        var go = GameObject.FindWithTag("Player");
        if (go != null)
        {
            _player        = go.transform;
            _lastPlayerPos = _player.position;
        }

        _objetivoPatrulla = puntoA;
    }

    private void Update()
    {
        if (_estado == Estado.Muerto || _player == null) return;
        ActualizarQuietudJugador();
    }

    private void FixedUpdate()
    {
        if (_estado == Estado.Muerto || _player == null) return;

        _stateTimer    -= Time.fixedDeltaTime;
        _cooldownTimer -= Time.fixedDeltaTime;

        if (_animator != null)
            _animator.SetBool(ParamMoving, _estado == Estado.Patrulla || _estado == Estado.Regreso);

        switch (_estado)
        {
            case Estado.Patrulla: TickPatrulla(); break;
            case Estado.Idle:     TickIdle();     break;
            case Estado.Regreso:  TickRegreso();  break;
            case Estado.Orbita:   TickOrbita();   break;
            case Estado.Windup:   TickWindup();   break;
            case Estado.Dash:     TickDash();     break;
            case Estado.Recover:  TickRecover();  break;
        }
    }

    // ── Quietud del jugador ───────────────────────────────────────

    private void ActualizarQuietudJugador()
    {
        float vel = Vector3.Distance(_player.position, _lastPlayerPos) / Time.deltaTime;
        if (vel < playerStillThreshold)
            _playerStillTimer += Time.deltaTime;
        else
            _playerStillTimer = 0f;
        _lastPlayerPos = _player.position;
    }

    // ── Ticks de estado ───────────────────────────────────────────

    private void TickPatrulla()
    {
        if (JugadorDetectado()) { EnterOrbita(alertar: true); return; }

        MoverHacia(_objetivoPatrulla.position, velocidadPatrulla);

        if (Vector2.Distance(transform.position, _objetivoPatrulla.position) < 0.15f)
        {
            _objetivoPatrulla = _objetivoPatrulla == puntoA ? puntoB : puntoA;
            _idleTimer        = duracionIdle;
            _estado           = Estado.Idle;
        }
    }

    private void TickIdle()
    {
        if (JugadorDetectado()) { EnterOrbita(alertar: true); return; }
        _idleTimer -= Time.fixedDeltaTime;
        if (_idleTimer <= 0f) _estado = Estado.Patrulla;
    }

    private void TickRegreso()
    {
        if (JugadorDetectado()) { EnterOrbita(alertar: true); return; }

        MoverHacia(_objetivoPatrulla.position, velocidadPatrulla);
        if (Vector2.Distance(transform.position, _objetivoPatrulla.position) < 0.15f)
            _estado = Estado.Patrulla;
    }

    private void TickOrbita()
    {
        if (Vector2.Distance(transform.position, _player.position) > radioAbandonar)
        {
            EnterRegreso();
            return;
        }

        // Avanzar ángulo orbital; radio varía sinusoidalmente para feel orgánico
        _orbitAngle += orbitVelocity * Time.fixedDeltaTime;
        float r         = orbitRadius + floatAmplitude * Mathf.Sin(Time.time * floatSpeed);
        Vector2 offset  = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * r;
        MoverHacia((Vector2)_player.position + offset, chaseSpeed);

        if (_cooldownTimer <= 0f && _playerStillTimer >= playerStillTime)
            EnterWindup();
    }

    private void TickWindup()
    {
        MoverHacia(_recoilTarget, 3f);
        // MoverHacia puede pisar el flip; mantener la cara hacia el jugador durante el telegrafío
        if (_dashDir.x != 0f) _sr.flipX = _dashDir.x > 0f;
        if (_stateTimer <= 0f) EnterDash();
    }

    private void TickDash()
    {
        float paso = dashSpeed * Time.fixedDeltaTime;

        if (Physics2D.Raycast(_rb.position, _dashDir, paso + 0.2f, spectralWallMask))
        {
            EnterRecover();
            return;
        }

        _rb.MovePosition(_rb.position + _dashDir * paso);

        if (_stateTimer <= 0f) EnterRecover();
    }

    private void TickRecover()
    {
        if (_stateTimer > 0f) return;

        if (JugadorDetectado())
            EnterOrbita(alertar: false);
        else
            EnterRegreso();
    }

    // ── Transiciones ──────────────────────────────────────────────

    private void EnterOrbita(bool alertar)
    {
        _estado = Estado.Orbita;
        // Iniciar ángulo desde la posición actual del fantasma para entrada suave
        Vector2 offset = _rb.position - (Vector2)_player.position;
        _orbitAngle    = Mathf.Atan2(offset.y, offset.x);

        if (alertar)
        {
            AudioManager.instance?.FxSoundEffect(sfxAlerta, transform, 1f);
            MostrarIconoAlerta();
        }

        SetAnimator(true, false);
    }

    private void EnterRegreso()
    {
        _estado = Estado.Regreso;
        OcultarIconoAlerta();
        SetAnimator(false, false);
    }

    private void EnterWindup()
    {
        _estado       = Estado.Windup;
        _stateTimer   = 0.35f;
        _dashDir      = ((Vector2)(_player.position - transform.position)).normalized;
        _recoilTarget = _rb.position - _dashDir * 0.5f;

        if (_dashDir.x != 0f) _sr.flipX = _dashDir.x > 0f;
        SetAnimator(true, true);
        _parpadeoRoutine = StartCoroutine(ParpadeoDash());
    }

    private void EnterDash()
    {
        DetenerParpadeo();
        _estado     = Estado.Dash;
        _stateTimer = dashDistance / dashSpeed;
        _trail.StartTrail();
    }

    private void EnterRecover()
    {
        DetenerParpadeo();
        _estado           = Estado.Recover;
        _stateTimer       = 0.4f;
        _cooldownTimer    = cooldown;
        _playerStillTimer = 0f;
        _trail.StopTrail();
        SetAnimator(false, false);
    }

    // ── Muerte ────────────────────────────────────────────────────

    public void Morir()
    {
        if (_estado == Estado.Muerto) return;
        _estado = Estado.Muerto;
        _trail.StopTrail();
        OcultarIconoAlerta();
        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private bool JugadorDetectado() =>
        Vector2.Distance(transform.position, _player.position) <= radioDeteccion;

    private void MoverHacia(Vector3 destino, float velocidad)
    {
        Vector2 toDestino = (Vector2)destino - _rb.position;
        float   distancia = toDestino.magnitude;

        if (distancia < 0.001f) return;

        Vector2 dir  = toDestino / distancia;
        float   paso = Mathf.Min(velocidad * Time.fixedDeltaTime, distancia);

        if (Physics2D.Raycast(_rb.position, dir, paso + 0.15f, spectralWallMask))
            return;

        _rb.MovePosition(_rb.position + dir * paso);

        if (Mathf.Abs(dir.x) > 0.01f)
            _sr.flipX = dir.x > 0;
    }

    private void SetAnimator(bool chasing, bool attacking)
    {
        if (_animator == null) return;
        _animator.SetBool(ParamChasing,   chasing);
        _animator.SetBool(ParamAttacking, attacking);
    }

    // ── Contacto ──────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }

    // ── Feedback visual ───────────────────────────────────────────

    private void DetenerParpadeo()
    {
        if (_parpadeoRoutine == null) return;
        StopCoroutine(_parpadeoRoutine);
        _parpadeoRoutine = null;
        _sr.color   = _colorOriginal;
        _sr.enabled = true;
    }

    private IEnumerator ParpadeoDash()
    {
        while (true)
        {
            _sr.color   = Color.white;
            _sr.enabled = true;
            yield return new WaitForSeconds(0.05f);
            _sr.enabled = false;
            yield return new WaitForSeconds(0.05f);
        }
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
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radioAbandonar);
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, orbitRadius);

        if (puntoA == null || puntoB == null) return;
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.8f);
        Gizmos.DrawLine(puntoA.position, puntoB.position);
        Gizmos.DrawWireSphere(puntoA.position, 0.2f);
        Gizmos.DrawWireSphere(puntoB.position, 0.2f);
    }
}
