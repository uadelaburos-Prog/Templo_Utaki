using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Header("Bounds de esta zona")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX =  20f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY =  20f;

    [Tooltip("Velocidad de transición al entrar en esta zona.")]
    [SerializeField, Range(0.5f, 10f)] private float transitionSpeed = 3f;

    public float MinX => minX;
    public float MaxX => maxX;
    public float MinY => minY;
    public float MaxY => maxY;
    public float TransitionSpeed => transitionSpeed;
    public float Area => (maxX - minX) * (maxY - minY);

    public bool Contains(Vector2 point) =>
        point.x >= minX && point.x <= maxX &&
        point.y >= minY && point.y <= maxY;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size   = new Vector3(maxX - minX, maxY - minY, 0f);

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.08f);
        Gizmos.DrawCube(center, size);
    }
#endif
}
