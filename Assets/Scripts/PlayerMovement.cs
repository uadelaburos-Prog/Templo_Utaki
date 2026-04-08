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

    private float inputX;
    private bool jumpPressedThisFrame;
    private bool jumpReleasedThisFrame;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpReady = true;
    }

        if (image != null)
            image.fillAmount = 1f;
    }

    void Update()
    {
        // Controles del Jugador
        inputX = 0f;
        if (Input.GetKey(KeyCode.D)) inputX = 1f;
        else if (Input.GetKey(KeyCode.A)) inputX = -1f;

        jumpPressedThisFrame = Input.GetKeyDown(KeyCode.Space);
        jumpReleasedThisFrame = Input.GetKeyUp(KeyCode.Space);

        isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(1f, 1f), 0f, mask);

        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
            if (jumpCooldownTimer <= 0f) jumpReady = true;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && jumpReady)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCooldownTimer = jumpCooldown;
            jumpReady = false;
        }

        // Jump cut: cortar el salto al soltar espacio
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMult);
        }

            if (jumpCooldownTimer > 0f)
            {
                jumpCooldownTimer -= Time.deltaTime;
                if(jumpCooldownTimer <= 0f) { jumpReady = true; }
            }
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(inputX * moveSpeed, rb.linearVelocity.y);

        if (jumpPressedThisFrame && isGrounded && jumpReady)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            Debug.Log("Salto hecho");
            jumpCooldownTimer = jumpCooldown;
            jumpReady = false;
            jumpPressedThisFrame = false;
        }

        GrappleScript grapple = GetComponent<GrappleScript>();

        if (!isGrounded)
        {
            if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMult);
            }

            if (rb.linearVelocity.y < 0 && grapple.isGrappling == false)
            {
                rb.gravityScale = fallGravity;
            }
            else if (rb.linearVelocity.y > 0 && Mathf.Abs(rb.linearVelocity.y) < hangTimeThreshold)
            {
                rb.gravityScale = hangGravity;
            }
            else
            {
                rb.gravityScale = normalGravity;
            }
        }
        else if (isGrounded || isHanging)
        {
            rb.gravityScale = normalGravity;
        }

            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }
        }

        if (!isHanging && isGrounded) {image.fillAmount += Time.deltaTime * 0.75f; grappleTime = 0f; }
    }

    private IEnumerator CutGrappleAndRecover()
    {
        hangingTimerRunning = true;

        // Cortar el grapple
        grappleScript.GrappleRetract();

        // Esperar antes de recuperar
        yield return new WaitForSeconds(2f);

        hangingTimerRunning = false;
    }

    // Resetear stamina al soltar el grapple manualmente
    private void ResetStamina()
    {
        if (!hangingTimerRunning)
        {
            grappleTime = 0f;
            if (image != null)
                image.fillAmount = 1f;
        }
    }
}