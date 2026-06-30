using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoopManager : MonoBehaviour
{
    [HideInInspector] public static GameLoopManager Instance { get; private set; }

    [Header("Cristales")]
    [SerializeField] private int cristalesObtenidos = 0;
    [SerializeField] private int cristalesTotales;

    [Header("Muerte / Reinicio")]
    [SerializeField] private int   contadorMuertes = 0;
    [SerializeField] private float tiempoFadeOut   = 0.4f;
    [SerializeField] private float tiempoReinicio  = 1.5f;

    [Header("Nivel")]
    [SerializeField] private int nivelActual;
    [SerializeField] private int totalNiveles;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxMuerte;

    [Header("UI — HUD")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private TMP_Text    txtCristales;
    [SerializeField] private TMP_Text    txtMuertes;

    [Header("UI — Paneles")]
    [SerializeField] private GameObject panelFinNivel;
    [Tooltip("Texto del panel de fin de nivel: tiempo del nivel (mm:ss).")]
    [SerializeField] private TMP_Text   txtTiempoPanel;
    [Tooltip("Texto del panel de fin de nivel: contador de muertes.")]
    [SerializeField] private TMP_Text   txtMuertesPanel;
    [Tooltip("Texto del panel de fin de nivel: cristales obtenidos / totales.")]
    [SerializeField] private TMP_Text   txtCristalesPanel;
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelOpciones;
    [Tooltip("Panel que muestra el esquema de controles. Se abre desde la pausa y vuelve a ella al cerrar.")]
    [SerializeField] private GameObject panelControles;

    [Header("Muerte Dramática")]
    [Tooltip("RawImage con el shader SpotlightOverlay. Debe ser hijo del GameObject de GameLoopManager para persistir entre escenas.")]
    [SerializeField] private SpotlightOverlay spotlightOverlay;
    [Tooltip("Tiempo de fade-in del spotlight al morir (s).")]
    [SerializeField] private float spotlightFadeIn  = 0.10f;
    [Tooltip("Tiempo de fade-out del spotlight antes del reinicio (s).")]
    [SerializeField] private float spotlightFadeOut = 0.20f;
    [Tooltip("Segundos que el spotlight permanece visible tras el fade-in. 0 = dura hasta que la escena se recargue.")]
    [SerializeField] private float tiempoSpotlight  = 0f;

    public  static bool IsPaused { get; private set; }

    private float   tiempoAcumulado;
    private int     cristalesAcumulados;
    private bool    isPaused;
    private bool    isDying;

    // Muertes acumuladas solo en el nivel actual (se resetea al entrar a un nivel nuevo).
    private int     muertesNivel;

    // buildIndex del último nivel cargado — distingue "nivel nuevo" de "recarga por muerte".
    private int     _nivelAnterior = -1;

    // Cristales ya recogidos en el nivel actual (clave = nivel + posición inicial).
    // Persiste entre recargas (DontDestroyOnLoad) para que no reaparezcan al morir.
    private readonly HashSet<string> _cristalesRecogidos = new HashSet<string>();

    // Estado del checkpoint — persiste entre recargas de escena (DontDestroyOnLoad)
    private Vector3 _checkpointPos;
    private bool    _checkpointActivo;
    private int     _checkpointScena = -1;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nivel nuevo = el buildIndex cambió. Una recarga por muerte mantiene el mismo
        // buildIndex, así que conserva cristales recogidos y muertes acumuladas.
        bool nivelNuevo     = scene.buildIndex != _nivelAnterior;
        _nivelAnterior      = scene.buildIndex;

        if (nivelNuevo)
        {
            _cristalesRecogidos.Clear();
            muertesNivel = 0;
        }

        nivelActual         = scene.buildIndex;
        isDying             = false;
        isPaused            = false;
        IsPaused            = false;
        Time.timeScale      = 1f;
        AudioListener.pause = false;
        if (spotlightOverlay != null) spotlightOverlay.gameObject.SetActive(false);

        // Red de seguridad: si un panel de pausa/opciones quedó activo al cambiar de
        // escena (muerte durante pausa, etc.), desactivarlo para que no quede colgado.
        if (panelPausa     != null) panelPausa.SetActive(false);
        if (panelOpciones  != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);

        // Al volver al menu (escena 0) el Canvas DDOL del GLM no debe bloquear la UI del menu.
        // El panel queda oculto de inmediato; el menu maneja su propia presentacion visual.
        if (scene.buildIndex == 0)
        {
            if (fadePanel != null)
            {
                fadePanel.alpha = 0f;
                fadePanel.gameObject.SetActive(false);
            }
            return;
        }

        cristalesObtenidos = _cristalesRecogidos.Count;
        ActualizarHUDCristales();
        ActualizarHUDMuertes();
        StartCoroutine(FadeInConEspera());
        if (_checkpointActivo && _checkpointScena == scene.buildIndex)
            StartCoroutine(AplicarSpawnCheckpoint());
        // La música por escena la maneja AudioManager (suscrito a sceneLoaded).
    }

    private void Start()
    {
        nivelActual  = SceneManager.GetActiveScene().buildIndex;
        totalNiveles = SceneManager.sceneCountInBuildSettings;

        ActualizarHUDCristales();
        ActualizarHUDMuertes();
    }

    private void Update()
    {
        // No procesar atajos de teclado en la escena del menu (indice 0).
        // El GameLoopManager persiste entre escenas (DDOL) y su Update correria
        // en el menu abriendo el panel de pausa sobre los botones del menu.
        if (nivelActual == 0) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            TogglePause();

        // R reinicia solo en juego (no en pausa ni en fin de nivel)
        if (Input.GetKeyDown(KeyCode.R) && !isPaused)
            Reintentar();
    }

    // ── GAMEPLAY ──────────────────────────────────────────────────

    // fromVoid: true  → muerte por el vacío (sin dramatismo, reinicio inmediato)
    // fromVoid: false → muerte por hazard (animación + spotlight dramático)
    public void PlayerDied(bool fromVoid = false)
    {
        if (isDying) return;
        isDying = true;

        // Si el jugador estaba en pausa al morir (pausa y hazard en el mismo frame),
        // cerrar el menú para que no sobreviva al reinicio de escena (BUG-013).
        if (isPaused) CerrarPausaSilencioso();

        contadorMuertes++;
        muertesNivel++;
        ActualizarHUDMuertes();
        AudioManager.instance?.FxSoundEffect(sfxMuerte, transform, 1f);

        if (fromVoid)
            StartCoroutine(RutinaReinicio());
        else
            StartCoroutine(RutinaMuerteDramatica());
    }

    // Registra un cristal como recogido por su posición inicial. Idempotente: si ya
    // estaba registrado (p. ej. doble disparo del OverlapCircle) no lo cuenta dos veces.
    public void CollectCrystal(Vector3 posicionInicial)
    {
        if (_cristalesRecogidos.Add(ClaveCristal(posicionInicial)))
        {
            cristalesObtenidos = _cristalesRecogidos.Count;
            ActualizarHUDCristales();
        }
    }

    // Consultado por CrystalPickup.Start() — si ya fue recogido, no reaparece tras morir.
    public bool CristalYaRecogido(Vector3 posicionInicial)
    {
        return _cristalesRecogidos.Contains(ClaveCristal(posicionInicial));
    }

    // Clave estable entre recargas: nivel + posición inicial redondeada a centésimas.
    private string ClaveCristal(Vector3 pos)
    {
        return $"{nivelActual}:{Mathf.RoundToInt(pos.x * 100f)}:{Mathf.RoundToInt(pos.y * 100f)}";
    }

    // Llamado por CheckpointZone al activarse — guarda posición y cristales actuales
    public void GuardarCheckpoint(Vector3 pos)
    {
        _checkpointPos    = pos;
        _checkpointActivo = true;
        _checkpointScena  = nivelActual;
    }

    // Usado por CheckpointZone.Start() para restaurar estado visual al recargar
    public bool EsEsteCheckpoint(Vector3 pos)
    {
        return _checkpointActivo
            && _checkpointScena == nivelActual
            && Vector3.Distance(_checkpointPos, pos) < 0.1f;
    }

    public void NivelCompleto()
    {
        float tiempoNivel    = Time.timeSinceLevelLoad;
        tiempoAcumulado     += tiempoNivel;
        cristalesAcumulados += cristalesObtenidos;

        // Desbloquea el siguiente nivel en el selector (persistido con PlayerPrefs).
        ProgresoNiveles.DesbloquearHasta(nivelActual + 1);

        ActualizarPanelResultado(tiempoNivel);

        if (panelFinNivel != null) panelFinNivel.SetActive(true);
        IsPaused              = true;
        Time.timeScale        = 0f;
        AudioListener.pause   = true;
    }

    public void ContinuarSiguienteNivel()
    {
        LimpiarCheckpoint();
        IsPaused              = false;
        Time.timeScale        = 1f;
        AudioListener.pause   = false;
        if (panelFinNivel != null) panelFinNivel.SetActive(false);

        int siguienteNivel = nivelActual + 1;
        cristalesObtenidos = 0;

        // Si no hay más niveles, vuelve al menú principal.
        int destino = siguienteNivel < totalNiveles ? siguienteNivel : 0;
        nivelActual = destino;
        StartCoroutine(CargarEscenaConFade(destino));
    }

    // ── PAUSA ─────────────────────────────────────────────────────

    public void TogglePause()
    {
        // No interrumpir el fin de nivel ni la victoria
        if (panelFinNivel != null && panelFinNivel.activeSelf) return;

        // No permitir pausar durante la secuencia de muerte: el reinicio recarga
        // la escena y un panel abierto aquí quedaría colgado tras el respawn (BUG-013).
        if (isDying) return;

        isPaused              = !isPaused;
        IsPaused              = isPaused;
        Time.timeScale        = isPaused ? 0f : 1f;
        AudioListener.pause   = isPaused;

        if (panelPausa != null) panelPausa.SetActive(isPaused);

        // Cerrar subpaneles al salir de pausa
        if (!isPaused)
        {
            if (panelOpciones  != null) panelOpciones.SetActive(false);
            if (panelControles != null) panelControles.SetActive(false);
        }
    }

    // Botón "Reanudar" del menú de pausa
    public void Reanudar()
    {
        if (!isPaused) return;
        TogglePause();
    }

    // Botón "Reintentar" — reinicia el nivel completo (ignora checkpoints)
    public void Reintentar()
    {
        LimpiarCheckpoint();
        CerrarPausaSilencioso();
        // Reinicio total del nivel: los cristales y las muertes vuelven a cero
        // (a diferencia de morir, que los conserva).
        _cristalesRecogidos.Clear();
        muertesNivel = 0;
        StartCoroutine(RutinaReinicio());
    }

    // Botón "Opciones"
    public void AbrirOpciones()
    {
        if (panelPausa    != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    // Botón "Atrás" dentro de opciones
    public void CerrarOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelPausa    != null && isPaused) panelPausa.SetActive(true);
    }

    // Botón "Menú Principal" — carga la escena índice 0
    public void IrAlMenuPrincipal()
    {
        LimpiarCheckpoint();
        CerrarPausaSilencioso();

        // Ocultar paneles que quedan activos en el Canvas DDOL — si persisten,
        // tapan la UI del menu y el jugador no puede interactuar con ella.
        if (panelFinNivel != null) panelFinNivel.SetActive(false);

        // Cancelar corrutinas en vuelo (RutinaReinicio, RutinaMuerteDramatica…).
        // Sin esto, una RutinaReinicio pendiente podria cargar la escena de juego
        // de forma asincrona incluso mientras el jugador ya esta en el menu.
        StopAllCoroutines();

        isDying = false;
        StartCoroutine(CargarEscenaConFade(0));
    }

    private void LimpiarCheckpoint()
    {
        _checkpointActivo = false;
        _checkpointScena  = -1;
    }

    // Cierra la pausa sin animación (usada antes de cargar escena)
    private void CerrarPausaSilencioso()
    {
        isPaused       = false;
        IsPaused       = false;
        Time.timeScale = 1f;
        if (panelPausa     != null) panelPausa.SetActive(false);
        if (panelOpciones  != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
    }

    // ── CORRUTINAS ────────────────────────────────────────────────

    // Posiciona al jugador en el checkpoint después de que Start() ya corrió (1 frame delay)
    private IEnumerator AplicarSpawnCheckpoint()
    {
        yield return null;
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player == null) yield break;

        player.RespawnAt(_checkpointPos);
        CamaraScript.Instance?.SnapToPlayer();
    }

    private IEnumerator RutinaMuerteDramatica()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        player?.TriggerDeath();

        // Pantalla negra inmediatamente (antes de que el jugador caiga o cambie de estado)
        if (spotlightOverlay != null)
        {
            spotlightOverlay.Activate(player != null ? player.transform : null);
            yield return StartCoroutine(spotlightOverlay.FadeIn(spotlightFadeIn));
        }

        // Pausar todo el juego — el Animator del jugador usa UnscaledTime y sigue animando
        Time.timeScale      = 0f;
        AudioListener.pause = true;

        if (tiempoSpotlight > 0f)
        {
            // Spotlight visible durante el tiempo configurado, luego fade-out y reinicio
            yield return new WaitForSecondsRealtime(tiempoSpotlight);
            if (spotlightOverlay != null)
                yield return StartCoroutine(spotlightOverlay.FadeOut(spotlightFadeOut));
        }
        // tiempoSpotlight == 0: el spotlight dura hasta la recarga; OnSceneLoaded lo desactiva

        yield return StartCoroutine(RutinaReinicio());
    }

    private IEnumerator RutinaReinicio()
    {
        yield return StartCoroutine(FadeOut(tiempoFadeOut));

        float espera = tiempoReinicio - tiempoFadeOut;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, espera));

        AsyncOperation op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        op.allowSceneActivation = false;

        while (op.progress < 0.7f)
            yield return null;

        op.allowSceneActivation = true;
    }

    private IEnumerator CargarEscenaConFade(int indice)
    {
        yield return StartCoroutine(FadeOut(tiempoFadeOut));

        AsyncOperation op = SceneManager.LoadSceneAsync(indice);
        op.allowSceneActivation = false;

        while (op.progress < 0.7f)
            yield return null;

        op.allowSceneActivation = true;
        // El fade-in lo maneja OnSceneLoaded -> FadeInConEspera (escenas de juego)
        // o directamente ocultando el panel (escena de menu).
    }

    private IEnumerator FadeOut(float duracion)
    {
        if (fadePanel == null) yield break;
        fadePanel.gameObject.SetActive(true);
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Clamp01(t / duracion);
            yield return null;
        }
        fadePanel.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;
        float t = 0f;
        while (t < tiempoFadeOut)
        {
            t += Time.unscaledDeltaTime;
            fadePanel.alpha = 1f - Mathf.Clamp01(t / tiempoFadeOut);
            yield return null;
        }
        fadePanel.alpha = 0f;
        fadePanel.gameObject.SetActive(false);
    }

    private IEnumerator FadeInConEspera()
    {
        if (fadePanel == null) yield break;
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 1f;
        yield return new WaitForSecondsRealtime(0.1f);
        yield return StartCoroutine(FadeIn());
    }

    // ── HUD ───────────────────────────────────────────────────────

    private void ActualizarHUDCristales()
    {
        if (txtCristales != null)
            txtCristales.text = $"{cristalesObtenidos}<color=#4A3E30>/</color><color=#C8A040>{cristalesTotales}</color>";
    }

    private void ActualizarHUDMuertes()
    {
        if (txtMuertes != null)
            txtMuertes.text = $"Muertes: {contadorMuertes}";
    }

    // Llena los tres textos del panel de fin de nivel: tiempo, muertes y cristales.
    private void ActualizarPanelResultado(float tiempoNivel)
    {
        if (txtTiempoPanel    != null) txtTiempoPanel.text    = $"Tiempo: {FormatearTiempo(tiempoNivel)}";
        if (txtMuertesPanel   != null) txtMuertesPanel.text   = $"Muertes: {muertesNivel}";
        if (txtCristalesPanel != null) txtCristalesPanel.text = $"Cristales: {cristalesObtenidos} / {cristalesTotales}";
    }

    // Formatea segundos a mm:ss (los niveles duran minutos, más legible que "312.45s").
    private static string FormatearTiempo(float segundos)
    {
        int min = (int)(segundos / 60f);
        int seg = (int)(segundos % 60f);
        return $"{min:00}:{seg:00}";
    }
}
