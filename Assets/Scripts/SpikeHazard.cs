using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpikeHazard : MonoBehaviour
{
    public enum SpikeMode { Estatico, Retractil }

    [Header("Modo")]
    [SerializeField] private SpikeMode modo = SpikeMode.Estatico;

    [Header("Retráctil — tiempos")]
    [Tooltip("Segundos que el pincho permanece extendido.")]
    [SerializeField] private float tiempoExtendido = 2f;
    [Tooltip("Segundos que el pincho permanece retraído.")]
    [SerializeField] private float tiempoRetraido  = 1.5f;

    [Header("Retráctil — movimiento")]
    [Tooltip("Velocidad de extensión/retracción (u/s).")]
    [SerializeField] private float velocidadMover  = 5f;
    [Tooltip("Desplazamiento Y al retraerse (negativo = baja hacia el suelo).")]
    [SerializeField] private float desplazamiento  = -0.6f;
    [Tooltip("0–1: fracción del ciclo para desfasar pinchos entre sí.")]
    [SerializeField, Range(0f, 1f)] private float faseInicial = 0f;
    [Tooltip("Segundos que el pincho espera retraído antes de su primera salida.")]
    [SerializeField] private float delayInicial = 0f;

    [Header("Aviso previo a salir")]
    [Tooltip("Segundos antes de extenderse en que el sprite parpadea.")]
    [SerializeField] private float tiempoAviso = 0.25f;
    [SerializeField] private Color colorAviso  = new Color(1f, 0.3f, 0.3f);


    private enum Estado { Extendido, Retractando, Retraido, Extendiendo }

    private Estado         estado;
    private float          timer;
    private Vector2        posExtendida;
    private Vector2        posRetraida;
    private Rigidbody2D    rb;
    private Collider2D     col;
    private SpriteRenderer sr;

    public void Init(SpikeMode nuevoModo, float tExtendido, float tRetraido,
                     float vel, float desp, float fase, float delay = 0f)
    {
        modo            = nuevoModo;
        tiempoExtendido = tExtendido;
        tiempoRetraido  = tRetraido;
        velocidadMover  = vel;
        desplazamiento  = desp;
        faseInicial     = fase;
        delayInicial    = delay;
    }

    private void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        sr  = GetComponentInChildren<SpriteRenderer>();

        posExtendida = transform.position;
        posRetraida  = posExtendida + Vector2.up * desplazamiento;

        if (modo == SpikeMode.Retractil)
        {
            if (delayInicial > 0f)
            {
                estado               = Estado.Retraido;
                timer                = delayInicial;
                rb.position          = posRetraida;
                if (col) col.enabled = false;
            }
            else
            {
                float ciclo = tiempoExtendido + tiempoRetraido;
                float t     = faseInicial * ciclo;

                if (t < tiempoExtendido)
                {
                    estado = Estado.Extendido;
                    timer  = tiempoExtendido - t;
                }
                else
                {
                    estado = Estado.Retraido;
                    timer  = ciclo - t;
                    rb.position      = posRetraida;
                    if (col) col.enabled = false;
                }
            }
        }
    }

    private void Update()
    {
        if (modo == SpikeMode.Estatico) return;

        timer -= Time.deltaTime;

        switch (estado)
        {
            case Estado.Extendido:
                if (timer <= 0f)
                    estado = Estado.Retractando;
                break;

            case Estado.Retraido:
                if (tiempoAviso > 0f && timer <= tiempoAviso)
                {
                    if (sr != null) sr.color = colorAviso;
                    rb.position = posExtendida;
                }
                else if (sr != null)
                {
                    sr.color = Color.white;
                }
                if (timer <= 0f)
                {
                    if (sr != null) sr.color = Color.white;
                    if (col) col.enabled = true;
                    if (tiempoAviso > 0f)
                    {
                        estado = Estado.Extendido;
                        timer  = tiempoExtendido;
                    }
                    else
                    {
                        estado = Estado.Extendiendo;
                    }
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (modo == SpikeMode.Estatico) return;

        switch (estado)
        {
            case Estado.Retractando:
                rb.MovePosition(Vector2.MoveTowards(rb.position, posRetraida, velocidadMover * Time.fixedDeltaTime));
                if (Vector2.Distance(rb.position, posRetraida) < 0.01f)
                {
                    rb.position          = posRetraida;
                    if (col) col.enabled = false;
                    estado               = Estado.Retraido;
                    timer                = tiempoRetraido;
                }
                break;

            case Estado.Extendiendo:
                rb.MovePosition(Vector2.MoveTowards(rb.position, posExtendida, velocidadMover * Time.fixedDeltaTime));
                if (Vector2.Distance(rb.position, posExtendida) < 0.01f)
                {
                    rb.position = posExtendida;
                    estado      = Estado.Extendido;
                    timer       = tiempoExtendido;
                }
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            modo = SpikeMode.Estatico;
            if (col) col.enabled = false;
            GameLoopManager.Instance.PlayerDied();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        if (modo == SpikeMode.Retractil)
        {
            Vector3 dest = transform.position + Vector3.up * desplazamiento;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireSphere(dest, 0.2f);
            Gizmos.DrawLine(transform.position, dest);
        }
    }
}
