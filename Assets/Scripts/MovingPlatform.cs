using System.Collections.Generic;
using UnityEngine;

// DefaultExecutionOrder(100) garantiza que FixedUpdate corra DESPUÉS de todos los pasajeros (orden 0).
// Así rider.position += _delta se aplica sobre la posición ya procesada por cada objeto.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Trayectoria")]
    [Tooltip("Desplazamiento desde la posición inicial hasta el punto B (unidades).")]
    [SerializeField] private Vector2 moveOffset = new Vector2(3f, 0f);
    [SerializeField] private float speed = 2f;

    [Header("Activación")]
    [Tooltip("Si está desactivado, la plataforma no se moverá hasta recibir la señal.")]
    [SerializeField] private bool enMovimiento = true;

    private Rigidbody2D _rb;
    private Vector2 _pointA;
    private Vector2 _pointB;
    private float _t;
    private float _dir = 1f;
    private Vector2 _delta;
    private List<Rigidbody2D> _riders = new List<Rigidbody2D>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        _pointA = _rb.position;
        _pointB = _rb.position + moveOffset;
    }

    private void FixedUpdate()
    {
        // Si no está activa, detiene la velocidad física y no procesa el movimiento
        if (!enMovimiento)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(_pointA, _pointB);
        if (dist < 0.001f) return;

        _t += _dir * speed * Time.fixedDeltaTime / dist;

        if (_t >= 1f) { _t = 1f; _dir = -1f; }
        else if (_t <= 0f) { _t = 0f; _dir = 1f; }

        Vector2 prevPos = _rb.position;
        Vector2 newPos = Vector2.Lerp(_pointA, _pointB, _t);

        // 1. CALCULAR Y ASIGNAR VELOCIDAD REAL (Crucial para que los pasajeros la detecten)
        _delta = newPos - prevPos;
        _rb.linearVelocity = _delta / Time.fixedDeltaTime;

        // 2. MOVER LA PLATAFORMA
        _rb.MovePosition(newPos);

        // Teleport directo a todos los pasajeros (solo el Player) — independiente de linearVelocity
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            if (_riders[i] == null) { _riders.RemoveAt(i); continue; }
            _riders[i].position += _delta;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        var rb = col.rigidbody;
        // Teletransporta al jugador y a la llave. La llave tiene fricción 0 (ver KeyItem),
        // así no se suma el arrastre físico al teletransporte (evita el doble empuje).
        if (rb != null && rb != _rb && !_riders.Contains(rb) &&
            (col.gameObject.CompareTag("Player") || col.gameObject.CompareTag("Key")))
        {
            _riders.Add(rb);
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.rigidbody != null)
        {
            _riders.Remove(col.rigidbody);
        }
    }

    // ── API Pública para Eventos ──────────────────────────────────

    // Llama a este método desde un UnityEvent (Botón, Palanca, Trigger) para iniciar la plataforma
    public void Arrancar()
    {
        enMovimiento = true;
    }

    // Por si necesitas pausarla en algún punto de tu nivel
    public void Detener()
    {
        enMovimiento = false;
    }

    // ── Gizmos ────────────────────────────────────────────────────

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
