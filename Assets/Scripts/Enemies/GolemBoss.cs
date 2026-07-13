using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

// Jefe Final: el Golem de Piedra (Nivel 6). Máquina de estados por corrutina maestra.
//
// Ciclo: Activar() → (Aparición opcional) → Combate: repite [set de N ataques → ventana de
// vulnerabilidad]. En cada ventana el Golem queda quieto mostrando el frame de una parte
// (mano izq → mano der → cabeza → pecho) durante X segundos; el jugador engancha esa parte
// con el gancho (GolemWeakPoint, tag/layer "Hookable") y tira para romperla. Romper el pecho
// (última parte) → Muriendo → alMorir.
//
// Ataques (selección por distancia al jugador, con cooldown por ataque):
//   · cerca  → Martillo: golpea a AMBOS costados a la vez (frame 4 = golpe). El telegrafo se
//              expande hacia los costados durante el windup y, al golpear, los tiles del suelo
//              alrededor del Golem cambian a un tile peligroso (pisarlos = muerte) por un rato.
//   · media  → Proyectiles: cae en su lugar enterrándose y dispara 4 ráfagas a alturas
//              decrecientes (3 proyectiles por lado por ráfaga), con ritmo pausado, y se recompone.
//   · lejos  → Salto: salta hacia el jugador (frame 2 = auge) y se reubica donde cae.
//
// Martillo y Proyectil se ANIMAN POR CÓDIGO (frame por frame, con el Animator desactivado
// durante el ataque) para controlar el ritmo pausado. Idle, Salto, partes vulnerables, tick
// y muerte usan el Animator.
//
// Contacto con el cuerpo del Golem = reinicio (GDD), vía la hitbox 'cuerpoDanio' activa
// durante todo el combate; además matan sus habilidades (martillo, suelo peligroso,
// aterrizaje) y los proyectiles.
//
// Fases (GDD): Fase 1 = mano izq. Fase 2 (desde mano der) = mayor cadencia + evento de
// colapso de plataformas (alEntrarFase2).
//
// Setup: Rigidbody2D (Kinematic, gravityScale 0). Partes débiles como hijos (GolemWeakPoint,
// tag/layer "Hookable"). Hitboxes de habilidad como hijos (GolemHitbox).
[RequireComponent(typeof(Rigidbody2D))]
public class GolemBoss : MonoBehaviour
{
    private enum Estado { Dormido, Emergiendo, Combate, Muriendo, Muerto }
    private enum Ataque { Martillo, Proyectiles, Salto }

    // Índices de frame clave de los ataques animados por código.
    private const int FRAME_GOLPE_MARTILLO   = 4;   // Golem_Martillo_: 0-3 windup, 4 golpe
    private const int FRAME_PRIMER_DISPARO   = 6;   // Golem_Proyectil_: 0-5 entrada
    private const int FRAME_ULTIMO_PROYECTIL = 12;  // disparos en 6,8,10,12 · bajadas 7,9,11

    // Triggers del Animator para el frame vulnerable de cada parte, en orden de secuencia.
    private static readonly string[] TriggersVuln =
        { "VulnManoIzq", "VulnManoDer", "VulnCabeza", "VulnPecho" };

    [Header("Referencias")]
    [SerializeField] private Animator      animator;
    [SerializeField] private SpriteRenderer sr;
    [Tooltip("Partes débiles EN ORDEN de secuencia: [0] mano izq · [1] mano der · [2] cabeza · [3] pecho.")]
    [SerializeField] private GolemWeakPoint[] partesDebiles;
    [Tooltip("Hitbox del cuerpo. Activa durante todo el combate: contacto con el Golem = reinicio (GDD).")]
    [SerializeField] private GolemHitbox cuerpoDanio;

    [Header("Activación")]
    [Tooltip("Si está activo, la pelea arranca sola al cargar la escena (sin BossFightTrigger).")]
    [SerializeField] private bool activarAlInicio = false;

