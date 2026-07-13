using UnityEngine;

// Zona vulnerable del Golem (mano izq, mano der, cabeza, pecho). Reutiliza el sistema de
// tracción del gancho (AttachType.Pull / IHookable, igual que ReactiveWall): el jugador la
// engancha y tira; al desplazarla lo suficiente se registra UN golpe y avisa al GolemBoss.
//
// Es un collider hookeable INVISIBLE: el resaltado visual lo da el propio cuerpo del Golem,
// que cambia al frame de esa parte (Animator) durante la ventana de vulnerabilidad.
//
// Trabaja en espacio LOCAL respecto al Golem: así el punto débil viaja con el cuerpo cuando
// el Golem se reubica de un salto, sin falsos positivos de "desplazamiento".
//
// Setup: hijo del Golem · Tag "Hookable" · Layer "Hookable" · Rigidbody2D · Collider2D
// (se fuerza Trigger en Awake para no empujar al jugador). Empieza desactivada (no enganchable).
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class GolemWeakPoint : MonoBehaviour, IHookable
{
    [Header("Golpe")]
    [Tooltip("Desplazamiento local (u) que debe alcanzar el tirón para contar como golpe.")]
    [SerializeField] private float distanciaGolpe = 0.6f;

    private Rigidbody2D   _rb;
    private Collider2D    _col;
    private GrappleScript _grappleJugador;

    private GolemBoss _boss;
    private int       _indice;

    private Vector2 _posLocalOriginal;
    private bool    _activa;      // vulnerable y enganchable
    private bool    _enganchada;
    private bool    _golpeado;    // ya registró el golpe de esta ventana

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        _col.isTrigger     = true;

        _posLocalOriginal = transform.localPosition;

        // Arranca inerte: sin colisión hasta que el Golem la vuelva vulnerable.
        _col.enabled = false;
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _grappleJugador = player.GetComponent<GrappleScript>();
    }

    // Llamado una vez por el GolemBoss para vincular la parte con su índice de daño.
    public void Inicializar(GolemBoss boss, int indice)
    {
        _boss   = boss;
        _indice = indice;
    }

    private void FixedUpdate()
    {
        if (!_activa || !_enganchada || _golpeado) return;

        // Desplazamiento en espacio local (independiente de dónde esté parado el Golem).
        float desplazamiento = Vector2.Distance(
            (Vector2)transform.localPosition, _posLocalOriginal);

        if (desplazamiento >= distanciaGolpe)
            RegistrarGolpe();
    }

    // ── VENTANA DE VULNERABILIDAD ─────────────────────────────────

    // Abre la parte a ser enganchada por el gancho.
    public void Activar()
    {
        _activa      = true;
        _golpeado    = false;
        _enganchada  = false;
        _col.enabled = true;
        RestaurarPosicion();
    }

    // Cierra la ventana sin golpe (expiró el tiempo).
    public void Desactivar()
    {
        _activa      = false;
        _enganchada  = false;
        _col.enabled = false;
        RestaurarPosicion();
    }

    // ── IHookable (sistema de tracción del gancho) ────────────────

    public void OnHooked()
    {
        if (!_activa) return;
        _enganchada = true;
        // Libera la posición para que el tirón del gancho la desplace; rotación fija.
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void OnReleased()
    {
        if (_golpeado) return;
        _enganchada = false;
        RestaurarPosicion();
    }

    // ── PRIVADOS ──────────────────────────────────────────────────

    private void RegistrarGolpe()
    {
        _golpeado   = true;
        _activa     = false;
        _enganchada = false;

        // Retraer el gancho del jugador para cortar la tracción antes de reposicionar.
        _grappleJugador?.GrappleRetract();

        RestaurarPosicion();
        _col.enabled = false;

        _boss?.RegistrarGolpe(_indice);
    }

    private void RestaurarPosicion()
    {
        _rb.linearVelocity  = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.constraints     = RigidbodyConstraints2D.FreezeAll;
        transform.localPosition = _posLocalOriginal;
        transform.localRotation = Quaternion.identity;
    }
}
