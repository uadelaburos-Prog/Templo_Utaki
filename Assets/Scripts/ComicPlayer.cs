using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reproductor de cinemáticas en formato cómic de PÁGINA ACUMULATIVA. Cada viñeta es
/// una Image ya colocada y dimensionada a mano en la escena (layout libre). El script
/// las revela una a una con un fundido de aparición, dejando visibles las anteriores,
/// hasta componer la página completa. Al terminar funde a negro y carga la siguiente
/// escena por NOMBRE (no por índice) para no depender del orden de Build Settings.
///
/// Las viñetas se toman del array 'vinetas' si está asignado; si no, de los hijos
/// directos de 'contenedorVinetas' en orden de jerarquía (más cómodo: solo arrastrás
/// las Images como hijos y las ordenás en el árbol).
///
/// Autónomo: no depende de GameLoopManager (igual que MenuManager). Se usa tanto en la
/// escena Intro (Menú → Intro → Selector) como en la escena Final (N6 → Final → Menú).
/// </summary>
public class ComicPlayer : MonoBehaviour
{
    [Header("Viñetas del cómic")]
    [Tooltip("Viñetas en orden de aparición. Cada una es una Image ya colocada/dimensionada " +
             "en la escena. Si se deja vacío, se toman los hijos de 'contenedorVinetas'.")]
    [SerializeField] private CanvasGroup[] vinetas;

    [Tooltip("Si 'vinetas' está vacío, se auto-rellena con los hijos directos de este " +
             "contenedor, en orden de jerarquía. Arrastrá aquí el objeto que agrupa las viñetas.")]
    [SerializeField] private Transform contenedorVinetas;

    [Header("Ritmo")]
    [Tooltip("Duración del fundido de aparición de cada viñeta (segundos).")]
    [SerializeField] private float fadeVineta = 0.8f;

    [Tooltip("Espera entre la aparición de una viñeta y el inicio de la siguiente (segundos).")]
    [SerializeField] private float tiempoEntreVinetas = 1.2f;

    [Tooltip("Espera tras la última viñeta antes de fundir a negro y salir (segundos).")]
    [SerializeField] private float tiempoFinal = 2f;

    [Header("Interacción")]
    [Tooltip("Permitir saltar toda la cinemática con la tecla indicada.")]
    [SerializeField] private bool permitirSaltar = true;
    [SerializeField] private KeyCode teclaSaltar = KeyCode.Escape;

    [Tooltip("Click izquierdo o Space adelantan la aparición de la siguiente viñeta sin esperar.")]
    [SerializeField] private bool permitirAdelantar = true;

    [Header("Transición de salida")]
    [Tooltip("Nombre EXACTO de la escena a cargar al terminar (debe estar en Build Settings).")]
    [SerializeField] private string escenaSiguiente = "Level Selector";

    [Tooltip("Panel negro a pantalla completa para el fundido a negro antes de cargar.")]
    [SerializeField] private CanvasGroup fadeNegro;

    [Header("Audio (opcional)")]
    [Tooltip("Música de la cinemática. Se reproduce vía AudioManager si está presente.")]
    [SerializeField] private AudioClip musica;

    private CanvasGroup[] _secuencia; // viñetas resueltas, en orden de aparición
    private bool _terminando;         // evita disparar la salida dos veces
    private bool _adelantar;          // pedido de adelantar la espera actual

    private void Start()
    {
        _secuencia = ResolverVinetas();

        // Todas las viñetas arrancan invisibles; se irán revelando en orden.
        foreach (CanvasGroup v in _secuencia)
            if (v != null) v.alpha = 0f;

        if (fadeNegro != null)
        {
            fadeNegro.alpha = 0f;
            fadeNegro.gameObject.SetActive(true);
        }

        if (musica != null && AudioManager.instance != null)
            AudioManager.instance.PlayMusic(musica);

        StartCoroutine(Reproducir());
    }

    private void Update()
    {
        if (_terminando) return;

        if (permitirSaltar && Input.GetKeyDown(teclaSaltar))
        {
            Terminar();
            return;
        }

        if (permitirAdelantar && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            _adelantar = true;
    }

    // Devuelve las viñetas a reproducir: el array explícito si está, o los hijos directos
    // del contenedor en orden de jerarquía (agregándoles CanvasGroup si les falta).
    private CanvasGroup[] ResolverVinetas()
    {
        if (vinetas != null && vinetas.Length > 0) return vinetas;

        if (contenedorVinetas == null) return new CanvasGroup[0];

        var lista = new List<CanvasGroup>();
        foreach (Transform hijo in contenedorVinetas)
        {
            var cg = hijo.GetComponent<CanvasGroup>();
            if (cg == null) cg = hijo.gameObject.AddComponent<CanvasGroup>();
            lista.Add(cg);
        }
        return lista.ToArray();
    }

    private IEnumerator Reproducir()
    {
        if (_secuencia == null || _secuencia.Length == 0)
        {
            Debug.LogWarning("[ComicPlayer] No hay viñetas asignadas ni en el contenedor; se salta la cinemática.");
            Terminar();
            yield break;
        }

        for (int i = 0; i < _secuencia.Length; i++)
        {
            if (_terminando) yield break;

            CanvasGroup vineta = _secuencia[i];
            if (vineta == null) continue;

            // Fundido de aparición — la viñeta queda visible (no se vuelve a ocultar).
            yield return StartCoroutine(Fade(vineta, vineta.alpha, 1f, fadeVineta));

            // Espera antes de la siguiente viñeta (interrumpible por "adelantar").
            bool esUltima = i == _secuencia.Length - 1;
            yield return StartCoroutine(Esperar(esUltima ? tiempoFinal : tiempoEntreVinetas));
        }

        Terminar();
    }

    // Espera 'duracion' segundos en tiempo real, cortando antes si se pide adelantar.
    private IEnumerator Esperar(float duracion)
    {
        _adelantar = false;
        float t = 0f;
        while (t < duracion && !_adelantar && !_terminando)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator Fade(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        if (grupo == null) yield break;

        if (duracion <= 0f)
        {
            grupo.alpha = hasta;
            yield break;
        }

        float t = 0f;
        while (t < duracion && !_terminando)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        grupo.alpha = hasta;
    }

    // Dispara la transición de salida una sola vez.
    private void Terminar()
    {
        if (_terminando) return;
        _terminando = true;
        StartCoroutine(RutinaSalida());
    }

    private IEnumerator RutinaSalida()
    {
        // Fundido a negro (si hay panel dedicado) y carga de la siguiente escena.
        if (fadeNegro != null)
            yield return StartCoroutine(Fade(fadeNegro, fadeNegro.alpha, 1f, fadeVineta));

        if (string.IsNullOrEmpty(escenaSiguiente))
        {
            Debug.LogWarning("[ComicPlayer] 'escenaSiguiente' vacío; volviendo al menú.");
            SceneManager.LoadScene("Menu");
            yield break;
        }

        SceneManager.LoadScene(escenaSiguiente);
    }
}
