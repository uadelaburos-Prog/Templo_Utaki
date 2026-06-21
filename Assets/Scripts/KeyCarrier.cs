using UnityEngine;

// Agregar a cualquier enemigo que deba portar una llave.
// La KeyItem puede ser cualquier GO de la escena — NO necesita ser hijo del enemigo.
// Flujo: Start() desvincula la llave y la congela → enemigo muere → SoltarLlave()
//        → la llave cae con física.
[RequireComponent(typeof(SpriteRenderer))]
public class KeyCarrier : MonoBehaviour
{
    [SerializeField] private KeyItem _llave;
    [Tooltip("Posición de la llave relativa al centro visual del sprite. X se invierte según la dirección.")]
    [SerializeField] private Vector2 offsetPortada = new Vector2(0.3f, -0.2f);

    private SpriteRenderer _sr;
    private Rigidbody2D    _llaveRb;
    private SpriteRenderer _llaveSr;

    // ── LIFECYCLE ─────────────────────────────────────────────────

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (_llave == null) return;

        _llaveRb = _llave.GetComponent<Rigidbody2D>();
        _llaveSr = _llave.GetComponent<SpriteRenderer>();

        // Desvincula de la jerarquía — la posición la manejamos con MovePosition en FixedUpdate
        _llave.transform.SetParent(null);
        _llave.IniciarPortadaEnemigo();   // Kinematic, sin collider, Update desactivado
    }

    private void FixedUpdate()
    {
        if (_llave == null || _llaveRb == null) return;

        Vector2 centro = (Vector2)_sr.bounds.center;
        float   signoX = _sr.flipX ? -1f : 1f;
        _llaveRb.MovePosition(centro + new Vector2(offsetPortada.x * signoX, offsetPortada.y));
        if (_llaveSr != null) _llaveSr.flipX = !_sr.flipX;
    }

    // ── PÚBLICO ───────────────────────────────────────────────────

    public void SoltarLlave()
    {
        if (_llave == null) return;
        _llave.enabled = true;   // reactiva Update de KeyItem
        _llave.Soltar();         // → estado EnVuelo, física Dynamic, collider ON
    }
}