    [Header("Rangos de decisión")]
    [Tooltip("A esta distancia o menos usa Martillo.")]
    [SerializeField] private float rangoCerca = 3.5f;
    [Tooltip("A esta distancia o más usa Salto. Entre cerca y lejos usa Proyectiles.")]
    [SerializeField] private float rangoLejos = 8f;

    [Header("Cadencia / cooldowns (s)")]
    [SerializeField] private float cooldownMartillo    = 2.5f;
    [SerializeField] private float cooldownProyectiles = 3f;
    [SerializeField] private float cooldownSalto       = 4f;
    [Tooltip("Espera mínima entre ataques del set (evita spam).")]
    [SerializeField] private float pausaEntreAtaques   = 1f;
    [Tooltip("Factor de cadencia en Fase 2 (<1 = más rápido).")]
    [SerializeField] private float factorFase2         = 0.6f;

    [Header("Set de ataques / vulnerabilidad")]
    [Tooltip("Cantidad de ataques antes de abrir una ventana de vulnerabilidad.")]
    [SerializeField] private int   ataquesPorSet   = 3;
    [Tooltip("Tiempo que la parte queda vulnerable si el jugador no la golpea (s).")]
    [SerializeField] private float tiempoVulnerable = 6f;
    [Tooltip("Duración de la animación de dolor (Tick) al romper una parte, antes de reanudar los ataques.")]
    [SerializeField] private float duracionTick = 0.6f;

    [Header("Martillo (animado por código)")]
    [Tooltip("Frames del martillo EN ORDEN: 0-3 windup, 4 golpe.")]
    [SerializeField] private Sprite[] framesMartillo;
    [Tooltip("Hitbox del puño izquierdo (se activa junto con la derecha).")]
    [SerializeField] private GolemHitbox     hitboxMartilloIzq;
    [Tooltip("Hitbox del puño derecho.")]
    [SerializeField] private GolemHitbox     hitboxMartilloDer;
    [SerializeField] private TelegrafoAtaque telegrafoMartilloIzq;
    [SerializeField] private TelegrafoAtaque telegrafoMartilloDer;
    [Tooltip("Duración total de la preparación (frames 0-3) antes del golpe.")]
    [SerializeField] private float windupMartillo  = 0.6f;
    [Tooltip("Ventana de daño del golpe (frame 4).")]
    [SerializeField] private float impactoMartillo = 0.3f;
    [Tooltip("Tiempo que se mantiene el frame de golpe antes de volver a idle (cierre del ataque).")]
    [SerializeField] private float recuperacionMartillo = 0.35f;

    [Header("Martillo — suelo peligroso")]
    [Tooltip("Tilemap del suelo de la arena. Durante el golpe, los tiles alrededor del Golem cambian a 'tilePeligroso'. Asignar en la escena.")]
    [SerializeField] private Tilemap tilemapSuelo;
    [Tooltip("Tile dañino que reemplaza al suelo original durante el golpe (se restaura al terminar).")]
    [SerializeField] private TileBase tilePeligroso;
    [Tooltip("Alcance del suelo peligroso en tiles a CADA lado del Golem.")]
    [SerializeField] private int alcanceTilesSuelo = 4;
    [Tooltip("Tiempo que el suelo queda peligroso tras el golpe (s). Después los tiles vuelven a la normalidad.")]
    [SerializeField] private float duracionSueloPeligroso = 1.2f;
    [Tooltip("Hitbox que cubre la franja de tiles peligrosos (BoxCollider2D; se posiciona y redimensiona sola).")]
    [SerializeField] private GolemHitbox hitboxSuelo;
    [Tooltip("Altura de la zona letal sobre los tiles peligrosos (u): pisar la franja = muerte.")]
    [SerializeField] private float alturaSueloPeligroso = 1f;

