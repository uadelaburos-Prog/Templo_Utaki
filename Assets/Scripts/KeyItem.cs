using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    [Header("Seguro anti-incrustación")]
    [Tooltip("Capas sólidas (paredes/suelo) que pueden atrapar la llave. Vacío = hereda la máscara de trayectoria (Ground + Platform).")]
    [SerializeField] private LayerMask maskSolidos;
    [Tooltip("Iteraciones de desincrustación al soltar dentro de una pared. Cada una empuja la llave la mínima distancia para salir por la superficie más cercana. Más = resuelve rincones/geometría gruesa.")]
    [SerializeField] private int iteracionesDesincrustacion = 8;

    [Header("UI Carga")]
    [Tooltip("Opcional. Si se deja vacío, se hereda automáticamente la barra de carga del GrappleScript del jugador.")]
    [SerializeField] private GameObject barraRoot;
    [SerializeField] private UnityEngine.UI.Image barraImagen;

    [Header("Previsualización de trayectoria")]
    [Tooltip("Muestra una parábola punteada de la trayectoria estimada mientras se carga el lanzamiento.")]
    [SerializeField] private bool  mostrarTrayectoria = true;
    [Tooltip("Cantidad máxima de puntos del punteado (pool).")]
    [SerializeField] private int   maxPuntosTrayectoria = 32;
    [Tooltip("Separación entre puntos del punteado (u).")]
    [SerializeField] private float espaciadoPunteado = 0.35f;
    [Tooltip("Diámetro de cada punto (u). El grosor es constante en toda la curva.")]
    [SerializeField] private float grosorPunteado = 0.12f;
    [SerializeField] private Color colorTrayectoria = new Color(1f, 0.25f, 0.2f, 0.8f);
    [Tooltip("Segundos de vuelo simulados por la previsualización.")]
    [SerializeField] private float tiempoTrayectoria = 1.2f;
    [Tooltip("Capas que cortan la previsualización (donde la llave chocaría). Vacío = Ground + Platform.")]
    [SerializeField] private LayerMask maskTrayectoria;
    [SerializeField] private string sortingLayerTrayectoria = "Player";

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
    private Camera         _cam;
    private SpriteRenderer[] _puntosPreview;   // pool de puntos del punteado
    private float          _radioLlave;        // radio de la hitbox (para el CircleCast)
    private float         _chargeTimer;
    private bool          _cargando;
    private float         _cooldownPickup;
    private bool          _portadaPorJugador;   // true solo mientras la lleva el jugador (habilita apertura de puertas)
    private Vector2       _posicionOrigen;
    private Quaternion    _rotacionOrigen;
    private Tilemap[]     _tilemapsSolidos;     // tilemaps con collider en capas sólidas (para desincrustar por celda)

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

        // Radio de la hitbox real de la llave — la previsualización lo usa para el CircleCast.
        // Se cachea acá porque en Portada el collider se desactiva (bounds inválidos).
        _radioLlave = _col is CircleCollider2D circ
            ? circ.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))
            : Mathf.Max(_col.bounds.extents.x, _col.bounds.extents.y);

        if (mostrarTrayectoria) CrearPunteadoTrayectoria();
    }

    private void Start()
    {
        _cam = Camera.main;

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
        // Seguro continuo anti-incrustación: mientras la llave está suelta (vuela o
        // reposa), si su centro entra en una celda sólida —porque el enemigo murió
        // incrustado o porque cayó dentro del tilemap y el CompositeCollider2D en modo
        // Outlines la atrapó ("todo actúa como suelo")— reubicarla a la celda libre más
        // cercana. Un solo chequeo al soltar no basta: puede colarse frames después.
        if (_estado == Estado.EnVuelo || _estado == Estado.EnSuelo)
        {
            if (CentroEnSolido())
                AsegurarPosicionLibre();
        }

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
        {
            _chargeTimer = Mathf.Min(_chargeTimer + Time.deltaTime, maxCargaTiempo);
            ActualizarTrayectoria();
        }

        if (barraRoot   != null) barraRoot.SetActive(_cargando);
        if (barraImagen != null) barraImagen.fillAmount = _cargando ? _chargeTimer / maxCargaTiempo : 0f;

        if (_cargando && Input.GetMouseButtonUp(0))
        {
            _cargando = false;
            if (barraRoot != null) barraRoot.SetActive(false);
            OcultarTrayectoria();
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
        OcultarTrayectoria();

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

        // Seguro: si el enemigo murió incrustado en una pared, la llave quedaría
        // atrapada e inalcanzable. Reubicarla en un hueco libre (o al origen).
        AsegurarPosicionLibre();
    }

    // Seguro anti-incrustación: si la llave quedó atrapada dentro del suelo/pared al
    // morir su portador, la reubica en el hueco libre más cercano.
    //   1) Tilemaps → consulta directa de celdas (HasTile). Funciona SIEMPRE, incluso
    //      con CompositeCollider2D en modo "Outlines", donde OverlapCircle/Distance
    //      NO detectan el interior del sólido.
    //   2) Resto de colliders (plataformas, props) → desincrustación por normal.
    private void AsegurarPosicionLibre()
    {
        DesincrustarDeTilemaps();
        DesincrustarDeColliders();

        // Detener la caída para que no vuelva a colarse tras despegarla; que se asiente
        // limpio con la gravedad desde el borde donde quedó.
        _rb.linearVelocity  = Vector2.zero;
        _rb.angularVelocity = 0f;
    }

    private LayerMask MaskSolidos()
    {
        return maskSolidos != 0
            ? maskSolidos
            : (maskTrayectoria != 0 ? maskTrayectoria : LayerMask.GetMask("Ground", "Platform"));
    }

    // Reubica la llave por consulta de celdas de tilemap (determinista, independiente
    // de la geometría del collider). Busca en anillos la celda vacía más cercana.
    private void DesincrustarDeTilemaps()
    {
        if (_tilemapsSolidos == null)
            CachearTilemapsSolidos();
        if (_tilemapsSolidos.Length == 0) return;

        Vector2 p = _rb.position;
        if (!CeldaOcupada(p)) return;   // el centro de la llave no está dentro de ningún tile

        Tilemap    refTm = _tilemapsSolidos[0];   // referencia de grilla para el barrido
        Vector3Int c0    = refTm.WorldToCell(p);

        Vector3Int mejor      = c0;
        float      mejorDist2 = Mathf.Infinity;

        // Anillos concéntricos crecientes; dentro de cada anillo elige la celda vacía
        // euclidianamente más cercana (mejor UX que un simple "primer libre").
        for (int r = 1; r <= 12; r++)
        {
            bool hallado = false;
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;   // solo el borde del anillo

                    Vector3Int c    = new Vector3Int(c0.x + dx, c0.y + dy, c0.z);
                    Vector3    wpos = refTm.GetCellCenterWorld(c);
                    if (CeldaOcupada(wpos)) continue;

                    float d2 = ((Vector2)wpos - p).sqrMagnitude;
                    if (d2 < mejorDist2) { mejorDist2 = d2; mejor = c; hallado = true; }
                }

            if (hallado)   // hueco encontrado en este anillo → es el más cercano posible
            {
                _rb.position = refTm.GetCellCenterWorld(mejor);
                return;
            }
        }
    }

    // Desincrustación por normal para colliders sólidos que NO son tilemap de contorno
    // (plataformas, cajas, props con collider relleno).
    private void DesincrustarDeColliders()
    {
        LayerMask solidos = MaskSolidos();
        float     radio   = _radioLlave * 1.05f;
        var       buffer  = new Collider2D[8];
        int       iters   = Mathf.Max(1, iteracionesDesincrustacion);

        for (int iter = 0; iter < iters; iter++)
        {
            int n = Physics2D.OverlapCircleNonAlloc(_rb.position, radio, buffer, solidos);
            if (n == 0) break;

            Vector2 empuje = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                if (buffer[i] == null || buffer[i] == _col) continue;
                // Los tilemaps de contorno no reportan solape interior — los ignora acá
                // (ya los resolvió DesincrustarDeTilemaps) y evita normales basura.
                if (buffer[i] is CompositeCollider2D || buffer[i] is TilemapCollider2D) continue;

                ColliderDistance2D d = _col.Distance(buffer[i]);
                if (d.isOverlapped) empuje += d.normal * d.distance;
            }

            if (empuje == Vector2.zero) break;
            _rb.position += empuje;
        }
    }

    // Cachea los tilemaps con collider cuyas capas están en la máscara de sólidos.
    private void CachearTilemapsSolidos()
    {
        LayerMask solidos = MaskSolidos();
        var lista = new List<Tilemap>();
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            bool enMascara = (solidos.value & (1 << tm.gameObject.layer)) != 0;
            bool tieneCol  = tm.TryGetComponent<TilemapCollider2D>(out _);
            if (enMascara && tieneCol) lista.Add(tm);
        }
        _tilemapsSolidos = lista.ToArray();
    }

    // Chequeo barato por-frame: ¿el centro de la llave está dentro de un tile sólido?
    private bool CentroEnSolido()
    {
        if (_tilemapsSolidos == null)
            CachearTilemapsSolidos();
        if (_tilemapsSolidos.Length == 0) return false;
        return CeldaOcupada(_rb.position);
    }

    // ¿La posición cae sobre una celda con tile SÓLIDO (con collider) en algún tilemap?
    // Ignora tiles decorativos sin collider (ColliderType.None) para no expulsar de más.
    private bool CeldaOcupada(Vector2 world)
    {
        foreach (var tm in _tilemapsSolidos)
        {
            Vector3Int c = tm.WorldToCell(world);
            if (tm.HasTile(c) && tm.GetColliderType(c) != Tile.ColliderType.None)
                return true;
        }
        return false;
    }

    // ── Previsualización de trayectoria ───────────────────────────

    // Crea el pool de puntos (hijos de la llave) que dibujan la parábola punteada.
    private void CrearPunteadoTrayectoria()
    {
        var root = new GameObject("TrayectoriaPreview");
        root.transform.SetParent(transform, false);

        // Compensar la escala de la llave para que el grosor de los puntos sea absoluto.
        Vector3 ls = transform.lossyScale;
        root.transform.localScale = new Vector3(
            ls.x != 0f ? 1f / ls.x : 1f,
            ls.y != 0f ? 1f / ls.y : 1f, 1f);

        Sprite punto = GenerarSpritePunto(16);
        _puntosPreview = new SpriteRenderer[Mathf.Max(2, maxPuntosTrayectoria)];
        for (int i = 0; i < _puntosPreview.Length; i++)
        {
            var go = new GameObject("Punto");
            go.transform.SetParent(root.transform, false);
            go.transform.localScale = Vector3.one * grosorPunteado;   // sprite de 1u de diámetro

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite           = punto;
            sr.color            = colorTrayectoria;
            sr.sortingLayerName = sortingLayerTrayectoria;
            sr.enabled          = false;
            _puntosPreview[i]   = sr;
        }

        // Sin máscara asignada → geometría sólida por defecto (donde la llave rebota).
        if (maskTrayectoria == 0)
            maskTrayectoria = LayerMask.GetMask("Ground", "Platform");
    }

    // Recalcula la trayectoria con la carga y el cursor actuales, simulando la física REAL
    // del Rigidbody2D paso a paso igual que Unity (Euler semi-implícito por fixedDeltaTime):
    //   v += g·gravityScale·dt · v /= (1 + linearDamping·dt) · p += v·dt
    // La curva se corta con un CircleCast del radio de la hitbox donde la llave chocaría.
    // (La masa no afecta: la velocidad se asigna directa y la gravedad es independiente.)
    private void ActualizarTrayectoria()
    {
        if (_puntosPreview == null || _player == null || _cam == null) return;

        Vector2 origen    = _player.position;
        Vector2 dirMundo  = ((Vector2)_cam.ScreenToWorldPoint(Input.mousePosition) - origen).normalized;
        float   velocidad = Mathf.Lerp(velocidadMin, velocidadMax, _chargeTimer / maxCargaTiempo);

        float   dt            = Time.fixedDeltaTime;
        Vector2 g             = Physics2D.gravity * _rb.gravityScale;
        float   factorDamping = 1f / (1f + _rb.linearDamping * dt);
        int     pasos         = Mathf.CeilToInt(tiempoTrayectoria / dt);

        Vector2 p = origen;
        Vector2 v = dirMundo * velocidad;

        int   puntoIdx     = 0;
        float distAcum     = 0f;
        float proximoPunto = espaciadoPunteado;   // el primer punto aparece a un paso del jugador

        for (int i = 0; i < pasos && puntoIdx < _puntosPreview.Length; i++)
        {
            // Integración idéntica a la del motor de física.
            v += g * dt;
            v *= factorDamping;
            Vector2 siguiente = p + v * dt;

            Vector2 seg   = siguiente - p;
            float   largo = seg.magnitude;
            if (largo <= 0.0001f) { p = siguiente; continue; }

            // La hitbox real define el choque: CircleCast con el radio del collider.
            bool corte = false;
            RaycastHit2D hit = Physics2D.CircleCast(p, _radioLlave, seg / largo, largo, maskTrayectoria);
            if (hit.collider != null)
            {
                siguiente = hit.centroid;   // centro de la llave en el momento del impacto
                largo     = (siguiente - p).magnitude;
                corte     = true;
            }

            // Emitir puntos equiespaciados dentro del tramo recorrido.
            while (proximoPunto <= distAcum + largo && puntoIdx < _puntosPreview.Length)
            {
                float f = largo > 0f ? (proximoPunto - distAcum) / largo : 0f;
                var   sr = _puntosPreview[puntoIdx++];
                sr.transform.position = Vector2.Lerp(p, siguiente, f);
                sr.enabled = true;
                proximoPunto += espaciadoPunteado;
            }

            distAcum += largo;
            p = siguiente;
            if (corte) break;
        }

        // Apagar los puntos sobrantes del pool.
        for (int i = puntoIdx; i < _puntosPreview.Length; i++)
            _puntosPreview[i].enabled = false;
    }

    private void OcultarTrayectoria()
    {
        if (_puntosPreview == null) return;
        foreach (var sr in _puntosPreview)
            if (sr != null) sr.enabled = false;
    }

    // Círculo blanco de 1u de diámetro (se tiñe con colorTrayectoria y escala con grosorPunteado).
    private Sprite GenerarSpritePunto(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float   r = size * 0.5f - 0.5f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y,
                    Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) <= r
                        ? Color.white : Color.clear);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
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
