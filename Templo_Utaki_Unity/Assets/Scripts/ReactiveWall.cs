using System.Collections;
using UnityEngine;

// Setup: Tag "Hookable" + Layer "Hookable" + Rigidbody2D (Dynamic, gravityScale 0) + Collider2D.
// El jugador choca con la pared pero no la empuja.
// Solo el gancho (GrappleScript) puede moverla al jalar con S.
// Al desplazarse lo suficiente cae rotando desde la base.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class ReactiveWall : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Sprite de la pared. Si se deja vacío se genera un placeholder marrón.")]
    [SerializeField] private Sprite spritePersonalizado;
    [SerializeField] private Color  colorPlaceholder = new Color32(101, 67, 33, 255);
    [SerializeField] private string sortingLayerName  = "Tilemap";

    [Header("Derribo")]
    [Tooltip("Desplazamiento desde la posición original que dispara la caída.")]
    [SerializeField] private float distanciaDerribo = 1.5f;
    [Tooltip("Duración de la animación de caída (segundos).")]
    [SerializeField] private float duracionCaida    = 0.4f;
    [Tooltip("Ángulos que rota la pared al caer (90 = queda horizontal).")]
    [SerializeField] private float angulosCaida     = 90f;

    private Rigidbody2D rb;
    private Collider2D  col;
    private Vector2     posicionOriginal;
    private bool        derribada;
    private bool        enganchada;

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        var sr = GetComponent<SpriteRenderer>();

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        // Completamente estático hasta que el gancho lo agarre
        rb.constraints    = RigidbodyConstraints2D.FreezeAll;

        sr.sortingLayerName = sortingLayerName;
        sr.sprite = spritePersonalizado != null ? spritePersonalizado : GenerarPlaceholder();

        posicionOriginal = rb.position;
    }

    private void FixedUpdate()
    {
        if (derribada || !enganchada) return;

        if (Vector2.Distance(rb.position, posicionOriginal) >= distanciaDerribo)
            StartCoroutine(Derribo());
    }

    // Llamado por GrappleScript al engancharse
    public void OnHooked()
    {
        enganchada = true;
        // Libera posición para que AddForce la mueva; rotación sigue fija
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // Llamado por GrappleScript al soltar antes del derribo
    public void OnReleased()
    {
        if (derribada) return;
        enganchada = false;
        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints     = RigidbodyConstraints2D.FreezeAll;
    }

    private IEnumerator Derribo()
    {
        derribada = true;

        // Dirección de caída según el lado del desplazamiento
        float signo = rb.position.x >= posicionOriginal.x ? 1f : -1f;

        // Detener movimiento y pasar a Kinematic para controlar la animación
        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType        = RigidbodyType2D.Kinematic;
        rb.constraints     = RigidbodyConstraints2D.None;

        // Ignorar colisión con el jugador desde ya
        var playerCol = GameObject.FindWithTag("Player")?.GetComponent<Collider2D>();
        if (playerCol != null)
            Physics2D.IgnoreCollision(col, playerCol, true);

        // Pivote en la base del objeto (punto que no se mueve durante la caída)
        Vector2 startPos   = transform.position;
        float   mitadAltura = col.bounds.extents.y;
        Vector2 pivote     = startPos - Vector2.up * mitadAltura;

        float startRot = transform.eulerAngles.z;
        float endRot   = startRot - angulosCaida * signo;
        float t        = 0f;

        while (t < duracionCaida)
        {
            t += Time.deltaTime;
            float progreso     = Mathf.Clamp01(t / duracionCaida);
            float anguloActual = Mathf.Lerp(startRot, endRot, progreso);

            // Rotar el vector (centro → pivote) y reconstruir la posición
            float   delta  = (anguloActual - startRot) * Mathf.Deg2Rad;
            float   cos    = Mathf.Cos(delta);
            float   sin    = Mathf.Sin(delta);
            Vector2 offset = startPos - pivote;
            Vector2 rotado = new Vector2(
                cos * offset.x - sin * offset.y,
                sin * offset.x + cos * offset.y
            );

            transform.position = pivote + rotado;
            transform.rotation = Quaternion.Euler(0f, 0f, anguloActual);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, endRot);
        col.enabled        = false;
    }

    private Sprite GenerarPlaceholder()
    {
        int w = 16, h = 48;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool linea = (y % 8 == 0) || (y % 8 == 1);
                tex.SetPixel(x, y, linea
                    ? new Color(colorPlaceholder.r * 0.7f, colorPlaceholder.g * 0.7f, colorPlaceholder.b * 0.7f)
                    : colorPlaceholder);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
