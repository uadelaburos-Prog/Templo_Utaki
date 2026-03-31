using UnityEngine;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(GrappleScript))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    public int moveSpeed = 5;
    public float counterForce = 0.7f;
    public float jumpForce = 10f;

    private float speedPlayer = 5f;
    private Vector3 posAnterior;

    [Header("Gravedad")]
    [SerializeField] private float normalGravity = 1f;
    [SerializeField] private float fallGravity = 5.5f;
    [SerializeField] private float maxFallSpeed = -20f;
    [SerializeField] private float hangGravity = 2f;
    [SerializeField] private float hangTimeThreshold = 0.1f;
    [SerializeField] private float downwardGravity = 2f;

    [Header("Suelo")]
    private bool isGrounded;
    private bool jumpPressed;

    [Header("Salto")]
    [SerializeField, Range(0f, 1f)] private float jumpCutMult = 0.5f;
    private bool jumpReady = true;
    [SerializeField] private float jumpCooldown = 1.2f;
    private float jumpCooldownTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    void Update()
    {
        // Controles del Jugador
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(moveSpeed * Time.deltaTime * Vector2.right);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(moveSpeed * Time.deltaTime * Vector2.left);
        }
        else
        {
            transform.Translate(Vector2.zero);
        }

        if (isGrounded)
        {
            jumpPressed = Input.GetKeyDown(KeyCode.Space);

            speedPlayer = (transform.position - posAnterior).magnitude / Time.deltaTime;
            posAnterior = transform.position;


            if (jumpCooldownTimer > 0f)
            {
                jumpCooldownTimer -= Time.deltaTime;
                jumpReady = jumpCooldownTimer <= 0;
            }

            if (jumpPressed && isGrounded && jumpReady)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpCooldownTimer = jumpCooldown;
            }

            GrappleScript grapple = GetComponent<GrappleScript>();

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

            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }

            if (Input.GetKey(KeyCode.S) && !isGrounded)
            {
                rb.gravityScale = downwardGravity;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Floor"))
        {
            isGrounded = true;
            Debug.Log("Suelo");
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == null)
        {
            isGrounded = false;
            Debug.Log("No Suelo");
        }
    }
}