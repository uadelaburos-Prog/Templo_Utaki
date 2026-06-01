using UnityEngine;

// Setup:
//   - Collider2D sólido (bloquea el paso físicamente) — isTrigger=false
//   - Inspector: asignar llaveVinculada (KeyItem del nivel) y radioApertura
//   - El reinicio de escena (GameLoopManager) resetea automáticamente el estado
[RequireComponent(typeof(Collider2D))]
public class KeyDoor : MonoBehaviour
{
    [Header("Vínculo")]
    [SerializeField] private KeyItem llaveVinculada;
    [SerializeField] private float   radioApertura = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxApertura;

    private Transform _player;
    private bool      _abierta;

    // ── Lifecycle ─────────────────────────────────────────────────

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
        _abierta = true;
        AudioManager.instance?.FxSoundEffect(sfxApertura, transform, 1f);
        llaveVinculada.Consumir();
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
