using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableAnchor : MonoBehaviour
{
    [Header("Sprites de daño")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteUnPocoRoto;
    [SerializeField] private Sprite spriteBastanteRoto;
    [SerializeField] private Sprite spriteRompiendose;

    [Header("Tiempos")]
    [SerializeField] private float breakDelay           = 0.6f;
    [SerializeField] private float breakingAnimDuration = 0.4f;

    [Header("Regeneración")]
    [SerializeField] private bool  permanentBreak = false;
    [SerializeField] private float regenDelay     = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxCrujido;
    [SerializeField] private AudioClip sfxRotura;

    private SpriteRenderer _sr;
    private Collider2D     _col;
    private bool           _isBreaking;
    private GrappleScript  _hookedGrapple;

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    // Llamado por GrappleScript al engancharse en este anclaje
    public void OnHooked(GrappleScript grapple)
    {
        _hookedGrapple = grapple;
        if (_isBreaking) return;
        StartCoroutine(RutinaRotura());
    }

    private IEnumerator RutinaRotura()
    {
        _isBreaking = true;
        float phaseDuration = breakDelay / 2f;

        AudioManager.instance?.FxSoundEffect(sfxCrujido, transform, 1f);

        _sr.sprite = spriteUnPocoRoto;
        yield return new WaitForSeconds(phaseDuration);

        _sr.sprite = spriteBastanteRoto;
        yield return new WaitForSeconds(phaseDuration);

        // Retraer el gancho antes de quitar el collider
        _hookedGrapple?.GrappleRetract();
        _hookedGrapple = null;

        AudioManager.instance?.FxSoundEffect(sfxRotura, transform, 1f);
        _col.enabled = false;
        _sr.sprite   = spriteRompiendose;
        yield return new WaitForSeconds(breakingAnimDuration);

        _sr.enabled = false;

        if (permanentBreak)
        {
            Destroy(gameObject);
            yield break;
        }

        yield return new WaitForSeconds(regenDelay);

        _sr.sprite   = spriteNormal;
        _sr.enabled  = true;
        _col.enabled = true;
        _isBreaking  = false;
    }
}
