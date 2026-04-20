using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoopManager : MonoBehaviour
{
    [HideInInspector] public static GameLoopManager Instance { get; private set; }

    [Header("Cristales")]
    int cristalesObtenidos = 0;
    [SerializeField] int cristalesTotales;

    [Header("Muerte / Reinicio")]
    [SerializeField] int   contadorMuertes = 0;
    [SerializeField] float tiempoFadeOut   = 0.4f;
    [SerializeField] float tiempoReinicio  = 1.5f;

    [Header("Nivel")]
    [SerializeField] int nivelActual;
    [SerializeField] int totalNiveles;

    [Header("UI")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private TMP_Text    txtCristales;
    [SerializeField] private TMP_Text    txtMuertes;
    [SerializeField] private GameObject  panelFinNivel;
    [SerializeField] private GameObject  panelPausa;
    [SerializeField] private GameObject  panelVictoria;

    private float tiempoAcumulado;
    private int   cristalesAcumulados;
    private bool  isPaused;

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
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void PlayerDied()
    {
        contadorMuertes++;
        ActualizarHUDMuertes();
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

        panelFinNivel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ContinuarSiguienteNivel()
    {
        Time.timeScale = 1f;
        panelFinNivel.SetActive(false);

        int siguienteNivel = nivelActual + 1;

        if (siguienteNivel >= totalNiveles)
        {
            MostrarVictoria();
        }
        else
        {
            nivelActual        = siguienteNivel;
            cristalesObtenidos = 0;
            StartCoroutine(CargarEscenaConFade(siguienteNivel));
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        Time.timeScale = isPaused ? 0f : 1f;
        panelPausa.SetActive(isPaused);
    }

    public void LeaveOut()
    {
        Application.Quit();
    }

    public void MostrarVictoria()
    {
        panelVictoria.SetActive(true);
        Time.timeScale = 0f;

        ActualizarPanelVictoria();
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
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 1f;

        yield return new WaitForSecondsRealtime(0.1f);

        yield return StartCoroutine(FadeIn());
    }

    private void ActualizarHUDCristales()
    {
        if (txtCristales != null)
            txtCristales.text = $"{cristalesObtenidos} / {cristalesTotales}";
    }

    private void ActualizarHUDMuertes()
    {
        if (txtMuertes != null)
            txtMuertes.text = $"Muertes: {contadorMuertes}";
    }

    private void ActualizarPanelFinNivel()
    {
        var resumen = panelFinNivel.GetComponentInChildren<TextMeshProUGUI>();
        if (resumen != null)
            resumen.text = $"Tiempo: {tiempoAcumulado:F2}s\nCristales: {cristalesObtenidos} / {cristalesTotales}";
    }

    private void ActualizarPanelVictoria()
    {
        var resumen = panelVictoria.GetComponentInChildren<TextMeshProUGUI>();
        if (resumen != null)
            resumen.text = $"¡Felicidades!\nTiempo total: {tiempoAcumulado:F2}s\nCristales totales: {cristalesAcumulados}";
    }
}
