using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class FragilePlatform : MonoBehaviour
{
    [Header("Tiempos")]
    [SerializeField] private float breakDelay = 1.5f;
    [SerializeField] private float regenDelay = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxCrujido;
    [SerializeField] private AudioClip sfxRotura;

    [Header("Fases visuales (3 colores de advertencia)")]
    [SerializeField] private Color[] warningColors =
    {
        new Color(1f, 1f,  0.3f),   // amarillo
        new Color(1f, 0.5f, 0f),    // naranja
        new Color(1f, 0.1f, 0.1f)   // rojo
    };

    private SpriteRenderer _sr;
    private Collider2D     _col;
    private Color          _originalColor;
    private bool           _isBreaking;

    private void Awake()
    {
        _sr            = GetComponent<SpriteRenderer>();
        _col           = GetComponent<Collider2D>();
        _originalColor = _sr.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isBreaking) return;
        if (!collision.collider.CompareTag("Player")) return;
        StartCoroutine(RutinaRotura());
    }

    private IEnumerator RutinaRotura()
    {
        _isBreaking = true;
        AudioManager.instance?.FxSoundEffect(sfxCrujido, transform, 1f);

        float tiempoFase = breakDelay / Mathf.Max(warningColors.Length, 1);
        foreach (Color color in warningColors)
        {
            _sr.color = color;
            yield return new WaitForSeconds(tiempoFase);
        }

        AudioManager.instance?.FxSoundEffect(sfxRotura, transform, 1f);
        _col.enabled = false;
        _sr.enabled  = false;

        yield return new WaitForSeconds(regenDelay);

        _sr.color    = _originalColor;
        _sr.enabled  = true;
        _col.enabled = true;
        _isBreaking  = false;
    }
}
