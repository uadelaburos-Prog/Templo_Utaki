using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float distance = 3f; // distancia a cada lado

    private Vector3 pointA;
    private Vector3 pointB;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pointA = transform.position + Vector3.left * distance;
        pointB = transform.position + Vector3.right * distance;
    }

    private void FixedUpdate()
    {
        float t = (Mathf.Sin(Time.fixedTime * speed) + 1f) / 2f;
        rb.MovePosition(Vector2.Lerp(pointA, pointB, t));
    }
}
