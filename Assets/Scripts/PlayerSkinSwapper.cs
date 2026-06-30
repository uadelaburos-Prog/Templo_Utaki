using UnityEngine;

// En el jugador. Cambia "todos los sprites" a una variante intercambiando el
// RuntimeAnimatorController del Animator: la variante referencia los sprites alternativos
// en los mismos estados/transiciones, por lo que las animaciones se conservan intactas.
//
// Setup: en Unity, duplicar el Animator Controller del jugador, reemplazar los sprites de
// cada clip por los de la variante y asignar ese controller a 'controllerVariante'.
[RequireComponent(typeof(Animator))]
public class PlayerSkinSwapper : MonoBehaviour
{
    [Header("Variante")]
    [Tooltip("Controller alternativo con los sprites de la variante en los mismos estados.")]
    [SerializeField] private RuntimeAnimatorController controllerVariante;
    [Tooltip("Aplicar un tinte al SpriteRenderer al activar la variante.")]
    [SerializeField] private bool  aplicarTinte  = false;
    [SerializeField] private Color tinteVariante = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxTransformacion;

    private Animator                  anim;
    private SpriteRenderer            sr;
    private RuntimeAnimatorController controllerOriginal;
    private Color                     colorOriginal = Color.white;
    private bool                      enVariante;

    public bool EnVariante => enVariante;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        anim               = GetComponent<Animator>();
        sr                 = GetComponent<SpriteRenderer>();
        controllerOriginal = anim.runtimeAnimatorController;
        if (sr != null) colorOriginal = sr.color;
    }

    // ── PÚBLICOS ──────────────────────────────────────────────────

    public void CambiarVariante()
    {
        if (enVariante || controllerVariante == null) return;
        enVariante = true;

        anim.runtimeAnimatorController = controllerVariante;
        if (aplicarTinte && sr != null) sr.color = tinteVariante;

        AudioManager.instance?.FxSoundEffect(sfxTransformacion, transform, 1f);
        GameDebug.Log("[PlayerSkinSwapper] Variante activada.");
    }

    public void RestaurarOriginal()
    {
        if (!enVariante) return;
        enVariante = false;

        anim.runtimeAnimatorController = controllerOriginal;
        if (sr != null) sr.color = colorOriginal;
    }

    public void Alternar()
    {
        if (enVariante) RestaurarOriginal();
        else            CambiarVariante();
    }
}
