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

    private float tiempoAcumulado;
    private int   cristalesAcumulados;
    private bool  isPaused;

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
        ActualizarHUDCristales();
        StartCoroutine(FadeInConEspera());
        if (musicaNivel != null)
            AudioManager.instance?.PlayClip(musicaNivel);
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
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            TogglePause();

        // R reinicia solo en juego (no en pausa ni en fin de nivel)
        if (Input.GetKeyDown(KeyCode.R) && !isPaused)
            Reintentar();
    }

    // ── GAMEPLAY ──────────────────────────────────────────────────

    public void PlayerDied()
    {
        contadorMuertes++;
        ActualizarHUDMuertes();
        AudioManager.instance?.FxSoundEffect(sfxMuerte, transform, 1f);
        StartCoroutine(RutinaReinicio());
    }

    public void CollectCrystal()
    {
        cristalesObtenidos++;
        ActualizarHUDCristales();
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

    // Botón "Reintentar" — reinicia el nivel actual con fade
    public void Reintentar()
    {
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
        CerrarPausaSilencioso();
        StartCoroutine(CargarEscenaConFade(0));
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

        while (!op.isDone)
            yield return null;

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator CargarEscenaConFade(int indice)
    {
        yield return StartCoroutine(FadeOut(tiempoFadeOut));

        AsyncOperation op = SceneManager.LoadSceneAsync(indice);
        op.allowSceneActivation = false;

        while (op.progress < 0.7f)
            yield return null;

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        yield return StartCoroutine(FadeIn());
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
