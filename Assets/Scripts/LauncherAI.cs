using UnityEngine;

// Setup: Layer "Enemy". Collider2D marcado como Trigger (forzado en Awake).
// El Lanzador no se mueve. Dispara proyectiles en una direccion fija cada [cadencia] segundos.
// Asignar puntoDisparo a un Transform hijo desplazado hacia el canon para evitar autocolision.
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class LauncherAI : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Vector2    direccionDisparo = Vector2.right;
    [SerializeField] private float      cadencia         = 2.5f;
    [SerializeField] private Transform  puntoDisparo;

    [Header("Visual")]
    [SerializeField] private Sprite[] framesCargando;
    [SerializeField] private Sprite[] framesDisparando;
    [SerializeField] private float    tiempoEntreFrames = 0.15f;

    private float timerCadencia;
    private float timerFrame;
    private int   frameActual;
    private bool  disparando;

    private SpriteRenderer sr;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        timerCadencia = 0f;
        MostrarFrame();
    }

    private void Update()
    {
        TickAnimacion(Time.deltaTime);
        TickDisparo(Time.deltaTime);
    }

    private void TickDisparo(float dt)
    {
        timerCadencia += dt;
        if (timerCadencia < cadencia) return;

        timerCadencia = 0f;
        Disparar();
    }

    private void Disparar()
    {
        if (prefabProyectil == null) return;

        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        GameObject go  = Instantiate(prefabProyectil, origen, Quaternion.identity);

        var proj = go.GetComponent<Projectile>();
        if (proj != null)
            proj.Inicializar(direccionDisparo.normalized);

        // Flash visual de disparo: reinicia la animacion en el estado disparando
        disparando   = true;
        frameActual  = 0;
        timerFrame   = 0f;
        MostrarFrame();
    }

    private void TickAnimacion(float dt)
    {
        timerFrame += dt;
        if (timerFrame < tiempoEntreFrames) return;

        timerFrame = 0f;
        Sprite[] ciclo = ObtenerCicloActual();

        if (ciclo == null || ciclo.Length == 0) return;

        frameActual++;
        if (frameActual >= ciclo.Length)
        {
            frameActual = 0;
            disparando  = false; // vuelve a ciclo de carga al terminar el flash
        }
        MostrarFrame();
    }

    private void MostrarFrame()
    {
        Sprite[] ciclo = ObtenerCicloActual();
        if (ciclo == null || ciclo.Length == 0) return;
        int idx   = Mathf.Clamp(frameActual, 0, ciclo.Length - 1);
        sr.sprite = ciclo[idx];
    }

    private Sprite[] ObtenerCicloActual() =>
        disparando && framesDisparando != null && framesDisparando.Length > 0
            ? framesDisparando
            : framesCargando;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Gizmos.DrawRay(origen, (Vector3)direccionDisparo.normalized * 5f);
        Gizmos.DrawWireSphere(origen, 0.1f);
    }
}
