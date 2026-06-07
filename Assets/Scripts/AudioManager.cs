using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer m_Mixer;
    [SerializeField] private AudioSource FxObject;
    [SerializeField] private AudioSource audioSource;

    public static AudioManager instance;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
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

    public void SetMasterVolume(float level)
    {
        m_Mixer.SetFloat("Master", Mathf.Log10(level) * 20f);
    }

    public void SetFxVolume(float level)
    {
        m_Mixer.SetFloat("SFX", Mathf.Log10(level) * 20f);
    }

    public void SetMusicVolume(float level)
    {
        m_Mixer.SetFloat("Music", Mathf.Log10(level) * 20f);
    }

    public void FxSoundEffect(AudioClip audioClip, Transform spawnPoint, float volume)
    {
        if (audioClip == null || FxObject == null) return;

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

    public void PlayClip(AudioClip clip)
    {
        StartCoroutine(SwapingVolume(clip));
    }

    public void StopMusic()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    private IEnumerator SwapingVolume(AudioClip clip)
    {
        // Si el volumen esta en 0 (fade-out interrumpido previo), usar 1 como objetivo
        // para evitar que la fade-in nunca se ejecute (while 0 < 0 = falso inmediato).
        float maxVolumen = (audioSource.volume > 0f) ? audioSource.volume : 1f;
        float currentVolumen = audioSource.volume;

        if (audioSource.isPlaying)
        {
            while (currentVolumen > 0)
            {
                currentVolumen -= Time.unscaledDeltaTime / 4;
                audioSource.volume = currentVolumen;
                yield return null;
            }
            currentVolumen = 0;

            audioSource.Stop();
        }
        audioSource.clip = clip;
        audioSource.Play();

        while (currentVolumen < maxVolumen)
        {
            currentVolumen += Time.unscaledDeltaTime / 4;
            audioSource.volume = currentVolumen;
            yield return null;
        }
        currentVolumen = maxVolumen;

        audioSource.volume = currentVolumen; // 100 a 0
    }
}
