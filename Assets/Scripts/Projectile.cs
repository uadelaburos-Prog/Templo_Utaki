using UnityEngine;

// Proyectil del Lanzador de Proyectiles.
// Se mueve en linea recta a velocidad constante.
// Se destruye al impactar al jugador, a la Momia, a geometria solida, o al agotar su vida.
// Atraviesa al Guerrero Espectral (su collider es trigger, no activa la rama de geometria solida).
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad   = 8f;
    [SerializeField] private float tiempoVida  = 10f;

    [Header("Visual")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float    tiempoEntreFrames = 0.1f;

    private Vector2        direccion;
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private float          timerVida;
    private float          timerFrame;
    private int            frameActual;

    private void Awake()
    {
        rb            = GetComponent<Rigidbody2D>();
        sr            = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.bodyType     = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>Llamar inmediatamente despues de Instantiate para fijar la direccion.</summary>
    public void Inicializar(Vector2 dir)
    {
        direccion = dir.normalized;

        // Rota el sprite para apuntar en la direccion de vuelo.
        float angulo  = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }

    private void FixedUpdate()
    {
        timerVida += Time.fixedDeltaTime;
        if (timerVida >= tiempoVida)
        {
            Destroy(gameObject);
            return;
        }
        rb.MovePosition(rb.position + direccion * velocidad * Time.fixedDeltaTime);
    }

    private void Update()
    {
        TickAnimacion(Time.deltaTime);
    }

    private void TickAnimacion(float dt)
    {
        if (frames == null || frames.Length == 0) return;

        timerFrame += dt;
        if (timerFrame < tiempoEntreFrames) return;

        timerFrame  = 0f;
        frameActual = (frameActual + 1) % frames.Length;
        sr.sprite   = frames[frameActual];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameLoopManager.Instance?.PlayerDied();
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("MummyEnemy"))
        {
            var momia = other.GetComponent<MummyAI>();
            if (momia != null) momia.Morir();
            else               Destroy(other.gameObject);
            Destroy(gameObject);
            return;
        }

        // Geometria solida (suelo, paredes, plataformas): no es trigger → destruye el proyectil.
        // Triggers (Espectral, HazardFire, etc.) son ignorados — el proyectil los atraviesa.
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
