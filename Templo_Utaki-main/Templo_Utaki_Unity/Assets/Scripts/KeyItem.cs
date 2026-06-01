using UnityEngine;

// Setup:
//   - Tag "Key" + Layer "Hookeable" (misma que Grappleable — GrappleScript la puede enganchar)
//   - Rigidbody2D: Dynamic, gravityScale=1, Freeze Rotation Z
//   - Collider2D: CircleCollider2D sólido (física y rebote) — isTrigger=false
//   - playerMask: asignar Layer "Player" en Inspector
//   - Si un enemigo porta la llave, llamar Soltar() cuando ese enemigo muere
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class KeyItem : MonoBehaviour
{
    [Header("Recolección")]
    [SerializeField] private float     radioPickup = 0.8f;
    [SerializeField] private LayerMask playerMask;

    [Header("Lanzamiento")]
    [Tooltip("Velocidad con carga mínima — equivalente a minGrappleDistance/maxGrappleDistance × launchSpeed.")]
    [SerializeField] private float velocidadMin   = 6f;
    [Tooltip("Velocidad con carga máxima — igual que launchSpeed del gancho.")]
    [SerializeField] private float velocidadMax   = 20f;
    [SerializeField] private float maxCargaTiempo = 1.5f;

    [Header("Portada")]
    [Tooltip("Offset desde el centro del jugador. X se invierte automáticamente según la dirección.")]
    [SerializeField] private Vector2 offsetPortada = new Vector2(0.45f, 0.3f);

    [Header("Física")]
    [Tooltip("Amortiguación lineal cuando la llave está en el suelo — evita que se deslice.")]
    [SerializeField] private float rozamientoSuelo = 10f;

    [Header("Respawn")]
    [SerializeField] private float yVoidThreshold = -15f;

    [Header("UI Carga")]
    [Tooltip("Mismo GameObject de barra de carga que usa GrappleScript.")]
    [SerializeField] private GameObject barraRoot;
    [SerializeField] private UnityEngine.UI.Image barraImagen;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxRecoger;
    [SerializeField] private AudioClip sfxLanzar;

    public bool EsPortada => _estado == Estado.Portada;

    private enum Estado { EnSuelo, Portada, EnVuelo }
    private Estado _estado = Estado.EnSuelo;

    private Rigidbody2D    _rb;
    private Collider2D     _col;
    private GrappleScript  _grapple;
    private Transform      _player;
    private SpriteRenderer _playerSr;
    private Vector2        _posOriginal;
    private float         _chargeTimer;
    private bool          _cargando;
    private float         _cooldownPickup;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        _posOriginal = transform.position;
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            _player   = playerGo.transform;
            _grapple  = playerGo.GetComponent<GrappleScript>();
            _playerSr = playerGo.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (_rb.position.y < yVoidThreshold)
        {
            Respawn();
            return;
        }

        if (_cooldownPickup > 0f)
        {
            _cooldownPickup -= Time.deltaTime;
            return;
        }

        switch (_estado)
        {
            case Estado.EnSuelo:
            case Estado.EnVuelo:
                if (Physics2D.OverlapCircle(transform.position, radioPickup, playerMask) != null)
                    Recoger();
                break;

            case Estado.Portada:
                ActualizarPortada();
                break;
        }
    }

    private void FixedUpdate()
    {
        // Amortiguación alta en suelo para evitar deslizamiento; sin damping en vuelo
        _rb.linearDamping = _estado == Estado.EnSuelo ? rozamientoSuelo : 0f;

        if (_estado == Estado.Portada && _player != null)
        {
            // Invertir X según la dirección del sprite del jugador
            float signoX  = (_playerSr != null && _playerSr.flipX) ? -1f : 1f;
            Vector2 offset = new Vector2(offsetPortada.x * signoX, offsetPortada.y);
            _rb.MovePosition((Vector2)_player.position + offset);
        }
    }

    // ── Portada ───────────────────────────────────────────────────

    private void ActualizarPortada()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _cargando    = true;
            _chargeTimer = 0f;
        }

        if (_cargando && Input.GetMouseButton(0))
            _chargeTimer = Mathf.Min(_chargeTimer + Time.deltaTime, maxCargaTiempo);

        if (barraRoot   != null) barraRoot.SetActive(_cargando);
        if (barraImagen != null) barraImagen.fillAmount = _cargando ? _chargeTimer / maxCargaTiempo : 0f;

        if (_cargando && Input.GetMouseButtonUp(0))
        {
            _cargando = false;
            if (barraRoot != null) barraRoot.SetActive(false);
            Lanzar(_chargeTimer / maxCargaTiempo);
        }
    }

    // ── Transiciones ──────────────────────────────────────────────

    private void Recoger()
    {
        _estado            = Estado.Portada;
        _rb.bodyType       = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _col.enabled       = false;

        if (_grapple != null)
        {
            _grapple.ForceIdle();
            _grapple.enabled = false;
        }

        AudioManager.instance?.FxSoundEffect(sfxRecoger, transform, 1f);
    }

    private void Lanzar(float fraccionCarga)
    {
        _estado            = Estado.EnVuelo;
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _col.enabled       = true;
        _cooldownPickup    = 0.3f;

        if (_grapple != null) _grapple.enabled = true;

        // Dirección desde el centro del jugador — igual que GrappleScript usa transform.position como origen
        Vector2 origen    = _player != null ? (Vector2)_player.position : _rb.position;
        Vector2 dirMundo  = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - origen).normalized;
        float   velocidad = Mathf.Lerp(velocidadMin, velocidadMax, fraccionCarga);
        _rb.linearVelocity = dirMundo * velocidad;

        AudioManager.instance?.FxSoundEffect(sfxLanzar, transform, 1f);
    }

    private void Respawn()
    {
        _cargando          = false;
        _estado            = Estado.EnSuelo;
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
        _col.enabled       = true;
        transform.position = _posOriginal;

        if (_grapple != null && !_grapple.enabled)
            _grapple.enabled = true;

        if (barraRoot != null) barraRoot.SetActive(false);
    }

    // ── API pública ───────────────────────────────────────────────

    // Llamado por KeyDoor al abrir — consume la llave
    public void Consumir()
    {
        if (_grapple != null) _grapple.enabled = true;
        gameObject.SetActive(false);
    }

    // Llamado por el enemigo portador al morir — suelta la llave con física
    public void Soltar()
    {
        if (_estado != Estado.Portada) return;
        _estado            = Estado.EnVuelo;
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _col.enabled       = true;
        _cooldownPickup    = 0.4f;
        if (_grapple != null) _grapple.enabled = true;
    }

    // ── Gizmos ────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radioPickup);
    }
}
