using UnityEngine;

// Setup:
//   1. Posicionar el GameObject como pivot/ancla de la zona.
//   2. Agregar un Collider2D (ej. BoxCollider2D) con Is Trigger = true.
//      El jugador debe tener el tag "Player".
//      Al entrar al collider, esta zona pasa a ser la activa (última entrada gana).
//   3. Ajustar cada extensión de forma independiente — el pivot NO se mueve al cambiar un lado.
//        extLeft   → cuánto se extiende hacia la izquierda del pivot
//        extRight  → cuánto se extiende hacia la derecha del pivot
//        extBottom → cuánto se extiende hacia abajo del pivot
//        extTop    → cuánto se extiende hacia arriba del pivot
//   Nota: el collider define la detección; los bounds de cámara son SIEMPRE los ext*.
public class CameraZone : MonoBehaviour
{
    [Header("Extensiones desde el pivot")]
    [SerializeField, Min(0f)] private float extLeft   = 10f;
    [SerializeField, Min(0f)] private float extRight  = 10f;
    [SerializeField, Min(0f)] private float extBottom = 7.5f;
    [SerializeField, Min(0f)] private float extTop    = 7.5f;

    [SerializeField, Range(0.5f, 10f)] private float transitionSpeed = 3f;

    public float TransitionSpeed => transitionSpeed;
    public float MinX => transform.position.x - extLeft;
    public float MaxX => transform.position.x + extRight;
    public float MinY => transform.position.y - extBottom;
    public float MaxY => transform.position.y + extTop;
    public float Area => (extLeft + extRight) * (extBottom + extTop);

    public bool Contains(Vector2 point) =>
        point.x >= MinX && point.x <= MaxX &&
        point.y >= MinY && point.y <= MaxY;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CamaraScript.Instance?.EnterZone(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Camera c = Camera.main;
        float halfH = c != null ? c.orthographicSize : 8f;
        float halfW = c != null ? c.orthographicSize * c.aspect : 14f;

        float cx0 = MinX + halfW, cx1 = MaxX - halfW;
        float cy0 = MinY + halfH, cy1 = MaxY - halfH;
        bool validX = cx1 > cx0;
        bool validY = cy1 > cy0;

        // Centro y tamaño real de la zona (asimétrica respecto al pivot)
        float zoneW  = extLeft + extRight;
        float zoneH  = extBottom + extTop;
        float zCx    = transform.position.x + (extRight - extLeft) * 0.5f;
        float zCy    = transform.position.y + (extTop   - extBottom) * 0.5f;
        Vector3 centro = new Vector3(zCx, zCy, 0f);
        Vector3 size   = new Vector3(zoneW, zoneH, 0f);

        // Borde exterior — naranja si la zona es demasiado baja para contener el campo visual
        Color outer = validY ? new Color(0.2f, 0.9f, 0.4f, 0.9f) : new Color(1f, 0.45f, 0f, 0.9f);
        Gizmos.color = outer;
        Gizmos.DrawWireCube(centro, size);
        Gizmos.color = new Color(outer.r, outer.g, outer.b, 0.07f);
        Gizmos.DrawCube(centro, size);

        // Cruz en el pivot para visualizar el punto de anclaje
        Gizmos.color = new Color(outer.r, outer.g, outer.b, 0.7f);
        float cross = 0.4f;
        Vector3 p = transform.position;
        Gizmos.DrawLine(p + Vector3.left  * cross, p + Vector3.right * cross);
        Gizmos.DrawLine(p + Vector3.down  * cross, p + Vector3.up    * cross);

        // Rectángulo amarillo interno = rango de desplazamiento del centro de cámara
        if (validX && validY)
        {
            Gizmos.color = new Color(1f, 0.95f, 0.1f, 0.55f);
            Vector3 iCenter = new Vector3((cx0 + cx1) * 0.5f, (cy0 + cy1) * 0.5f, 0f);
            Vector3 iSize   = new Vector3(cx1 - cx0, cy1 - cy0, 0f);
            Gizmos.DrawWireCube(iCenter, iSize);
        }
    }
#endif
}
