using UnityEngine;

// DefaultExecutionOrder(100) garantiza que FixedUpdate corra DESPUÉS de PlayerMovement (orden 0).
// Así _playerRb.position += _delta se aplica sobre la posición ya procesada por el jugador.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Trayectoria")]
    [Tooltip("Desplazamiento desde la posición inicial hasta el punto B (unidades).")]
    [SerializeField] private Vector2 moveOffset = new Vector2(3f, 0f);
    [SerializeField] private float   speed      = 2f;

    private Rigidbody2D _rb;
    private Vector2     _pointA;
    private Vector2     _pointB;
    private float       _t;
    private float       _dir = 1f;
    private Vector2     _delta;
    private Rigidbody2D _playerRb;

    private void Awake()
    {
        _rb          = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        _pointA = _rb.position;
        _pointB = _rb.position + moveOffset;
    }

    private void FixedUpdate()
    {
        float dist = Vector2.Distance(_pointA, _pointB);
        if (dist < 0.001f) return;

        _t += _dir * speed * Time.fixedDeltaTime / dist;

        if      (_t >= 1f) { _t = 1f; _dir = -1f; }
        else if (_t <= 0f) { _t = 0f; _dir =  1f; }

        Vector2 prevPos = _rb.position;
        Vector2 newPos  = Vector2.Lerp(_pointA, _pointB, _t);

        _rb.MovePosition(newPos);
        _delta = newPos - prevPos;

        // Teleport directo al jugador — independiente de linearVelocity, no puede ser sobreescrito
        if (_playerRb != null)
            _playerRb.position += _delta;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Player"))
            _playerRb = col.rigidbody;
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.collider.CompareTag("Player"))
            _playerRb = null;
    }

    private void OnDrawGizmos()
    {
        Vector2 a = Application.isPlaying ? _pointA : (Vector2)transform.position;
        Vector2 b = Application.isPlaying ? _pointB : (Vector2)transform.position + moveOffset;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.15f);
        Gizmos.DrawWireSphere(b, 0.15f);
    }
}
