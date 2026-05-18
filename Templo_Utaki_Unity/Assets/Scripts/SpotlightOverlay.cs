using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Efecto reflector de muerte: pantalla negra con un círculo despejado alrededor del jugador.
// Setup: RawImage que llene el Canvas → asignarle el material con shader Custom/SpotlightOverlay.
[RequireComponent(typeof(RawImage))]
public class SpotlightOverlay : MonoBehaviour
{
    [Header("Spotlight")]
    [Tooltip("Radio del círculo visible como fracción de la altura de pantalla (0.18 ≈ ~195px en 1080p).")]
    [SerializeField] private float radius    = 0.18f;
    [Tooltip("Suavidad del borde del círculo.")]
    [SerializeField] private float softness  = 0.10f;
    [Tooltip("Oscuridad máxima de la zona negra (0-1).")]
    [SerializeField] private float maxAlpha  = 0.92f;

    private static readonly int PropPlayerViewport = Shader.PropertyToID("_PlayerViewport");
    private static readonly int PropRadius         = Shader.PropertyToID("_Radius");
    private static readonly int PropSoftness       = Shader.PropertyToID("_Softness");
    private static readonly int PropMaxAlpha       = Shader.PropertyToID("_MaxAlpha");

    private RawImage  _image;
    private Material  _mat;
    private Transform _playerTransform;
    private Camera    _cam;
    private float     _currentAlpha;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _image = GetComponent<RawImage>();
        // Instanciar el material para no modificar el asset compartido
        _mat         = new Material(_image.material);
        _image.material = _mat;
        _cam         = Camera.main;

        _mat.SetFloat(PropMaxAlpha, 0f);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_playerTransform != null && _cam != null)
        {
            Vector3 vp = _cam.WorldToViewportPoint(_playerTransform.position);
            _mat.SetVector(PropPlayerViewport, new Vector4(vp.x, vp.y, 0f, 0f));
        }

        _mat.SetFloat(PropRadius,   radius);
        _mat.SetFloat(PropSoftness, softness);
        _mat.SetFloat(PropMaxAlpha, _currentAlpha);
    }

    // ── API pública ───────────────────────────────────────────────

    public void Activate(Transform player)
    {
        _playerTransform = player;
        _currentAlpha    = 0f;
        _mat.SetFloat(PropMaxAlpha, 0f);
        gameObject.SetActive(true);
    }

    public IEnumerator FadeIn(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t             += Time.unscaledDeltaTime;
            _currentAlpha  = Mathf.Lerp(0f, maxAlpha, t / duration);
            yield return null;
        }
        _currentAlpha = maxAlpha;
    }

    public IEnumerator FadeOut(float duration)
    {
        float start = _currentAlpha;
        float t     = 0f;
        while (t < duration)
        {
            t             += Time.unscaledDeltaTime;
            _currentAlpha  = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        _currentAlpha = 0f;
        gameObject.SetActive(false);
    }
}
