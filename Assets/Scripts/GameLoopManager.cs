using System.Collections;
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
    [SerializeField] private AudioClip musicaNivel;

    [Header("UI — HUD")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private TMP_Text    txtCristales;
    [SerializeField] private TMP_Text    txtMuertes;

    [Header("UI — Paneles")]
    [SerializeField] private GameObject panelFinNivel;
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelOpciones;
    [SerializeField] private GameObject panelVictoria;

    [Header("Muerte Dramática")]
    [Tooltip("RawImage con el shader SpotlightOverlay. Debe ser hijo del GameObject de GameLoopManager para persistir entre escenas.")]
    [SerializeField] private SpotlightOverlay spotlightOverlay;
    [Tooltip("Tiempo de fade-in del spotlight al morir (s).")]
    [SerializeField] private float spotlightFadeIn  = 0.10f;
    [Tooltip("Tiempo de fade-out del spotlight antes del reinicio (s).")]
    [SerializeField] private float spotlightFadeOut = 0.20f;
    [Tooltip("Segundos que el spotlight permanece visible tras el fade-in. 0 = dura hasta que la escena se recargue.")]
    [SerializeField] private float tiempoSpotlight  = 0f;

    private float   tiempoAcumulado;
    private int     cristalesAcumulados;
    private bool    isPaused;
    private bool    isDying;

    // Estado del checkpoint — persiste entre recargas de escena (DontDestroyOnLoad)
    private Vector3 _checkpointPos;
    private bool    _checkpointActivo;
    private int     _checkpointScena       = -1;
    private int     _cristalesEnCheckpoint;

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
        nivelActual         = scene.buildIndex;
        isDying             = false;
        Time.timeScale      = 1f;
        AudioListener.pause = false;
        if (spotlightOverlay != null) spotlightOverlay.gameObject.SetActive(false);

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

        ActualizarHUDCristales();
        ActualizarHUDMuertes();
        StartCoroutine(FadeInConEspera());
        if (_checkpointActivo && _checkpointScena == scene.buildIndex)
            StartCoroutine(AplicarSpawnCheckpoint());
        if (musicaNivel != null)
        {
            AudioManager.instance?.PlayClip(musicaNivel);
            AudioManager.instance?.SetMusicVolume(0.5f);
        }
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

        contadorMuertes++;
        ActualizarHUDMuertes();
        AudioManager.instance?.FxSoundEffect(sfxMuerte, transform, 1f);

        if (fromVoid)
            StartCoroutine(RutinaReinicio());
        else
            StartCoroutine(RutinaMuerteDramatica());
    }

    public void CollectCrystal()
    {
        cristalesObtenidos++;
        ActualizarHUDCristales();
    }

    // Llamado por CheckpointZone al activarse — guarda posición y cristales actuales
    public void GuardarCheckpoint(Vector3 pos)
    {
        _checkpointPos         = pos;
        _checkpointActivo      = true;
        _checkpointScena       = nivelActual;
        _cristalesEnCheckpoint = cristalesObtenidos;
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
        tiempoAcumulado     += Time.timeSinceLevelLoad;
        cristalesAcumulados += cristalesObtenidos;

        ActualizarPanelFinNivel();

        if (panelFinNivel != null) panelFinNivel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ContinuarSiguienteNivel()
    {
        LimpiarCheckpoint();
        Time.timeScale = 1f;
        if (panelFinNivel != null) panelFinNivel.SetActive(false);

        int siguienteNivel = nivelActual + 1;

        if (siguienteNivel >= totalNiveles)
            MostrarVictoria();
        else
        {
            nivelActual        = siguienteNivel;
            cristalesObtenidos = 0;
            StartCoroutine(CargarEscenaConFade(siguienteNivel));
        }
    }

    public void MostrarVictoria()
    {
        if (panelVictoria != null) panelVictoria.SetActive(true);
        Time.timeScale = 0f;
        ActualizarPanelVictoria();
    }

    // ── PAUSA ─────────────────────────────────────────────────────

    public void TogglePause()
    {
        // No interrumpir el fin de nivel ni la victoria
        if (panelFinNivel != null && panelFinNivel.activeSelf) return;
        if (panelVictoria != null && panelVictoria.activeSelf) return;

        isPaused              = !isPaused;
        Time.timeScale        = isPaused ? 0f : 1f;
        AudioListener.pause   = isPaused;

        if (panelPausa != null) panelPausa.SetActive(isPaused);

        // Cerrar opciones al salir de pausa
        if (!isPaused && panelOpciones != null)
            panelOpciones.SetActive(false);
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
        if (panelVictoria != null) panelVictoria.SetActive(false);

        // Cancelar corrutinas en vuelo (RutinaReinicio, RutinaMuerteDramatica…).
        // Sin esto, una RutinaReinicio pendiente podria cargar la escena de juego
        // de forma asincrona incluso mientras el jugador ya esta en el menu.
        StopAllCoroutines();

        isDying = false;
        StartCoroutine(CargarEscenaConFade(0));
    }

    private void LimpiarCheckpoint()
    {
        _checkpointActivo      = false;
        _checkpointScena       = -1;
        _cristalesEnCheckpoint = 0;
    }

    // Cierra la pausa sin animación (usada antes de cargar escena)
    private void CerrarPausaSilencioso()
    {
        isPaused       = false;
        Time.timeScale = 1f;
        if (panelPausa    != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
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
        cristalesObtenidos = _cristalesEnCheckpoint;
        ActualizarHUDCristales();
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

        cristalesObtenidos      = 0;
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

    private void ActualizarPanelFinNivel()
    {
        if (panelFinNivel == null) return;
        var resumen = panelFinNivel.GetComponentInChildren<TextMeshProUGUI>();
        if (resumen != null)
            resumen.text = $"Tiempo: {Time.timeSinceLevelLoad:F2}s\nCristales: {cristalesObtenidos} / {cristalesTotales}";
    }

    private void ActualizarPanelVictoria()
    {
        if (panelVictoria == null) return;
        var resumen = panelVictoria.GetComponentInChildren<TextMeshProUGUI>();
        if (resumen != null)
            resumen.text = $"Tiempo total: {tiempoAcumulado:F2}s\nCristales: {cristalesAcumulados}";
    }
}
