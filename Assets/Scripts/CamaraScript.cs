using UnityEngine;

public class CamaraScript : MonoBehaviour
{
    Camera cam;

    [Header("Side Viewer")]
    [SerializeField, Range (6f, 13f)] private float orthoSize = 8f;
    [SerializeField, Range (0f, 2f)] private float lookBehindDistance = 3f;
    [SerializeField, Range (1f, 5f)] private float lookBehindSpeed = 1f;
    [SerializeField, Range (1f, 6f)] private float camSpeed = 1f;

    [Header("Up&Down Viewer")]
    [SerializeField, Range (0f, 3f)] private float lookDownDistance = 3f;
    [SerializeField, Range (1f, 5f)] private float lookDownSpeed = 1f;

    [Header("Variables")]
    [SerializeField] private float maxFallSpeed = 10f;
 
    [SerializeField] private Transform player;
    private Rigidbody2D rb;

    private float currentLookBehind;
    private float currentLookDown;

    private void Start()
    {
        rb = player.GetComponent<Rigidbody2D>();
        cam = GetComponent<Camera>();
        cam.orthographicSize = orthoSize;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        //logica de la Camera para mirar hacia atras
        float input = Input.GetAxis("Horizontal");

        float targetLookBehind = input * lookBehindDistance;

        currentLookBehind = Mathf.Lerp(currentLookBehind, targetLookBehind, lookBehindSpeed * Time.deltaTime);

        Vector3 targetPos = new Vector3(player.position.x - currentLookBehind, player.position.y - currentLookDown, transform.position.z);

        //logica mirar hacia arriba
        if (rb.linearVelocityY < 0f)
        {
            float fallPercent = Mathf.Clamp01(-rb.linearVelocityY / maxFallSpeed);
            float targetLookDown = fallPercent * lookDownDistance;

            currentLookDown = Mathf.Lerp(currentLookDown, targetLookDown, lookDownSpeed * Time.deltaTime);
        }
        else
        {
            currentLookDown = Mathf.Lerp(currentLookDown, 0f, lookDownSpeed * Time.deltaTime);
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, camSpeed * Time.deltaTime);
    }
}
