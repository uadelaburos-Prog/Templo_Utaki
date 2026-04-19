using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(GrappleScript))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Movimiento — Suelo")]
    [Tooltip("Velocidad horizontal máxima en suelo (u/s).")]
    [SerializeField] private float moveSpeed   = 12f;
    [Tooltip("Aceleración horizontal al moverse en la dirección deseada en suelo (u/s²).")]
    [SerializeField] private float groundAccel = 140f;
    [Tooltip("Desaceleración al cambiar dirección o soltar la tecla en suelo (u/s²).")]
    [SerializeField] private float groundDecel = 55f;

    [Header("Movimiento — Aire")]
    [Tooltip("Aceleración horizontal en aire libre. Valor bajo para preservar el momentum del swing (u/s²).")]
    [SerializeField] private float freeAirAccel      = 4f;
    [Tooltip("Fuerza tangencial aplicada al arco del péndulo al presionar A/D mientras cuelga del gancho (N).")]
    [SerializeField] private float swingForce        = 15f;
    [Tooltip("Escala de gravedad mientras el jugador está enganchado. Controla la velocidad del péndulo. Physics2D Gravity Y = -30.")]
    [SerializeField] private float swingGravityScale = 2.5f;

    [Header("Gravedad — Physics2D Gravity Y = -30")]
    [Tooltip("Multiplicador de gravedad en caída libre y salto. Gravedad efectiva = gravityScale × Physics2D.gravity.y.")]
    [SerializeField] private float gravityScale      = 3.5f;
    [Tooltip("Velocidad vertical máxima de caída (u/s, valor negativo). Limita la velocidad terminal.")]
    [SerializeField] private float maxFallSpeed      = -20f;
    [Tooltip("Si la velocidad vertical es menor a este valor y se sostiene Espacio, la gravedad se reduce a la mitad (efecto apex float).")]
    [SerializeField] private float halfGravThreshold = 5f;

    [Header("Salto")]
    [Tooltip("Velocidad vertical inicial al saltar (u/s).")]
    [SerializeField] private float jumpForce    = 13f;
    [Tooltip("Velocidad vertical mínima sostenida mientras se mantiene Espacio durante el salto variable.")]
    [SerializeField] private float varJumpSpeed = 11f;
    [Tooltip("Duración máxima del salto variable sosteniendo Espacio (segundos).")]
    [SerializeField] private float varJumpTime  = 0.2f;
    [Tooltip("Multiplicador de velocidad vertical al soltar Espacio antes del apex. 0 = corte total, 1 = sin corte.")]
    [SerializeField, Range(0f, 1f)] private float jumpCutMult = 0.25f;
    [Tooltip("Ventana de tiempo tras salir de un borde en la que aún se puede saltar (segundos).")]
    [SerializeField] private float coyoteTime     = 0.12f;
    [Tooltip("El input de salto se recuerda durante este tiempo. Si el jugador aterriza en la ventana, el salto se ejecuta automáticamente (segundos).")]
    [SerializeField] private float jumpBufferTime  = 0.15f;

    [Header("Momentum Jump")]
    [Tooltip("Boost de velocidad horizontal adicional al saltar a alta velocidad en la misma dirección (u/s).")]
    [SerializeField] private float jumpHBoost          = 4f;
    [Tooltip("Fracción de moveSpeed necesaria para activar el boost horizontal. 0.8 = requiere estar al 80% de la velocidad máxima.")]
    [SerializeField] private float jumpHBoostThreshold = 0.8f;

    [Header("Swing Jump")]
    [Tooltip("Ventana de tiempo tras soltar el gancho en la que el salto recibe boost de altura (segundos).")]
    [SerializeField] private float swingJumpWindow    = 0.2f;
    [Tooltip("Multiplicador de jumpForce al saltar dentro de la ventana swing jump. 1.3 = +30% de altura.")]
    [SerializeField] private float swingJumpBoostMult = 1.3f;

    [Header("Suelo")]
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    [Tooltip("Layer mask de las superficies que se consideran suelo para la detección de isGrounded.")]
    [SerializeField] private LayerMask mask;
    [Tooltip("Transform hijo del jugador desde el que se lanza el OverlapBox de detección de suelo.")]
    [SerializeField] private Transform groundCheck;

    private GrappleScript grapple;
    public bool isHanging => grapple.isGrappling;

    private float inputX          = 0f;
    private bool  jumpHeld        = false;
    private float coyoteTimer     = 0f;
    private float jumpBufferTimer = 0f;
    private float varJumpTimer    = 0f;
    private bool  varJumpActive   = false;
    private bool  wasHanging      = false;
    private float swingJumpTimer  = 0f;

    void Start()
    {
        rb      = GetComponent<Rigidbody2D>();
        grapple = GetComponent<GrappleScript>();
    }

    void Update()
    {
        inputX = 0f;
        if (Input.GetKey(KeyCode.D))      inputX =  1f;
        else if (Input.GetKey(KeyCode.A)) inputX = -1f;

        jumpHeld = Input.GetKey(KeyCode.Space);

        // --- DETECCIÓN DE SOLTAR GANCHO → abre ventana swing jump ---
        bool hangingNow = isHanging;
        if (wasHanging && !hangingNow)
            swingJumpTimer = swingJumpWindow;
        wasHanging = hangingNow;

        if (swingJumpTimer > 0f)
            swingJumpTimer -= Time.deltaTime;

        // --- COYOTE TIME ---
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        // --- INPUT BUFFERING ---
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBufferTime;
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        // --- SALTO ---
        // canJump: suelo, coyote, o dentro de ventana swing jump
        bool canJump    = isGrounded || coyoteTimer > 0f || swingJumpTimer > 0f;
        bool isSwingJump = swingJumpTimer > 0f && !isGrounded && coyoteTimer <= 0f;

        if (jumpBufferTimer > 0f && canJump)
        {
            float vy = isSwingJump ? jumpForce * swingJumpBoostMult : jumpForce;

            float hBoost = 0f;
            bool atSpeed = Mathf.Abs(rb.linearVelocity.x) >= moveSpeed * jumpHBoostThreshold;
            bool sameDir = inputX != 0 && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(inputX);
            if (atSpeed && sameDir)
                hBoost = inputX * jumpHBoost;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x + hBoost, vy);
            varJumpTimer   = varJumpTime;
            varJumpActive  = true;
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
            swingJumpTimer  = 0f;
        }

        // --- SALTO VARIABLE (VarJump) ---
        if (varJumpActive && varJumpTimer > 0f)
        {
            if (jumpHeld && rb.linearVelocity.y > 0f)
            {
                if (rb.linearVelocity.y < varJumpSpeed)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, varJumpSpeed);

                varJumpTimer -= Time.deltaTime;
                if (varJumpTimer <= 0f)
                    varJumpActive = false;
            }
            else if (!jumpHeld)
            {
                if (rb.linearVelocity.y > 0f)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMult);
                varJumpTimer  = 0f;
                varJumpActive = false;
            }
        }
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(1f, 1f), 0f, mask);

        // --- MOVIMIENTO HORIZONTAL ---
        if (isGrounded)
        {
            float targetX = inputX * moveSpeed;
            bool  sameDir = inputX != 0 && Mathf.Sign(inputX) == Mathf.Sign(rb.linearVelocity.x);
            float accel   = sameDir ? groundAccel : groundDecel;
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, targetX, accel * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }
        else if (isHanging)
        {
            // Fuerza tangencial al arco — elimina equilibrio diagonal
            Vector2 anchorPos = grapple.joint.connectedAnchor;
            Vector2 ropeDir   = ((Vector2)transform.position - anchorPos).normalized;
            Vector2 tangente  = new Vector2(-ropeDir.y, ropeDir.x);
            rb.AddForce(tangente * inputX * swingForce);
        }
        else
        {
            // Aire libre: control casi nulo — preserva el momentum del swing
            float targetX = inputX * moveSpeed;

            bool tieneMomentum = inputX != 0
                && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(inputX)
                && Mathf.Abs(rb.linearVelocity.x) > moveSpeed;

            if (!tieneMomentum)
                rb.linearVelocity = new Vector2(
                    Mathf.MoveTowards(rb.linearVelocity.x, targetX, freeAirAccel * Time.fixedDeltaTime),
                    rb.linearVelocity.y
                );
        }

        // --- GRAVEDAD (uniforme, apex float al mantener salto) ---
        if (!isHanging && !isGrounded)
        {
            float gScale = gravityScale;
            if (Mathf.Abs(rb.linearVelocity.y) < halfGravThreshold && jumpHeld)
                gScale *= 0.5f;

            rb.gravityScale = gScale;

            if (rb.linearVelocity.y < maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
        else if (isHanging)
        {
            rb.gravityScale = swingGravityScale;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawCube(groundCheck.position + Vector3.down * 0.1f, new Vector3(0.5f, 0.1f, 0f));
    }
}
