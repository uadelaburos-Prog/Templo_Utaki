using UnityEngine;
using UnityEngine.Events;

// El ídolo/reliquia del Nivel 5. Al recogerlo por proximidad (OverlapCircle, igual que
// CrystalPickup) dispara una sola vez el UnityEvent 'alRecoger', desde el que se cablea
// toda la consecuencia: SecuenciaEventos.Disparar() (techo/roca/trampas) y
// PlayerSkinSwapper.CambiarVariante() (cambio de sprites del jugador).
public class RelicaPickup : MonoBehaviour
{
    [Header("Flotación")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed     = 2f;

    [Header("Recolección")]
    [SerializeField] private float     pickupRadius = 0.7f;
    [SerializeField] private LayerMask playerMask;

    [Header("Evento")]
    [Tooltip("Se invoca una sola vez al recoger. Cablear SecuenciaEventos.Disparar(), PlayerSkinSwapper.CambiarVariante(), etc.")]
    [SerializeField] private UnityEvent alRecoger;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxRecoger;

    private Vector3 startPos;
    private float   floatOffset;
    private bool    recogida;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Start()
    {
        startPos    = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (recogida) return;

        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * floatSpeed + floatOffset) * floatAmplitude;

        if (Physics2D.OverlapCircle(transform.position, pickupRadius, playerMask))
            Recoger();
    }

    // ── PRIVADOS ──────────────────────────────────────────────────

    private void Recoger()
    {
        recogida = true;
        GameDebug.Log("[RelicaPickup] Reliquia recogida — disparando secuencia.");
        AudioManager.instance?.FxSoundEffect(sfxRecoger, transform, 1f);
        alRecoger?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.84f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
