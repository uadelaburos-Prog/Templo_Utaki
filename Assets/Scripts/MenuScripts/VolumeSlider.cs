using UnityEngine;
using UnityEngine.UI;

// Conecta un Slider de UI con el AudioManager para controlar el volumen de un canal.
// Colocar en el GameObject del Slider y elegir el canal en el Inspector.
// El AudioManager es único y persistente, así que cualquier slider (menú principal,
// pausa, etc.) muestra y modifica el MISMO volumen → consistente en todo el juego.
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public enum Canal { Master, Musica, SFX }

    [Tooltip("Qué volumen controla este slider.")]
    [SerializeField] private Canal canal = Canal.Master;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void OnEnable()
    {
        // Al abrir el menú, reflejar el volumen actual SIN disparar el callback
        // (evita re-guardar el mismo valor o pisar el estado al solo mostrarlo).
        if (AudioManager.instance != null)
            slider.SetValueWithoutNotify(LeerVolumenActual());

        slider.onValueChanged.AddListener(AlCambiar);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(AlCambiar);
    }

    private float LeerVolumenActual()
    {
        switch (canal)
        {
            case Canal.Musica: return AudioManager.instance.GetMusicVolume();
            case Canal.SFX:    return AudioManager.instance.GetFxVolume();
            default:           return AudioManager.instance.GetMasterVolume();
        }
    }

    private void AlCambiar(float valor)
    {
        if (AudioManager.instance == null) return;

        switch (canal)
        {
            case Canal.Musica: AudioManager.instance.SetMusicVolume(valor); break;
            case Canal.SFX:    AudioManager.instance.SetFxVolume(valor);    break;
            default:           AudioManager.instance.SetMasterVolume(valor); break;
        }
    }
}
