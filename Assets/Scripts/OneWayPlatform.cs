using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class OneWayPlatform : MonoBehaviour
{
    [Tooltip("Segundos que la colisión se deshabilita al bajar atravesando (abajo + salto).")]
    [SerializeField] private float duracionBajar = 0.3f;

    private Collider2D  col;
    private Collider2D  playerCol;
    private Rigidbody2D playerRb;
    private bool        forzarBajar;
    private bool        _ignoring;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go == null) return;
        playerCol = go.GetComponent<Collider2D>();
        playerRb  = go.GetComponent<Rigidbody2D>();

        _ignoring = true;
        Physics2D.IgnoreCollision(playerCol, col, true);
    }

    private void FixedUpdate()
    {
        if (playerCol == null) return;
        if (forzarBajar) { Ignorar(true); return; }

        float pies = playerCol.bounds.min.y;
        float tope = col.bounds.max.y;
        float vy   = playerRb != null ? playerRb.linearVelocity.y : 0f;

        // Subiendo: sin lookahead — permite saltar a través desde abajo
        if (vy > 0f)
        {
            Ignorar(pies < tope - 0.05f);
            return;
        }

        // Bajando o en reposo: lookahead de un step para capturar tunneling y entrada diagonal
        float dt       = Time.fixedDeltaTime;
        float piesNext = pies + vy * dt;
        float pxNow    = playerCol.bounds.center.x;
        float pxNext   = pxNow + (playerRb != null ? playerRb.linearVelocity.x * dt : 0f);

        bool dentroXNow  = pxNow  >= col.bounds.min.x && pxNow  <= col.bounds.max.x;
        bool dentroXNext = pxNext >= col.bounds.min.x && pxNext <= col.bounds.max.x;

        // Claramente por debajo ahora y en el próximo step → ignorar
        if (pies < tope - 0.05f && piesNext < tope - 0.05f) { Ignorar(true); return; }

        // Fuera del rango X ahora y en el próximo step → ignorar (travesía lateral)
        if (!dentroXNow && !dentroXNext) { Ignorar(true); return; }

        // Va a cruzar la superficie y está (o estará) dentro del rango X → activar colisión
        Ignorar(false);
    }

    private void Ignorar(bool valor)
    {
        if (_ignoring == valor) return;
        _ignoring = valor;
        Physics2D.IgnoreCollision(playerCol, col, valor);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (forzarBajar || !collision.gameObject.CompareTag("Player")) return;
        if (Input.GetAxisRaw("Vertical") < -0.5f)
            StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        forzarBajar = true;
        Ignorar(true);
        yield return new WaitForSeconds(duracionBajar);
        forzarBajar = false;
    }
}
