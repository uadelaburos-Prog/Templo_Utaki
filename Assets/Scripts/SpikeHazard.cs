using UnityEngine;

// Estados visuales del ciclo retráctil:
//   Retraido     → oculto bajo el suelo, sin daño
//   Asomando     → punta visible (fraccionAsomando del desplazamiento), sin daño — aviso físico
//   Desplegado   → completamente fuera, CON daño
[RequireComponent(typeof(Rigidbody2D))]
public class SpikeHazard : MonoBehaviour
{
    public enum SpikeMode { Estatico, Retractil }

    [Header("Modo")]
    [SerializeField] private SpikeMode modo = SpikeMode.Estatico;

    [Header("Retráctil — tiempos")]
    [Tooltip("Segundos completamente oculto.")]
    [SerializeField] private float tiempoRetraido  = 1.5f;
    [Tooltip("Segundos con la punta visible (aviso, sin daño).")]
    [SerializeField] private float tiempoAsomando  = 0.4f;
    [Tooltip("Segundos completamente desplegado (con daño).")]
    [SerializeField] private float tiempoExtendido = 1.5f;

    [Header("Retráctil — movimiento")]
    [Tooltip("Velocidad de extensión y retracción (u/s).")]
    [SerializeField] private float velocidadMover = 5f;
    [Tooltip("Desplazamiento total al retraerse (negativo = baja hacia el suelo).")]
    [SerializeField] private float desplazamiento = -0.6f;
    [Tooltip("Fracción del desplazamiento visible en estado Asomando (0.05–0.5).")]
    [SerializeField, Range(0.05f, 0.5f)] private float fraccionAsomando = 0.25f;
    [Tooltip("Desfase del ciclo (0–1). Desincroniza pinchos del mismo grupo.")]
    [SerializeField, Range(0f, 1f)] private float faseInicial  = 0f;
    [Tooltip("Espera inicial antes del primer ciclo (anula faseInicial si > 0).")]
    [SerializeField] private float delayInicial = 0f;

    // Estados internos: los 3 de espera + 3 de movimiento entre ellos
    private enum Estado { Retraido, SaliendoTip, Asomando, SaliendoFull, Desplegado, Retractando }

    private Estado      _estado;
    private float       _timer;
    private Vector2     _posExtendida;
    private Vector2     _posAsomando;
    private Vector2     _posRetraida;
    private Rigidbody2D _rb;
    private Collider2D  _col;

    // ── Init (llamado por SpikeGroup) ────────────────────────────────
    public void Init(SpikeMode nuevoModo, float tRetraido, float tAsomando, float tExtendido,
                     float vel, float desp, float fracAsomando, float fase, float delay = 0f)
    {
        modo             = nuevoModo;
        tiempoRetraido   = tRetraido;
        tiempoAsomando   = tAsomando;
        tiempoExtendido  = tExtendido;
        velocidadMover   = vel;
        desplazamiento   = desp;
        fraccionAsomando = fracAsomando;
        faseInicial      = fase;
        delayInicial     = delay;
    }

    // ── Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        _rb              = GetComponent<Rigidbody2D>();
        _rb.bodyType     = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
    }

    private void Start()
    {
        _col          = GetComponent<Collider2D>();
        _posExtendida = _rb.position;
        _posRetraida  = _posExtendida + Vector2.up * desplazamiento;
        _posAsomando  = Vector2.Lerp(_posRetraida, _posExtendida, fraccionAsomando);

        if (modo == SpikeMode.Retractil)
            IniciarCiclo();
    }

    private void IniciarCiclo()
    {
        if (delayInicial > 0f) { EntrarRetraido(delayInicial); return; }

        float ciclo = tiempoRetraido + tiempoAsomando + tiempoExtendido;
        float t     = faseInicial * ciclo;

        if (t < tiempoRetraido)
        {
            EntrarRetraido(tiempoRetraido - t);
        }
        else if (t < tiempoRetraido + tiempoAsomando)
        {
            _rb.position = _posAsomando;
            EntrarAsomando(tiempoRetraido + tiempoAsomando - t);
        }
        else
        {
            _rb.position = _posExtendida;
            EntrarDesplegado(ciclo - t);
        }
    }

    // ── Transiciones ────────────────────────────────────────────────
    private void EntrarRetraido(float duracion)
    {
        _estado    = Estado.Retraido;
        _timer     = duracion;
        _rb.position       = _posRetraida;
        if (_col) _col.enabled = false;
    }

    private void EntrarAsomando(float duracion)
    {
        _estado = Estado.Asomando;
        _timer  = duracion;
        if (_col) _col.enabled = false;
    }

    private void EntrarDesplegado(float duracion)
    {
        _estado = Estado.Desplegado;
        _timer  = duracion;
        if (_col) _col.enabled = true;
    }

    // ── Update — temporizadores de espera ───────────────────────────
    private void Update()
    {
        if (modo == SpikeMode.Estatico) return;

        switch (_estado)
        {
            case Estado.Retraido:
                _timer -= Time.deltaTime;
                if (_timer <= 0f) _estado = Estado.SaliendoTip;
                break;

            case Estado.Asomando:
                _timer -= Time.deltaTime;
                if (_timer <= 0f) _estado = Estado.SaliendoFull;
                break;

            case Estado.Desplegado:
                _timer -= Time.deltaTime;
                if (_timer <= 0f) _estado = Estado.Retractando;
                break;
        }
    }

    // ── FixedUpdate — movimiento entre posiciones ───────────────────
    private void FixedUpdate()
    {
        if (modo == SpikeMode.Estatico) return;

        switch (_estado)
        {
            case Estado.SaliendoTip:
                _rb.MovePosition(Vector2.MoveTowards(_rb.position, _posAsomando, velocidadMover * Time.fixedDeltaTime));
                if (Vector2.Distance(_rb.position, _posAsomando) < 0.01f)
                    EntrarAsomando(tiempoAsomando);
                break;

            case Estado.SaliendoFull:
                _rb.MovePosition(Vector2.MoveTowards(_rb.position, _posExtendida, velocidadMover * Time.fixedDeltaTime));
                if (Vector2.Distance(_rb.position, _posExtendida) < 0.01f)
                    EntrarDesplegado(tiempoExtendido);
                break;

            case Estado.Retractando:
                _rb.MovePosition(Vector2.MoveTowards(_rb.position, _posRetraida, velocidadMover * Time.fixedDeltaTime));
                if (Vector2.Distance(_rb.position, _posRetraida) < 0.01f)
                    EntrarRetraido(tiempoRetraido);
                break;
        }
    }

    // ── Daño ─────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_estado != Estado.Desplegado) return;
        if (!other.CompareTag("Player")) return;

        if (_col) _col.enabled = false; // evita doble trigger
        GameLoopManager.Instance?.PlayerDied();
    }

    // ── Gizmos ───────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        if (modo == SpikeMode.Retractil)
        {
            Vector3 retPos = transform.position + Vector3.up * desplazamiento;
            Vector3 asoPos = Vector3.Lerp(retPos, transform.position, fraccionAsomando);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
            Gizmos.DrawLine(transform.position, retPos);
            Gizmos.DrawWireSphere(retPos, 0.1f);

            Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
            Gizmos.DrawWireSphere(asoPos, 0.1f);
        }
    }
}
