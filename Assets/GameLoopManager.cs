using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoopManager : MonoBehaviour
{
    [HideInInspector] public static GameLoopManager Instance { get; private set; }

    [Header("Cristales")]
    int cristalesObtenidos = 0;
    [SerializeField] int cristalesTotales;

    [Header("Muerte / Reinicio")]
    [SerializeField] int contadorMuertes = 0;
    float tiempoFadeOut = 0.1f;
    float tiempoReinicio = 0f;

    [Header("Nivel")]
    [SerializeField] int nivelActual;
    [SerializeField] int totalNiveles;

    [Header("UI")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private TMP_Text txtCristales;
    [SerializeField] private TMP_Text txtMuertes;
    [SerializeField] private GameObject panelFinNivel;
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelVictoria;

    private float tiempoAcumulado;
    private int CristalesTotales;
    private bool isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
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
        ActualizarHUBCristales();
        StartCoroutine(FadeInConEspera());
    }

    private void Start()
    {
        nivelActual = SceneManager.GetActiveScene().buildIndex;
        totalNiveles = SceneManager.sceneCountInBuildSettings; // Restamos 1 para excluir la escena de menú

        txtCristales.text = $"{cristalesObtenidos} / {cristalesTotales}";
        txtMuertes.text = $"Muertes: {contadorMuertes}";
    }

    public void PlayerDied()
    {
        contadorMuertes++;
        ActualizarHUBMuertes();
        StartCoroutine(RutinaReinicio());
    }

    public void CollectCrystal()
    {
        cristalesObtenidos++;
        ActualizarHUBCristales();

        if (cristalesObtenidos >= cristalesTotales)
        {
            NivelCompleto();
        }
    }

    public void NivelCompleto()
    {
        tiempoAcumulado += Time.timeSinceLevelLoad;
        CristalesTotales += cristalesObtenidos;

        ActualizarPanelFinNivel();

        panelFinNivel.SetActive(true);
        Time.timeScale = 0f; // Pausa el juego
    }

    public void ContinuarSiguienteNivel()
    {
        Time.timeScale = 1f; // Reanuda el juego
        panelFinNivel.SetActive(false);

        int siguienteNivel = nivelActual + 1;

        if (siguienteNivel >= totalNiveles)
        {
            MostrarVictoria();
        }
        else
        {
            nivelActual = siguienteNivel;
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
        Time.timeScale = 0f; // Pausa el juego

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

        yield return null;

        cristalesObtenidos = 0;
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
        // Mantener pantalla en negro mientras Unity termina de renderizar
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 1f;

        yield return new WaitForSecondsRealtime(0.1f);

        yield return StartCoroutine(FadeIn());
    }

    private void ActualizarHUBCristales()
    {
        txtCristales.text = $"{cristalesObtenidos} / {cristalesTotales}";
    }  
    
    private void ActualizarHUBMuertes()
    {
        txtMuertes.text = $"Muertes: {contadorMuertes}";
    }

    private void ActualizarPanelFinNivel()
    {
       TMPro.TextMeshProUGUI resumen = panelFinNivel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
       if(resumen != null)
       {
           resumen.text = $"Tiempo: {tiempoAcumulado:F2} segundos\nCristales: {cristalesObtenidos} / {cristalesTotales}";
       }
    }   

    private void ActualizarPanelVictoria()
    {
        TMPro.TextMeshProUGUI resumen = panelVictoria.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (resumen != null)
        {
            resumen.text = $"¡Felicidades!\nTiempo total: {tiempoAcumulado:F2} segundos\nCristales totales: {CristalesTotales}";
        }
    }
}