    [Header("Proyectiles (animado por código)")]
    [SerializeField] private GameObject prefabProyectil;
    [Tooltip("Frames del proyectil EN ORDEN 0-12 (entrada, disparos 6/8/10/12, bajadas, rebobina).")]
    [SerializeField] private Sprite[] framesProyectil;
    [Tooltip("Hoyo/cañón izquierdo (posición de la ráfaga más alta; baja en las siguientes).")]
    [SerializeField] private Transform  puntoDisparoIzq;
    [SerializeField] private Transform  puntoDisparoDer;
    [SerializeField] private TelegrafoAtaque telegrafoProyectilIzq;
    [SerializeField] private TelegrafoAtaque telegrafoProyectilDer;
    [Tooltip("Proyectiles por ráfaga y por lado (salen a la vez, espaciados).")]
    [SerializeField] private int   proyectilesPorRafaga = 3;
    [Tooltip("Separación entre los proyectiles de una misma ráfaga (u).")]
    [SerializeField] private float espaciadoProyectil = 0.6f;
    [Tooltip("Cuánto baja la altura de disparo en cada ráfaga sucesiva (u).")]
    [SerializeField] private float bajadaPorRafaga = 0.7f;
    [Tooltip("Tiempo por frame de bajada del proyectil (ritmo lento).")]
    [SerializeField] private float tiempoBajadaProyectil = 0.35f;
    [Tooltip("Pausa después de cada ráfaga de disparo (s).")]
    [SerializeField] private float pausaEntreRafagas = 1f;

    [Header("Salto")]
    [Tooltip("Hitbox del aterrizaje (se activa donde cae el Golem).")]
    [SerializeField] private GolemHitbox     hitboxAterrizaje;
    [SerializeField] private TelegrafoAtaque telegrafoAterrizaje;
    [SerializeField] private float alturaSalto   = 4f;
    [SerializeField] private float duracionSalto = 0.9f;
    [SerializeField] private float windupSalto   = 0.5f;
    [SerializeField] private float impactoAterrizaje = 0.3f;
    [Tooltip("LayerMask del suelo para calcular dónde aterriza el salto.")]
    [SerializeField] private LayerMask maskSuelo;

    [Header("Aparición")]
    [Tooltip("Altura que el Golem sube al emerger de la tierra.")]
    [SerializeField] private float alturaEmerger  = 5f;
    [SerializeField] private float duracionEmerger = 2f;
    [Tooltip("Tiempo que la cámara enfoca al Golem antes de devolver el control.")]
    [SerializeField] private float tiempoFocoCamara = 1.5f;
    [SerializeField] private float velocidadCamara  = 3f;
    [Tooltip("Si está activo, la aparición emerge de la tierra con foco de cámara. Si no, arranca directo.")]
    [SerializeField] private bool  usarAparicion = true;

    [Header("Muerte")]
    [SerializeField] private float duracionMuerte = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip musicaBoss;
    [SerializeField] private AudioClip sfxAparicion;
    [SerializeField] private AudioClip sfxMartillo;
    [SerializeField] private AudioClip sfxSalto;
    [SerializeField] private AudioClip sfxAterrizaje;
    [SerializeField] private AudioClip sfxProyectil;
    [SerializeField] private AudioClip sfxTickDanio;
    [SerializeField] private AudioClip sfxMuerte;

    [Header("Eventos")]
    [Tooltip("Se invoca una vez al entrar en Fase 2 (cablear colapso de plataformas, etc.).")]
    [SerializeField] private UnityEvent alEntrarFase2;
    [Tooltip("Se invoca una vez al morir el Golem (cablear ObjetoActivable.Desactivar() de la pared para que el jugador escape).")]
    [SerializeField] private UnityEvent alMorir;

    // ── Estado interno ────────────────────────────────────────────
    private Estado _estado = Estado.Dormido;
    private Rigidbody2D _rb;
    private Transform   _jugador;

    private bool _peleaIniciada;
    private bool _fase2;
    private int  _parteActual;          // índice de la próxima parte a romper (0..3)
    private bool _golpeEnVentana;       // el jugador golpeó durante la ventana actual

    private float _proxMartillo, _proxProyectiles, _proxSalto;
    private Coroutine _focoCamara;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType    = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _jugador = player.transform;

