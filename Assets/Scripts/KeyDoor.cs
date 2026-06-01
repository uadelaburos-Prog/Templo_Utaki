using System.Collections;
using UnityEngine;

// Setup:
//   - Collider2D sólido (bloquea el paso físicamente) — isTrigger=false
//   - SpriteRenderer con el sprite cerrado por defecto
//   - Inspector: asignar llaveVinculada, radioApertura y los 5 spritesApertura
//   - El reinicio de escena (GameLoopManager) resetea automáticamente el estado
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class KeyDoor : MonoBehaviour
{
    [Header("Vínculo")]
    [SerializeField] private KeyItem llaveVinculada;
    [SerializeField] private float   radioApertura = 2f;

    [Header("Animación de apertura")]
    [Tooltip("5 sprites en orden: frame 0 = cerrada, frame 4 = completamente abierta.")]
    [SerializeField] private Sprite[] spritesApertura;
    [Tooltip("Tiempo que dura cada frame en segundos.")]
    [SerializeField] private float    duracionFrame = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxApertura;

    private SpriteRenderer _sr;
    private Collider2D     _col;
    private Transform      _player;
    private bool           _abierta;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null) _player = playerGo.transform;
    }

    private void Update()
    {
        if (_abierta || _player == null || llaveVinculada == null) return;

        if (llaveVinculada.EsPortada &&
            Vector2.Distance(transform.position, _player.position) <= radioApertura)
        {
            Abrir();
        }
    }

    // ── Apertura ──────────────────────────────────────────────────

    private void Abrir()
    {
        _abierta     = true;
        _col.enabled = false; // el jugador puede pasar de inmediato
        AudioManager.instance?.FxSoundEffect(sfxApertura, transform, 1f);
        llaveVinculada.Consumir();
        StartCoroutine(RutinaApertura());
    }

    private IEnumerator RutinaApertura()
    {
        if (spritesApertura == null || spritesApertura.Length == 0)
        {
            gameObject.SetActive(false);
            yield break;
        }

        int     total      = spritesApertura.Length;
        float   altoPuerta = _sr.bounds.size.y;
        Vector2 posInicial = transform.position;
        // Evita división por cero si por error solo hay 1 sprite
        float   divisor    = Mathf.Max(total - 1, 1);

        for (int i = 0; i < total; i++)
        {
            if (spritesApertura[i] != null)
                _sr.sprite = spritesApertura[i];

            // Desplazamiento proporcional al frame: frame 0 = sin mover, frame final = altura completa
            transform.position = posInicial + Vector2.up * (altoPuerta * i / divisor);

            yield return new WaitForSeconds(duracionFrame);
        }

        gameObject.SetActive(false);
    }

    // ── Gizmos ────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radioApertura);

        if (llaveVinculada != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);
            Gizmos.DrawLine(transform.position, llaveVinculada.transform.position);
        }
    }
}
