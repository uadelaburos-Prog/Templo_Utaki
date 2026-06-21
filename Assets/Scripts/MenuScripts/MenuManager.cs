using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelOpciones;

    [Header("Transición")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float tiempoFade = 0.4f;

    // ── Botones ───────────────────────────────────────────────────

    public void IniciarJuego()
    {
        StartCoroutine(CargarEscena(1));
    }

    /// <summary>
    /// Carga un nivel específico pasando su índice de Build Settings.
    /// Ideal para usar con botones de selección de nivel en el Inspector.
    /// </summary>
    public void IrANivel(int indice)
    {
        StartCoroutine(CargarEscena(indice));
    }

    public void AbrirOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void Salir()
    {
        StartCoroutine(CargarSalida());
    }

    // ── Transición ────────────────────────────────────────────────

    private IEnumerator CargarEscena(int indice)
    {
        yield return StartCoroutine(FadeOut());
        AudioManager.instance?.StopMusic();
        SceneManager.LoadScene(indice);
    }

    private IEnumerator CargarSalida()
    {
        yield return StartCoroutine(FadeOut());
        AudioManager.instance?.StopMusic();
        Application.Quit();
    }

    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        float t = 0f;
        while (t < tiempoFade)
        {
            t += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Clamp01(t / tiempoFade);
            yield return null;
        }
        fadePanel.alpha = 1f;
    }
}
