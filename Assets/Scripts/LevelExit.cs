using UnityEngine;

// Setup: Tag "Finish" en este GameObject + Collider2D marcado como Trigger.
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    private bool activado;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activado || !other.CompareTag("Player")) return;
        activado = true;
        GameLoopManager.Instance?.Reintentar();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
