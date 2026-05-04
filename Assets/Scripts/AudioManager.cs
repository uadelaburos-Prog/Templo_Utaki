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
            Destroy(gameObject);
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

    private IEnumerator SwapingVolume(AudioClip clip)
    {
        float maxVolumen = audioSource.volume;
        float currentVolumen = audioSource.volume;

        if (audioSource.isPlaying)
        {
            while (currentVolumen > 0)
            {
                currentVolumen -= Time.deltaTime / 4;
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
            currentVolumen += Time.deltaTime / 4;
            audioSource.volume = currentVolumen;
            yield return null;
        }
        currentVolumen = maxVolumen;

        audioSource.volume = currentVolumen; // 100 a 0
    }
}
