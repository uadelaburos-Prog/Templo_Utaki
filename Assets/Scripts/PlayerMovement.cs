using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(GrappleScript))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Gravedad")]
    [SerializeField] private float normalGravity = 1f;
    [SerializeField] private float fallGravity = 5.5f;
    [SerializeField] private float maxFallSpeed = -20f;
    [SerializeField] private float hangGravity = 2f;
    [SerializeField] private float hangTimeThreshold = 0.1f;

    [Header("Suelo")]
    private bool isGrounded;
    [SerializeField] private LayerMask mask;
    [SerializeField] private Transform groundCheck;

    [Header("Salto")]
    [SerializeField, Range(0f, 1f)] private float jumpCutMult = 0.5f;
    private bool jumpReady = true;
    [SerializeField] private float jumpCooldown = 1.2f;
    private float jumpCooldownTimer = 0f;

    private GrappleScript grapple;

    public bool isHanging => grapple.isGrappling;

    // Coyote time
    [SerializeField] private float coyoteTime = 0.12f;
    private float coyoteTimer = 0f;

    // Input buffering
    [SerializeField] private float jumpBufferTime = 0.15f;
    private float jumpBufferTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        grapple = GetComponent<GrappleScript>();
    }

    void Update()
    {
        float inputX = 0f;
        if (Input.GetKey(KeyCode.D)) inputX = 1f;
        else if (Input.GetKey(KeyCode.A)) inputX = -1f;

        if (!isHanging)
        {
            rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);
        }

        isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(1f, 1f), 0f, mask);

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

        // --- JUMP COOLDOWN RESET ---                  
        if (!jumpReady)
        {
            jumpCooldownTimer -= Time.deltaTime;
            if (jumpCooldownTimer <= 0f)
                jumpReady = true;
        }

        // --- CONDICIÓN DE SALTO ---
        bool canJump = isGrounded || coyoteTimer > 0f;

        if (jumpBufferTimer > 0f && canJump && jumpReady)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpCooldownTimer = jumpCooldown;
            jumpReady = false;
        }

        // Jump cut
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMult);
    }

    void FixedUpdate()                                  
    {
        if (isHanging)
        {
            rb.gravityScale = hangGravity;
        }
        else if (rb.linearVelocity.y < -hangTimeThreshold)
        {
            // Falling — apply heavy gravity and clamp fall speed
            rb.gravityScale = fallGravity;

            if (rb.linearVelocity.y < maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
        else if (rb.linearVelocity.y > 0f)
        {
            // Rising
            rb.gravityScale = normalGravity;
        }
        else
        {
            // Grounded or apex hang
            rb.gravityScale = normalGravity;
        }

        if (!isHanging && isGrounded) {image.fillAmount += Time.deltaTime * 0.75f; grappleTime = 0f; }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawCube(groundCheck.position + Vector3.down * 0.1f, new Vector3(0.5f, 0.1f, 0f));
    }
}