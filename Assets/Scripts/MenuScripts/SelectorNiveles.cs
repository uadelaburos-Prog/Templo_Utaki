using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Manager del selector de niveles (UI Canvas). Reutiliza el hover/click nativo de los
// Button de Unity: cada botón se resalta al pasar el cursor (transición Highlighted del
// propio Button) y carga su nivel al presionar. Habilita o bloquea cada botón según el
// progreso guardado en ProgresoNiveles, mostrando un candado opcional en los bloqueados.
public class SelectorNiveles : MonoBehaviour
{
    [Serializable]
    private struct BotonNivel
    {
        public Button boton;
        [Tooltip("Build index de la escena del nivel (debe estar en Build Settings).")]
        public int    buildIndex;
        [Tooltip("Image del botón. Su sprite normal es el de DESBLOQUEADO.")]
        public Image  imagen;
        [Tooltip("Sprite que se muestra cuando el nivel está BLOQUEADO (ej: candado).")]
        public Sprite spriteBloqueado;
        [Tooltip("Si está activo, este nivel siempre se puede jugar (ej: Nivel 1), sin importar el progreso.")]
        public bool   siempreDesbloqueado;
    }

    [Header("Niveles (en orden)")]
    [SerializeField] private BotonNivel[] niveles;

    [Header("Transición")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float       tiempoFade = 0.4f;

    // Sprite original (desbloqueado) de cada botón, cacheado tal como viene del editor.
    private Sprite[] _spritesDesbloqueado;

    // Awake corre antes que OnEnable, así guardamos el sprite normal antes de tocarlo.
    private void Awake()
    {
        if (niveles == null) return;
        _spritesDesbloqueado = new Sprite[niveles.Length];
        for (int i = 0; i < niveles.Length; i++)
            if (niveles[i].imagen != null)
                _spritesDesbloqueado[i] = niveles[i].imagen.sprite;
    }

    // Refrescamos cada vez que se muestra el panel, así refleja el progreso recién hecho.
    private void OnEnable() => RefrescarBotones();

    // ── PÚBLICOS ──────────────────────────────────────────────────

    // Aplica el estado de desbloqueo a cada botón y (re)cablea su click.
    public void RefrescarBotones()
    {
        if (niveles == null) return;

        for (int i = 0; i < niveles.Length; i++)
        {
            BotonNivel n = niveles[i];
            if (n.boton == null) continue;

            bool desbloqueado    = n.siempreDesbloqueado || ProgresoNiveles.EstaDesbloqueado(n.buildIndex);
            n.boton.interactable = desbloqueado;

            // Desbloqueado → sprite original del botón; bloqueado → spriteBloqueado.
            if (n.imagen != null)
            {
                Sprite original = _spritesDesbloqueado != null ? _spritesDesbloqueado[i] : n.imagen.sprite;
                n.imagen.sprite = desbloqueado || n.spriteBloqueado == null ? original : n.spriteBloqueado;
            }

            // Limpiamos listeners runtime previos (no afecta los persistentes del Inspector)
            // y cableamos el destino con una captura local para evitar el bug de closure.
            int destino = n.buildIndex;
            n.boton.onClick.RemoveAllListeners();
            n.boton.onClick.AddListener(() => CargarNivel(destino));
        }
    }

    // Carga un nivel por build index (ignora si está bloqueado, por seguridad).
    public void CargarNivel(int buildIndex)
    {
        bool permitido = ProgresoNiveles.EstaDesbloqueado(buildIndex);
        if (!permitido && niveles != null)
            foreach (BotonNivel n in niveles)
                if (n.buildIndex == buildIndex && n.siempreDesbloqueado) { permitido = true; break; }

        if (!permitido) return;
        StartCoroutine(CargarConFade(buildIndex));
    }

    // Reinicia el progreso y refresca la UI (para un botón de opciones/testing).
    public void ReiniciarProgreso()
    {
        ProgresoNiveles.Reiniciar();
        RefrescarBotones();
    }

    // ── PRIVADOS ──────────────────────────────────────────────────

    private IEnumerator CargarConFade(int indice)
    {
        yield return StartCoroutine(FadeOut());
        // No cortamos la música: el nivel destino reclama su pista con crossfade (PlayMusic).
        SceneManager.LoadScene(indice);
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
