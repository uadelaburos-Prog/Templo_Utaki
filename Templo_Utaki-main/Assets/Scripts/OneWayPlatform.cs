using System.Collections;
using UnityEngine;

// Setup en el Tilemap GameObject:
//   - TilemapCollider2D: activar "Used By Composite"
//   - CompositeCollider2D: Geometry Type = Outlines, Body Type = Static
//   (NO marcar "Used By Effector" — este script no usa PlatformEffector2D)
[RequireComponent(typeof(CompositeCollider2D))]
public class OneWayPlatform : MonoBehaviour
{
    [Tooltip("Margen vertical: si los pies del jugador están este valor por debajo del tope, pasa a través.")]
    [SerializeField] private float tolerancia   = 0.05f;
    [Tooltip("Segundos que la colisión se deshabilita al bajar atravesando (abajo + salto).")]
    [SerializeField] private float duracionBajar = 0.3f;

    private Collider2D col;
    private Collider2D playerCol;
    private bool       forzarBajar;

    private void Awake()
    {
        col = GetComponent<CompositeCollider2D>();
    }

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null) playerCol = go.GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (playerCol == null || forzarBajar) return;

        float pies = playerCol.bounds.min.y;
        float tope = col.ClosestPoint(playerCol.bounds.center).y;

        Physics2D.IgnoreCollision(playerCol, col, pies < tope - tolerancia);
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
        Physics2D.IgnoreCollision(playerCol, col, true);
        yield return new WaitForSeconds(duracionBajar);
        forzarBajar = false;
    }

    private void OnDrawGizmos()
    {
        if (col == null) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
