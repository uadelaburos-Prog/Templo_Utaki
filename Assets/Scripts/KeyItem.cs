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
    [Tooltip("Tiempo (s) tras lanzar o soltar la llave durante el cual NO puede volver a recogerse — evita la re-recolección instantánea.")]
    [SerializeField] private float     cooldownRecoger = 0.5f;

    [Header("Lanzamiento")]
    [Tooltip("Velocidad con carga mínima — equivalente a minGrappleDistance/maxGrappleDistance × launchSpeed.")]
    [SerializeField] private float velocidadMin   = 6f;
    [Tooltip("Velocidad con carga máxima — igual que launchSpeed del gancho.")]
    [SerializeField] private float velocidadMax   = 20f;
    [SerializeField] private float maxCargaTiempo = 1.5f;

    [Header("Portada")]
    [Tooltip("Offset desde el centro visual del sprite del jugador. Y negativo = más abajo (cintura), positivo = más arriba (hombros). X se invierte según la dirección.")]
    [SerializeField] private Vector2 offsetPortada = new Vector2(0.3f, -0.1f);

    [Header("Física")]
    [Tooltip("Desaceleración horizontal (u/s²) en suelo — evita deslizamiento sin frenar la caída. Más alto = se detiene antes.")]
    [SerializeField] private float rozamientoSuelo = 80f;

    [Header("UI Carga")]
    [Tooltip("Opcional. Si se deja vacío, se hereda automáticamente la barra de carga del GrappleScript del jugador.")]
    [SerializeField] private GameObject barraRoot;
    [SerializeField] private UnityEngine.UI.Image barraImagen;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxRecoger;
    [SerializeField] private AudioClip sfxLanzar;

    // Solo cuenta como "portada" para la puerta cuando la lleva el JUGADOR.
    // Mientras la porta un enemigo el estado también es Portada, pero no debe abrir puertas.
    public bool EsPortada => _estado == Estado.Portada && _portadaPorJugador;

    private enum Estado { EnSuelo, Portada, EnVuelo }
    private Estado _estado = Estado.EnSuelo;

    private Rigidbody2D    _rb;
    private Collider2D     _col;
    private GrappleScript  _grapple;
    private Transform      _player;
    private SpriteRenderer _playerSr;
    private float         _chargeTimer;
    private bool          _cargando;
    private float         _cooldownPickup;
    private bool          _portadaPorJugador;   // true solo mientras la lleva el jugador (habilita apertura de puertas)
    private Vector2       _posicionOrigen;
    private Quaternion    _rotacionOrigen;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        // Lugar de origen: al caer al void, la llave reaparece aquí en vez de destruirse
        _posicionOrigen = transform.position;
        _rotacionOrigen = transform.rotation;

        // Sin fricción: el frenado en suelo lo gestiona rozamientoSuelo por código, y sobre
        // una MovingPlatform evita que el arrastre físico se sume al teletransporte (doble empuje)
        _col.sharedMaterial = new PhysicsMaterial2D("LlaveSinFriccion") { friction = 0f, bounciness = 0f };
        _col.enabled = false;   // reactivar fuerza a que el collider tome el material nuevo
        _col.enabled = true;
    }

    private void Start()
    {
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            _player   = playerGo.transform;
            _grapple  = playerGo.GetComponent<GrappleScript>();
            _playerSr = playerGo.GetComponent<SpriteRenderer>();

            // La llave nunca colisiona físicamente con el jugador (lo atraviesa al
            // lanzarla hacia atrás); el pickup usa OverlapCircle, no esta colisión
            foreach (var pc in playerGo.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(_col, pc, true);

            // Heredar la barra de carga del jugador si no se asignó por Inspector
            if (_grapple != null)
            {
                if (barraRoot   == null) barraRoot   = _grapple.ChargeBarRoot;
                if (barraImagen == null) barraImagen = _grapple.ChargeBarImage;
            }
        }
    }

    private void Update()
    {
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
        // Frenar solo el deslizamiento horizontal en suelo — la gravedad sigue
        // tirando libre en Y (linearDamping global frenaría también la caída)
        if (_estado == Estado.EnSuelo)
        {
            Vector2 v = _rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, 0f, rozamientoSuelo * Time.fixedDeltaTime);
            _rb.linearVelocity = v;
        }

        if (_estado == Estado.Portada && _player != null)
        {
            // Usar el centro visual del sprite como origen — independiente del pivot del transform
            Vector2 centroJugador = _playerSr != null
                ? (Vector2)_playerSr.bounds.center
                : (Vector2)_player.position;

            float   signoX = (_playerSr != null && _playerSr.flipX) ? -1f : 1f;
            Vector2 offset = new Vector2(offsetPortada.x * signoX, offsetPortada.y);
            _rb.MovePosition(centroJugador + offset);
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
        _portadaPorJugador = true;
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
        _portadaPorJugador = false;
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _col.enabled       = true;
        _cooldownPickup    = cooldownRecoger;

        if (_grapple != null) _grapple.enabled = true;

        // Dirección desde el centro del jugador — igual que GrappleScript usa transform.position como origen
        Vector2 origen    = _player != null ? (Vector2)_player.position : _rb.position;
        Vector2 dirMundo  = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - origen).normalized;
        float   velocidad = Mathf.Lerp(velocidadMin, velocidadMax, fraccionCarga);
        _rb.linearVelocity = dirMundo * velocidad;

        AudioManager.instance?.FxSoundEffect(sfxLanzar, transform, 1f);
    }

    // ── API pública ───────────────────────────────────────────────

    // Llamado por VoidScript cuando la llave cae al vacío — en vez de destruirse,
    // reaparece en su lugar de origen para no bloquear el nivel
    public void MorirPorVoid()
    {
        // Si la llave estaba siendo portada, devolver el control del gancho al jugador
        if (_estado == Estado.Portada && _grapple != null)
            _grapple.enabled = true;

        if (barraRoot != null) barraRoot.SetActive(false);
        _cargando = false;

        // Reaparece limpia en el origen, lista para caer y asentarse
        enabled             = true;   // reactiva Update si la portaba un enemigo
        _portadaPorJugador  = false;
        transform.position  = _posicionOrigen;
        transform.rotation  = _rotacionOrigen;
        _estado             = Estado.EnVuelo;
        _rb.bodyType        = RigidbodyType2D.Dynamic;
        _rb.linearVelocity  = Vector2.zero;
        _rb.angularVelocity = 0f;
        _col.enabled        = true;
        _cooldownPickup     = cooldownRecoger;
    }

    // Llamado por KeyDoor al abrir — consume la llave
    public void Consumir()
    {
        _portadaPorJugador = false;
        if (_grapple != null) _grapple.enabled = true;
        gameObject.SetActive(false);
    }

    // Llamado por KeyCarrier en Start() — congela la llave mientras la porta el enemigo
    public void IniciarPortadaEnemigo()
    {
        _estado            = Estado.Portada;
        _portadaPorJugador = false;   // la porta un enemigo, NO el jugador → no abre puertas
        _rb.bodyType       = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _col.enabled       = false;
        enabled            = false;   // suspende Update: sin pickup ni input de ratón
    }

    // Llamado por el enemigo portador al morir — suelta la llave con física
    public void Soltar()
    {
        if (_estado != Estado.Portada) return;
        _estado            = Estado.EnVuelo;
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _col.enabled       = true;
        _cooldownPickup    = cooldownRecoger;
        if (_grapple != null) _grapple.enabled = true;
    }

    // ── Gizmos ────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radioPickup);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Solo evaluar si la llave está en vuelo
        if (_estado != Estado.EnVuelo) return;

        // Verificar si el objeto pertenece a la capa Ground
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Analizar la dirección del impacto usando el primer punto de contacto
            Vector2 normal = collision.contacts[0].normal;

            // Si la normal apunta hacia arriba (Y > 0.7), es una superficie plana o rampa suave
            if (normal.y > 0.7f)
            {
                _estado = Estado.EnSuelo;
            }
        }
    }

}