        for (int i = 0; i < partesDebiles.Length; i++)
        {
            partesDebiles[i]?.Inicializar(this, i);
            partesDebiles[i]?.Desactivar();
        }

        ApagarHitboxesAtaque();
        cuerpoDanio?.SetActiva(false);

        if (activarAlInicio) IniciarPelea();
    }

    // ── API PÚBLICA ───────────────────────────────────────────────

    // Llamado por BossFightTrigger cuando el jugador cae a la arena.
    // saltarIntro = true cuando es un respawn de checkpoint (no repetir la cinemática).
    public void IniciarPelea(bool saltarIntro = false)
    {
        if (_peleaIniciada) return;
        _peleaIniciada = true;
        StartCoroutine(RutinaPelea(saltarIntro));
    }

    // Alias sin argumentos para cablear la activación desde un UnityEvent
    // (Lever, placa de presión, RelicaPickup, etc.).
    public void Activar() => IniciarPelea();

    // Llamado por GolemWeakPoint al completar el tirón sobre la parte vulnerable.
    public void RegistrarGolpe(int indice)
    {
        if (_estado != Estado.Combate) return;
        if (indice != _parteActual)    return;   // solo cuenta la parte activa

        _golpeEnVentana = true;
        AudioManager.instance?.FxSoundEffect(sfxTickDanio, transform, 1f);
        animator?.SetTrigger("Tick");
        GameDebug.Log($"[GolemBoss] Golpe registrado en parte {_parteActual}");

        _parteActual++;

        // Fase 2 desde la mano derecha (tras romper la 1ª mano).
        if (!_fase2 && _parteActual >= 1)
            EntrarFase2();

        if (_parteActual >= partesDebiles.Length)
            _estado = Estado.Muriendo;   // el pecho fue el último → muerte
    }

    // ── CORRUTINA MAESTRA ─────────────────────────────────────────

    private IEnumerator RutinaPelea(bool saltarIntro)
    {
        if (usarAparicion && !saltarIntro)
            yield return StartCoroutine(RutinaAparicion());
        else
            AudioManager.instance?.PlayMusic(musicaBoss);

        _estado = Estado.Combate;

        // Contacto con el cuerpo = reinicio durante todo el combate (GDD).
        cuerpoDanio?.SetActiva(true);

        while (_estado == Estado.Combate)
        {
            for (int i = 0; i < ataquesPorSet && _estado == Estado.Combate; i++)
            {
                yield return StartCoroutine(EjecutarAtaqueElegido());
                yield return new WaitForSeconds(PausaAjustada(pausaEntreAtaques));
            }

            if (_estado == Estado.Combate)
                yield return StartCoroutine(RutinaVentanaVulnerable());
        }

        if (_estado == Estado.Muriendo)
            yield return StartCoroutine(RutinaMuerte());
    }

    // ── APARICIÓN ─────────────────────────────────────────────────

    private IEnumerator RutinaAparicion()
    {
        _estado = Estado.Emergiendo;

        var (mov, grapple) = CongelarJugador(true);
        bool camaraTomada  = TomarCamara(true);

        Vector2 posCombate   = _rb.position;
        Vector2 posEnterrado = posCombate - Vector2.up * alturaEmerger;
        _rb.position = posEnterrado;

        AudioManager.instance?.FxSoundEffect(sfxAparicion, transform, 1f);
        AudioManager.instance?.PlayMusic(musicaBoss);
        animator?.SetTrigger("Emerger");

        float t = 0f;
        while (t < duracionEmerger)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionEmerger));
            _rb.MovePosition(Vector2.Lerp(posEnterrado, posCombate, k));
            yield return new WaitForFixedUpdate();
        }
        _rb.MovePosition(posCombate);

        yield return new WaitForSeconds(tiempoFocoCamara);

        if (camaraTomada) TomarCamara(false);
        CongelarJugador(false, mov, grapple);
    }

    // ── SELECCIÓN Y EJECUCIÓN DE ATAQUES ──────────────────────────

    private IEnumerator EjecutarAtaqueElegido()
    {
        Ataque? elegido = ElegirAtaque();
        if (elegido == null)
        {
            yield return new WaitForSeconds(0.3f);
            yield break;
        }

        switch (elegido.Value)
        {
            case Ataque.Martillo:    yield return StartCoroutine(AtaqueMartillo());    break;
            case Ataque.Proyectiles: yield return StartCoroutine(AtaqueProyectiles()); break;
            case Ataque.Salto:       yield return StartCoroutine(AtaqueSalto());       break;
        }
    }

    private Ataque? ElegirAtaque()
    {
        float dist = _jugador != null
            ? Vector2.Distance(transform.position, _jugador.position)
            : rangoCerca;

        Ataque preferido = dist <= rangoCerca ? Ataque.Martillo
                         : dist >= rangoLejos ? Ataque.Salto
                         : Ataque.Proyectiles;

        if (Disponible(preferido)) return preferido;

        foreach (Ataque a in OrdenFallback(preferido))
            if (Disponible(a)) return a;

        return null;
    }

    private Ataque[] OrdenFallback(Ataque preferido)
    {
        switch (preferido)
        {
            case Ataque.Martillo: return new[] { Ataque.Proyectiles, Ataque.Salto };
            case Ataque.Salto:    return new[] { Ataque.Proyectiles, Ataque.Martillo };
            default:              return new[] { Ataque.Martillo, Ataque.Salto };
        }
    }

    private bool Disponible(Ataque a)
    {
        switch (a)
        {
            case Ataque.Martillo:    return Time.time >= _proxMartillo;
            case Ataque.Proyectiles: return Time.time >= _proxProyectiles;
            case Ataque.Salto:       return Time.time >= _proxSalto;
        }
        return false;
    }

    // ── ATAQUE: MARTILLO (por código, golpea ambos costados) ──────

    private IEnumerator AtaqueMartillo()
    {
        _proxMartillo = Time.time + PausaAjustada(cooldownMartillo);

        telegrafoMartilloIzq?.Mostrar(windupMartillo);
        telegrafoMartilloDer?.Mostrar(windupMartillo);

        bool control = TomarControlSprite();

        // Windup (frames 0-3): preparación, repartida en la duración del windup.
        float tFrame = FRAME_GOLPE_MARTILLO > 0 ? windupMartillo / FRAME_GOLPE_MARTILLO : windupMartillo;
        for (int f = 0; f < FRAME_GOLPE_MARTILLO; f++)
        {
            MostrarFrame(framesMartillo, f);
            yield return new WaitForSeconds(tFrame);
        }

        // Golpe (frame 4): daño a ambos costados a la vez + onda de suelo peligroso.
        MostrarFrame(framesMartillo, FRAME_GOLPE_MARTILLO);
        AudioManager.instance?.FxSoundEffect(sfxMartillo, transform, 1f);
        hitboxMartilloIzq?.SetActiva(true);
        hitboxMartilloDer?.SetActiva(true);
        StartCoroutine(RutinaSueloPeligroso());
        yield return new WaitForSeconds(impactoMartillo);
        hitboxMartilloIzq?.SetActiva(false);
        hitboxMartilloDer?.SetActiva(false);

        // Recuperación: sostener el frame de golpe para que el ataque cierre completo
        // en vez de saltar de golpe a idle.
        yield return new WaitForSeconds(recuperacionMartillo);

        SoltarControlSprite(control);
    }

    // Onda del martillo: reemplaza los tiles del suelo alrededor del Golem por el tile
    // peligroso, cubre la franja con una hitbox letal, y tras 'duracionSueloPeligroso'
    // restaura los tiles originales. Solo cambia celdas donde ya había suelo.
    private IEnumerator RutinaSueloPeligroso()
    {
        if (tilemapSuelo == null || tilePeligroso == null) yield break;

        // Celda del suelo bajo el centro del Golem.
        RaycastHit2D hit = Physics2D.Raycast(_rb.position, Vector2.down, 30f, maskSuelo);
        if (hit.collider == null) yield break;
        Vector3Int centro = tilemapSuelo.WorldToCell(hit.point + Vector2.down * 0.05f);

        // Cambiar tiles existentes por el peligroso, recordando los originales.
        var originales = new Dictionary<Vector3Int, TileBase>();
        for (int dx = -alcanceTilesSuelo; dx <= alcanceTilesSuelo; dx++)
        {
            Vector3Int celda    = centro + new Vector3Int(dx, 0, 0);
            TileBase   original = tilemapSuelo.GetTile(celda);
            if (original == null || original == tilePeligroso) continue;
            originales[celda] = original;
            tilemapSuelo.SetTile(celda, tilePeligroso);
        }

        // Zona letal sobre la franja: pisar los tiles peligrosos = muerte.
        if (hitboxSuelo != null)
        {
            float   ancho  = (alcanceTilesSuelo * 2 + 1) * tilemapSuelo.cellSize.x;
            Vector3 topeCentro = tilemapSuelo.GetCellCenterWorld(centro)
                               + Vector3.up * (tilemapSuelo.cellSize.y * 0.5f);
            hitboxSuelo.transform.position = topeCentro + Vector3.up * (alturaSueloPeligroso * 0.5f);
            if (hitboxSuelo.TryGetComponent(out BoxCollider2D caja))
                caja.size = new Vector2(ancho, alturaSueloPeligroso);
            hitboxSuelo.SetActiva(true);
        }

        yield return new WaitForSeconds(duracionSueloPeligroso);

        hitboxSuelo?.SetActiva(false);
        foreach (var kv in originales)
            tilemapSuelo.SetTile(kv.Key, kv.Value);
    }

    // ── ATAQUE: PROYECTILES (por código, ritmo pausado) ───────────

    private IEnumerator AtaqueProyectiles()
    {
        _proxProyectiles = Time.time + PausaAjustada(cooldownProyectiles);

        float tEntrada = FRAME_PRIMER_DISPARO * tiempoBajadaProyectil;
        telegrafoProyectilIzq?.Mostrar(tEntrada);
        telegrafoProyectilDer?.Mostrar(tEntrada);

        bool control = TomarControlSprite();

        // Entrada (frames 0-5): cae y se entierra en su lugar, bajando lento. No se reubica.
        for (int f = 0; f < FRAME_PRIMER_DISPARO; f++)
        {
            MostrarFrame(framesProyectil, f);
            yield return new WaitForSeconds(tiempoBajadaProyectil);
        }

        // Ráfagas: frames pares (6,8,10,12) disparan y pausan; impares (7,9,11) bajan lento.
        int rafaga = 0;
        for (int f = FRAME_PRIMER_DISPARO; f <= FRAME_ULTIMO_PROYECTIL; f++)
        {
            MostrarFrame(framesProyectil, f);
            if (f % 2 == 0)
            {
                DispararRafaga(rafaga++);
                yield return new WaitForSeconds(pausaEntreRafagas);
            }
            else
            {
                yield return new WaitForSeconds(tiempoBajadaProyectil);
            }
        }

        // Rebobinado (11→0): el Golem se recompone, lento.
        for (int f = FRAME_ULTIMO_PROYECTIL - 1; f >= 0; f--)
        {
            MostrarFrame(framesProyectil, f);
            yield return new WaitForSeconds(tiempoBajadaProyectil);
        }

        SoltarControlSprite(control);
    }

    // Dispara una ráfaga a ambos lados a la altura de esa ráfaga (baja con el índice).
    private void DispararRafaga(int rafaga)
    {
        if (prefabProyectil == null) return;

        AudioManager.instance?.FxSoundEffect(sfxProyectil, transform, 1f);
        float bajada = rafaga * bajadaPorRafaga;
        DispararLado(puntoDisparoIzq, Vector2.left,  bajada);
        DispararLado(puntoDisparoDer, Vector2.right, bajada);
    }

    // Instancia 'proyectilesPorRafaga' proyectiles hacia 'dir', espaciados, a la vez.
    private void DispararLado(Transform punto, Vector2 dir, float bajada)
    {
        Vector3 origen = (punto != null ? punto.position : transform.position) + Vector3.down * bajada;
        for (int i = 0; i < proyectilesPorRafaga; i++)
        {
            Vector3 pos = origen + (Vector3)(dir * (i * espaciadoProyectil));
            GameObject go = Instantiate(prefabProyectil, pos, Quaternion.identity);
            go.GetComponent<Projectile>()?.Inicializar(dir);
        }
    }

    // ── ATAQUE: SALTO (se reubica donde cae) ──────────────────────

    private IEnumerator AtaqueSalto()
    {
        _proxSalto = Time.time + PausaAjustada(cooldownSalto);

        Vector2 origen  = _rb.position;
        Vector2 destino = CalcularDestinoSalto(origen);

        // Telegrafío: marcar la zona de aterrizaje.
        if (telegrafoAterrizaje != null)
        {
            telegrafoAterrizaje.transform.position = destino;
            telegrafoAterrizaje.Mostrar(windupSalto + duracionSalto);
        }

        animator?.SetTrigger("SaltoWindup");
        yield return new WaitForSeconds(windupSalto);

        // Arco de salto hacia el destino (frame 2 del clip = auge, coincide con k=0.5).
        animator?.SetTrigger("Saltar");
        AudioManager.instance?.FxSoundEffect(sfxSalto, transform, 1f);
        float t = 0f;
        while (t < duracionSalto)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionSalto);
            Vector2 pos = Vector2.Lerp(origen, destino, k);
            pos.y += Mathf.Sin(k * Mathf.PI) * alturaSalto;
            _rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }
        _rb.MovePosition(destino);

        // Aterrizaje (frame 4): daño donde cae.
        AudioManager.instance?.FxSoundEffect(sfxAterrizaje, transform, 1f);
        hitboxAterrizaje?.SetActiva(true);
        yield return new WaitForSeconds(impactoAterrizaje);
        hitboxAterrizaje?.SetActiva(false);
    }

    private Vector2 CalcularDestinoSalto(Vector2 origen)
    {
        if (_jugador == null) return origen;

        Vector2 destino = new Vector2(_jugador.position.x, origen.y);
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(destino.x, _jugador.position.y + 1f), Vector2.down, 30f, maskSuelo);
        if (hit.collider != null)
            destino.y = hit.point.y + (origen.y - SueloBajoOrigen(origen));

        return destino;
    }

    private float SueloBajoOrigen(Vector2 origen)
    {
        RaycastHit2D hit = Physics2D.Raycast(origen, Vector2.down, 30f, maskSuelo);
        return hit.collider != null ? hit.point.y : origen.y;
    }

    // ── VENTANA DE VULNERABILIDAD ─────────────────────────────────

    private IEnumerator RutinaVentanaVulnerable()
    {
        if (_parteActual >= partesDebiles.Length) yield break;

        var parte = partesDebiles[_parteActual];
        if (parte == null) yield break;

        _golpeEnVentana = false;
        parte.Activar();
        // Mostrar el frame estático de esa parte (el cuerpo se queda quieto y debilitado).
        if (_parteActual < TriggersVuln.Length) animator?.SetTrigger(TriggersVuln[_parteActual]);
        GameDebug.Log($"[GolemBoss] Ventana vulnerable: parte {_parteActual}");

        float t = 0f;
        while (t < tiempoVulnerable && !_golpeEnVentana && _estado == Estado.Combate)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Si expiró sin golpe, la parte sigue siendo la misma en la próxima ventana.
        if (!_golpeEnVentana)
        {
            parte.Desactivar();
            animator?.SetTrigger("Reposar");
        }
        else
        {
            // Hubo golpe: RegistrarGolpe ya disparó "Tick". Esperar a que la animación de
            // dolor se reproduzca antes de reanudar los ataques (si no, el próximo ataque la
            // pisa: martillo/proyectil desactivan el Animator y el Tick nunca se ve).
            yield return new WaitForSeconds(duracionTick);
        }
    }

    // ── MUERTE ────────────────────────────────────────────────────

    private IEnumerator RutinaMuerte()
    {
        _estado = Estado.Muerto;

        // Asegurar que el Animator controle el sprite (por si murió durante un ataque por código).
        if (animator != null) animator.enabled = true;

        // Apagar todo el daño: el Golem ya no es una amenaza.
        ApagarHitboxesAtaque();
        cuerpoDanio?.SetActiva(false);
        foreach (var p in partesDebiles) p?.Desactivar();

        // El estado "Morir" del Animator es terminal y no-loop → se queda en el último frame.
        animator?.SetTrigger("Morir");
        AudioManager.instance?.FxSoundEffect(sfxMuerte, transform, 1f);
        GameDebug.Log("[GolemBoss] Golem derrotado.");

        yield return new WaitForSeconds(duracionMuerte);

        // Evento de derrota: destruir la pared para que el jugador escape (cableado en Inspector).
        alMorir?.Invoke();
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private void EntrarFase2()
    {
        _fase2 = true;
        animator?.SetBool("Fase2", true);
        GameDebug.Log("[GolemBoss] Entrando en Fase 2.");
        alEntrarFase2?.Invoke();
    }

    private float PausaAjustada(float baseSeg) => _fase2 ? baseSeg * factorFase2 : baseSeg;

    // Desactiva el Animator para animar el sprite por código. Devuelve true si tomó el control.
    private bool TomarControlSprite()
    {
        if (animator == null) return false;
        animator.enabled = false;
        return true;
    }

    // Reactiva el Animator y lo lleva a Idle tras un ataque animado por código.
    private void SoltarControlSprite(bool tenia)
    {
        if (!tenia || animator == null) return;
        animator.enabled = true;
        animator.SetTrigger("Reposar");
    }

    private void MostrarFrame(Sprite[] frames, int i)
    {
        if (sr != null && frames != null && i >= 0 && i < frames.Length && frames[i] != null)
            sr.sprite = frames[i];
    }

    private void ApagarHitboxesAtaque()
    {
        hitboxMartilloIzq?.SetActiva(false);
        hitboxMartilloDer?.SetActiva(false);
        hitboxAterrizaje?.SetActiva(false);
        hitboxSuelo?.SetActiva(false);
    }

    private (MonoBehaviour, MonoBehaviour) CongelarJugador(bool congelar,
        MonoBehaviour mov = null, MonoBehaviour grapple = null)
    {
        if (congelar)
        {
            if (_jugador == null) return (null, null);
            mov     = _jugador.GetComponent<PlayerMovement>();
            grapple = _jugador.GetComponent<GrappleScript>();
            var rbJug = _jugador.GetComponent<Rigidbody2D>();
            if (rbJug != null) rbJug.linearVelocity = Vector2.zero;
            if (mov     != null) mov.enabled     = false;
            if (grapple != null) grapple.enabled = false;
            return (mov, grapple);
        }

        if (mov     != null) mov.enabled     = true;
        if (grapple != null) grapple.enabled = true;
        return (mov, grapple);
    }

    private bool TomarCamara(bool tomar)
    {
        if (CamaraScript.Instance == null || Camera.main == null) return false;

        if (tomar)
        {
            CamaraScript.Instance.enabled = false;
            _focoCamara = StartCoroutine(RutinaFocoCamara());
            return true;
        }

        if (_focoCamara != null) { StopCoroutine(_focoCamara); _focoCamara = null; }
        CamaraScript.Instance.enabled = true;
        CamaraScript.Instance.SnapToPlayer();
        return true;
    }

    private IEnumerator RutinaFocoCamara()
    {
        var camT = Camera.main.transform;
        while (true)
        {
            Vector3 objetivo = new Vector3(transform.position.x, transform.position.y, camT.position.z);
            camT.position = Vector3.Lerp(camT.position, objetivo, velocidadCamara * Time.deltaTime);
            yield return null;
        }
    }

    // ── GIZMOS ────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, rangoCerca);
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, rangoLejos);
    }
}
