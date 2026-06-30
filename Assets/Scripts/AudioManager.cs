using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer m_Mixer;
    [SerializeField] private AudioSource FxObject;
    [SerializeField] private AudioSource audioSource;

    [Header("Música por escena")]
    [Tooltip("Música de TODO el juego, indexada por buildIndex: [0]=menú, [1]=N1, [2..5]=niveles, etc. Asignar EL MISMO clip a varias escenas de la misma zona evita que la música se reinicie al pasar entre ellas. Se configura una sola vez desde el menú (este AudioManager persiste entre escenas).")]
    [SerializeField] private AudioClip[] musicaPorEscena;

    [Header("Volumen — valores por defecto (0–1)")]
    [Tooltip("Volúmenes la PRIMERA vez que se juega. Después mandan los valores guardados en PlayerPrefs (los sliders).")]
    [SerializeField, Range(0f, 1f)] private float volumenMasterDefault = 0.8f;
    [SerializeField, Range(0f, 1f)] private float volumenMusicaDefault = 0.8f;
    [SerializeField, Range(0f, 1f)] private float volumenSfxDefault    = 0.8f;

    public static AudioManager instance;

    // Volúmenes actuales (0–1), persistidos en PlayerPrefs → consistentes en todo el juego.
    private float _volMaster, _volMusic, _volSfx;

    private const string PREF_MASTER = "vol_master";
    private const string PREF_MUSIC  = "vol_music";
    private const string PREF_SFX    = "vol_sfx";

    // Clip de música actualmente activo — permite que PlayMusic sea idempotente:
    // si se vuelve a pedir el mismo clip (recarga por muerte, cambio de nivel dentro
    // de la misma zona), la música continúa en vez de reiniciarse.
    private AudioClip _clipActual;
    private Coroutine _fadeRoutine;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CargarVolumenes();
            SceneManager.sceneLoaded += OnSceneLoaded;
            // Reproducir la música de la escena en la que nace (sceneLoaded no se
            // dispara para la escena ya activa al momento de suscribirse).
            ReproducirMusicaDeEscena(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            // IMPORTANTE: NO destruir el GameObject completo.
            // Este AudioManager puede compartir su GO con MenuManager u otros scripts.
            // Si hacemos Destroy(gameObject) destruimos MenuManager y el boton Play
            // queda apuntando a un componente muerto → no responde al click.
            // Solo silenciamos y destruimos los AudioSources hijos duplicados.
            foreach (AudioSource s in GetComponentsInChildren<AudioSource>(true))
            {
                s.Stop();
                Destroy(s.gameObject);
            }
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReproducirMusicaDeEscena(scene.buildIndex);
    }

    // Elige la pista del array según el buildIndex y la reproduce (idempotente:
    // si es el mismo clip que ya suena —misma zona o recarga por muerte— no reinicia).
    private void ReproducirMusicaDeEscena(int buildIndex)
    {
        if (musicaPorEscena == null || buildIndex < 0 || buildIndex >= musicaPorEscena.Length)
            return;

        // El volumen ya está aplicado al mixer (CargarVolumenes en Awake) y persiste entre
        // escenas — no lo pisamos acá para respetar el valor elegido en el slider.
        PlayMusic(musicaPorEscena[buildIndex]);   // null → detiene la música
    }

    // ── VOLUMEN ───────────────────────────────────────────────────
    // Set* aplican al mixer y persisten en PlayerPrefs. Get* devuelven el valor actual
    // (0–1) para inicializar los sliders. El AudioManager es único y persistente, así
    // que el volumen es el mismo en todos los menús de todo el juego.

    private void CargarVolumenes()
    {
        _volMaster = PlayerPrefs.GetFloat(PREF_MASTER, volumenMasterDefault);
        _volMusic  = PlayerPrefs.GetFloat(PREF_MUSIC,  volumenMusicaDefault);
        _volSfx    = PlayerPrefs.GetFloat(PREF_SFX,    volumenSfxDefault);
        AplicarAlMixer("Master", _volMaster);
        AplicarAlMixer("Music",  _volMusic);
        AplicarAlMixer("SFX",    _volSfx);
    }

    // Convierte 0–1 lineal a dB. 0 → -80dB (silencio real; evita Log10(0) = -∞).
    private void AplicarAlMixer(string parametro, float level)
    {
        if (m_Mixer == null) return;
        float db = level <= 0.0001f ? -80f : Mathf.Log10(level) * 20f;
        m_Mixer.SetFloat(parametro, db);
    }

    public void SetMasterVolume(float level)
    {
        _volMaster = Mathf.Clamp01(level);
        AplicarAlMixer("Master", _volMaster);
        PlayerPrefs.SetFloat(PREF_MASTER, _volMaster);
    }

    public void SetMusicVolume(float level)
    {
        _volMusic = Mathf.Clamp01(level);
        AplicarAlMixer("Music", _volMusic);
        PlayerPrefs.SetFloat(PREF_MUSIC, _volMusic);
    }

    public void SetFxVolume(float level)
    {
        _volSfx = Mathf.Clamp01(level);
        AplicarAlMixer("SFX", _volSfx);
        PlayerPrefs.SetFloat(PREF_SFX, _volSfx);
    }

    public float GetMasterVolume() => _volMaster;
    public float GetMusicVolume()  => _volMusic;
    public float GetFxVolume()     => _volSfx;

    public void FxSoundEffect(AudioClip audioClip, Transform spawnPoint, float volume)
    {
        if (audioClip == null) return;
        if (FxObject == null)
        {
            // Aviso ruidoso: si la instancia persistente (la del menú) no tiene el
            // prefab de SFX asignado, ningún SFX sonará en todo el juego.
            Debug.LogWarning("AudioManager: 'FxObject' sin asignar en la instancia persistente — no sonarán los SFX. Asigná FxObject (y audioSource/m_Mixer) en el AudioManager del menú, o convertilo en prefab.", this);
            return;
        }

        AudioSource source = Instantiate(FxObject, spawnPoint.position, Quaternion.identity);

        //asignacion del clip de audio
        source.clip = audioClip;

        //asignacion de volumen
        source.volume = volume;

        //reproducir el audio
        source.Play();

        //obtener el tamaño del clip de audio
        float clipLenght = source.clip.length;

        //destruir el Objeto
        Destroy(source.gameObject, clipLenght);
    }

    // Reproduce música de fondo con crossfade. IDEMPOTENTE: si el clip pedido ya es el
    // que está sonando, no hace nada (la música continúa). Así, recargar la escena al
    // morir o pasar a otro nivel de la misma zona no reinicia la pista. clip == null
    // detiene la música.
    public void PlayMusic(AudioClip clip, float volumenObjetivo = 1f)
    {
        if (audioSource == null) return;
        if (clip == null) { StopMusic(); return; }

        // Ya suena este clip → continuar sin reiniciar.
        if (clip == _clipActual && audioSource.isPlaying) return;

        _clipActual = clip;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(SwapingVolume(clip, Mathf.Clamp01(volumenObjetivo)));
    }

    // Alias retrocompatible (volumen objetivo 1).
    public void PlayClip(AudioClip clip) => PlayMusic(clip);

    public void StopMusic()
    {
        if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        _clipActual = null;
    }

    private IEnumerator SwapingVolume(AudioClip clip, float volumenObjetivo)
    {
        // Fade-out de la pista anterior (si había alguna sonando).
        float currentVolumen = audioSource.volume;
        if (audioSource.isPlaying)
        {
            while (currentVolumen > 0f)
            {
                currentVolumen -= Time.unscaledDeltaTime / 4f;
                audioSource.volume = Mathf.Max(0f, currentVolumen);
                yield return null;
            }
            audioSource.Stop();
        }

        // Fade-in de la pista nueva desde silencio.
        audioSource.clip   = clip;
        audioSource.volume = 0f;
        audioSource.Play();

        currentVolumen = 0f;
        while (currentVolumen < volumenObjetivo)
        {
            currentVolumen += Time.unscaledDeltaTime / 4f;
            audioSource.volume = Mathf.Min(volumenObjetivo, currentVolumen);
            yield return null;
        }
        audioSource.volume = volumenObjetivo;
        _fadeRoutine = null;
    }
}
