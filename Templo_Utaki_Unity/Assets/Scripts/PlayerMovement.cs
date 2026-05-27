using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(GrappleScript))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Movimiento")]
    [Tooltip("Velocidad horizontal máxima (u/s). GDD §4.1: 8–10.")]
    [SerializeField] private float moveSpeed = 9f;
    [Tooltip("Aceleración horizontal en el aire al presionar una dirección (u/s²).")]
    [SerializeField] private float airAccel  = 35f;
    [Tooltip("Desaceleración en aire sin input — permite caer sobre el mismo tile (u/s²).")]
    [SerializeField] private float airDecel  = 20f;

    [Header("Salto")]
    [Tooltip("Velocidad vertical inicial al saltar (u/s). GDD §4.1: 12.")]
    [SerializeField] private float jumpForce   = 12f;
    [Tooltip("Multiplicador al soltar espacio antes del apex. GDD §4.1: 0.5.")]
    [SerializeField, Range(0f, 1f)] private float jumpCutMult = 0.5f;
    [Tooltip("Ventana post-borde donde aún se puede saltar (s). GDD §4.1: 0.12.")]
    [SerializeField] private float coyoteTime  = 0.12f;

    [Header("Gravedad — Physics2D.gravity.y = −30")]
    [Tooltip("Escala de gravedad mientras el jugador sube (espacio sostenido). Menor = apex más flotante.")]
    [SerializeField] private float riseGravityScale = 2.0f;
    [Tooltip("Escala de gravedad en caída libre. GDD §4.1 fallGravity ≈ 5.5 → usa ~3.5 con gravity −30.")]
    [SerializeField] private float fallGravityScale = 3.5f;
    [Tooltip("Velocidad vertical máxima de caída (u/s, negativo). GDD §4.1: −20.")]
    [SerializeField] private float maxFallSpeed     = -20f;

    [Header("Gancho")]
    [Tooltip("Fuerza tangencial aplicada al arco del péndulo con A/D (N).")]
    [SerializeField] private float swingForce        = 15f;
    [Tooltip("Escala de gravedad mientras el jugador está enganchado.")]
    [SerializeField] private float swingGravityScale = 2.5f;

    [Header("Suelo")]
    [SerializeField] private LayerMask mask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckWidth  = 0.45f;
    [SerializeField] private float groundCheckHeight = 0.05f;
    [Tooltip("Normal Y mínima para considerar un contacto como suelo (0.5 = ~45° con margen). Evita que las paredes cuenten como suelo.")]
    [SerializeField, Range(0.5f, 1f)] private float groundNormalThreshold = 0.5f;

    [Header("Muerte")]
    [Tooltip("Duración de la animación de muerte en segundos. Debe coincidir con el clip 'Death' del Animator.")]
    [SerializeField] private float deathAnimDuration = 1.5f;

    public float DeathAnimDuration => deathAnimDuration;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxSalto;
    [SerializeField] private AudioClip sfxAterrizaje;

    private bool isGrounded;
    public  bool IsGrounded => isGrounded;

    private ContactFilter2D _groundFilter;
    private ContactPoint2D[] _contacts = new ContactPoint2D[8];

    private GrappleScript grapple;
    public  bool isHanging => grapple.isGrappling;

    private Animator       anim;
    private SpriteRenderer sr;

    private float inputX      = 0f;
    private bool  jumpHeld    = false;
    private float coyoteTimer = 0f;
    private bool  jumpQueued  = false;   // se mantiene hasta aterrizar, sin expirar
    private bool  jumpCutDone = false;
    private bool  wasHanging  = false;
    private bool  wasGrounded = false;
    private bool  isDead      = false;
    private float _deathVY    = 0f;

    void Start()
    {
        rb            = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        grapple = GetComponent<GrappleScript>();
        anim    = GetComponent<Animator>();
        sr      = GetComponent<SpriteRenderer>();

        _groundFilter = new ContactFilter2D();
        _groundFilter.SetLayerMask(mask);
        _groundFilter.useTriggers = false;
    }

    // Llamado por GameLoopManager al respawnear en un checkpoint
    public void RespawnAt(Vector3 pos)
    {
        transform.position = pos;
        rb.linearVelocity  = Vector2.zero;
        rb.gravityScale    = 1f;
    }

    // Llamado por GameLoopManager cuando el jugador muere por un elemento (no el vacío)
    public void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;

        _deathVY = rb.linearVelocity.y;                         // conservar caída actual
        rb.linearVelocity = new Vector2(0f, _deathVY);
        rb.gravityScale   = 0f;                                  // la gravedad se simula manualmente

        if (grapple != null) grapple.ForceIdle();

        // UnscaledTime: la animación sigue reproduciéndose aunque Time.timeScale = 0
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        anim.SetBool("IsDead",     true);   // bloquea Any-State → Fall/Jump/etc. cada frame
        anim.SetBool("IsRunning",  false);
        anim.SetBool("IsGrounded", false);
        anim.SetBool("IsHanging",  false);
        anim.SetBool("IsClimbing", false);
        anim.SetFloat("VelocityY", 0f);     // neutral: evita disparar condiciones de Fall
        anim.Play("Death", 0, 0f);
    }

    void Update()
    {
        if (isDead)
        {
            // Todo el juego pausado (timeScale=0) pero el jugador sigue cayendo
            float dt = Time.unscaledDeltaTime;
            _deathVY     += Physics2D.gravity.y * fallGravityScale * dt;
            _deathVY      = Mathf.Max(_deathVY, maxFallSpeed);
            rb.position  += new Vector2(0f, _deathVY * dt);
            return;
        }

        inputX = 0f;
        if      (Input.GetKey(KeyCode.D)) inputX =  1f;
        else if (Input.GetKey(KeyCode.A)) inputX = -1f;

        jumpHeld = Input.GetKey(KeyCode.Space);

        if (inputX != 0f) sr.flipX = inputX < 0f;

        // Saltar mientras se cuelga corta la soga en el mismo frame
        if (Input.GetKeyDown(KeyCode.Space) && isHanging)
            grapple.GrappleRetract();

        // Coyote time — se activa también al soltar el gancho
        bool hangingNow = isHanging;
        if (wasHanging && !hangingNow)
        {
            coyoteTimer  = coyoteTime;
            jumpCutDone  = false;
            if (jumpHeld) jumpQueued = true; // space ya apretado al soltar → anota el salto
        }
        wasHanging = hangingNow;

        if (isGrounded)       coyoteTimer = coyoteTime;
        else if (!hangingNow) coyoteTimer -= Time.deltaTime;

        // Marcar salto — el intent se conserva hasta aterrizar, sin expirar
        if (Input.GetKeyDown(KeyCode.Space)) jumpQueued = true;

        // Aterrizaje
        if (isGrounded && !wasGrounded)
            AudioManager.instance?.FxSoundEffect(sfxAterrizaje, transform, 1f);
        wasGrounded = isGrounded;

        // Ejecutar salto
        bool canJump = !hangingNow && (isGrounded || coyoteTimer > 0f);
        if (jumpQueued && canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            AudioManager.instance?.FxSoundEffect(sfxSalto, transform, 1f);
            jumpQueued  = false;
            coyoteTimer = 0f;
            jumpCutDone = false;
        }

        // Jump cut: soltar espacio antes del apex recorta la velocidad vertical
        if (!jumpHeld && rb.linearVelocity.y > 0f && !jumpCutDone)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMult);
            jumpCutDone = true;
        }

        if (isGrounded) jumpCutDone = false;

        // Parámetros del Animator — después de que hangingNow está definido
        anim.SetBool("IsRunning",  isGrounded && inputX != 0f);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetBool("IsHanging",  hangingNow);
        anim.SetFloat("VelocityY", rb.linearVelocity.y);
        anim.SetBool("IsClimbing", hangingNow && grapple.IsClimbing);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        isGrounded = false;
        int contactCount = rb.GetContacts(_groundFilter, _contacts);
        for (int i = 0; i < contactCount; i++)
        {
            if (_contacts[i].normal.y >= groundNormalThreshold)
            {
                isGrounded = true;
                break;
            }
        }

        // Movimiento horizontal
        if (isHanging)
        {
            // Fuerza tangencial al péndulo
            Vector2 anchorPos = grapple.joint.connectedAnchor;
            Vector2 ropeDir   = ((Vector2)transform.position - anchorPos).normalized;
            Vector2 tangente  = new Vector2(-ropeDir.y, ropeDir.x);
            rb.AddForce(tangente * inputX * swingForce);
        }
        else if (isGrounded)
        {
            // Suelo: velocidad directa, sin inercia
            if (Mathf.Abs(rb.linearVelocity.x) > moveSpeed)
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * moveSpeed, rb.linearVelocity.y);

            // Sin input y sin impulso de salto: zerear Y para evitar deslizamiento en rampas
            float vy = (inputX == 0f && rb.linearVelocity.y <= 0f) ? 0f : rb.linearVelocity.y;
            rb.linearVelocity = new Vector2(inputX * moveSpeed, vy);
        }
        else
        {
            // Aire: accel al presionar, decel sin input
            float target = inputX * moveSpeed;
            float accel  = inputX == 0f ? airDecel : airAccel;

            // Conservar momentum del swing si supera moveSpeed en la misma dirección
            bool tieneMomentum = inputX != 0f
                && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(inputX)
                && Mathf.Abs(rb.linearVelocity.x) > moveSpeed;

            if (!tieneMomentum)
                rb.linearVelocity = new Vector2(
                    Mathf.MoveTowards(rb.linearVelocity.x, target, accel * Time.fixedDeltaTime),
                    rb.linearVelocity.y);
        }

        // Gravedad
        if (isHanging)
        {
            rb.gravityScale = swingGravityScale;
        }
        else if (!isGrounded)
        {
            rb.gravityScale = (rb.linearVelocity.y > 0f && jumpHeld)
                ? riseGravityScale
                : fallGravityScale;

            if (rb.linearVelocity.y < maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheck.position, new Vector3(groundCheckWidth, groundCheckHeight, 0f));
    }
}
