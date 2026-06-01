using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class FragilePlatform : MonoBehaviour
{
    [Header("Sprites de daño")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteUnPocoRota;
    [SerializeField] private Sprite spriteBastanteRota;
    [SerializeField] private Sprite spriteRompiendose;

    [Header("Tiempos")]
    [SerializeField] private float breakDelay           = 1.2f;  // tiempo entre fase 1→2→3 (dividido en 2 fases iguales)
    [SerializeField] private float breakingAnimDuration = 0.4f;  // cuánto dura el sprite "Rompiéndose" antes de desaparecer
    [SerializeField] private float regenDelay           = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxCrujido;
    [SerializeField] private AudioClip sfxRotura;

    private SpriteRenderer _sr;
    private Collider2D     _col;
    private bool           _isBreaking;

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isBreaking) return;
        if (!collision.collider.CompareTag("Player")) return;
        if (collision.collider.bounds.min.y < _col.bounds.max.y - 0.1f) return;
        StartCoroutine(RutinaRotura());
    }

    private IEnumerator RutinaRotura()
    {
        _isBreaking = true;
        float phaseDuration = breakDelay / 2f;

        AudioManager.instance?.FxSoundEffect(sfxCrujido, transform, 1f);

        _sr.sprite = spriteUnPocoRota;
        yield return new WaitForSeconds(phaseDuration);

        _sr.sprite = spriteBastanteRota;
        yield return new WaitForSeconds(phaseDuration);

        AudioManager.instance?.FxSoundEffect(sfxRotura, transform, 1f);
        _col.enabled = false;
        _sr.sprite   = spriteRompiendose;
        yield return new WaitForSeconds(breakingAnimDuration);

        _sr.enabled = false;
        yield return new WaitForSeconds(regenDelay);

        _sr.sprite   = spriteNormal;
        _sr.enabled  = true;
        _col.enabled = true;
        _isBreaking  = false;
    }
}
