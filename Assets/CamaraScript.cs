using UnityEngine;

public class CamaraScript : MonoBehaviour
{
    Camera cam;

    [SerializeField, Range (6f, 15f)] private float orthoSize = 8f;
    [SerializeField, Range (0f, 6f)] private float lookBehindDistance = 3f;
    [SerializeField, Range (1f, 5f)] private float lookBehindSpeed = 1f;
    [SerializeField, Range (1f , 5f)] private float camSpeed = 1f;

    [SerializeField] private Transform player;

    private float currentLookBehind;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = orthoSize;
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        //logica de la Camera para mirar hacia atras
        float input = Input.GetAxis("Horizontal");

        float targetLookBehind = input * lookBehindDistance;
        
        currentLookBehind = Mathf.Lerp(currentLookBehind, targetLookBehind, lookBehindSpeed * Time.deltaTime);

        Vector3 targetPos = new Vector3(player.position.x - currentLookBehind * lookBehindDistance, player.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, camSpeed * Time.deltaTime);
    }
}
