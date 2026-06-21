using UnityEngine;

public class CamaraScript : MonoBehaviour
{
    public static CamaraScript Instance { get; private set; }

    Camera cam;

    [Header("Side Viewer")]
    [SerializeField, Range (6f, 13f)] private float orthoSize = 8f;
    [SerializeField, Range (0f, 2f)] private float lookBehindDistance = 3f;
    [SerializeField, Range (1f, 5f)] private float lookBehindSpeed = 1f;
    [SerializeField, Range (1f, 6f)] private float camSpeed = 1f;

    [Header("Up&Down Viewer")]
    [SerializeField, Range (0f, 3f)] private float lookDownDistance = 3f;
    [SerializeField, Range (1f, 5f)] private float lookDownSpeed = 1f;

    [Header("Vertical Offset")]
    [SerializeField, Range (-3f, 5f)] private float verticalOffset = 2f;

    [Header("Terrain Awareness")]
    [SerializeField, Range(1f, 40f)] private float terrainHeightRef = 12f;
    [SerializeField, Range(0f, 6f)] private float terrainOffsetStrength = 3f;
    [SerializeField, Range(0.5f, 8f)] private float terrainOffsetSpeed = 2f;

    [Header("Camera Bounds (default / sin zona)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX =  20f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY =  20f;

    [Header("Variables")]
    [SerializeField] private float maxFallSpeed = 10f;

    [SerializeField] private Transform player;
    private Rigidbody2D rb;

    private float currentLookBehind;
    private float currentLookDown;
    private float _currentTerrainOffset;
    private float _inputHorizontal;

    private float _activeMinX, _activeMaxX, _activeMinY, _activeMaxY;

    private CameraZone _currentZone;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        rb  = player.GetComponent<Rigidbody2D>();
        cam = GetComponent<Camera>();
        cam.orthographicSize = orthoSize;

        _activeMinX = minX; _activeMaxX = maxX;
        _activeMinY = minY; _activeMaxY = maxY;

        // Detectar zona inicial (OnTriggerEnter2D no dispara para solapamientos previos al Enable).
        // Entre varias zonas solapadas al inicio, activa la de menor área.
        CameraZone startZone  = null;
        float       smallestA = float.MaxValue;
        foreach (CameraZone z in FindObjectsByType<CameraZone>(FindObjectsSortMode.None))
        {
            if (z.Contains(player.position) && z.Area < smallestA)
            {
                smallestA = z.Area;
                startZone = z;
            }
        }
        if (startZone != null) EnterZone(startZone);

        SnapToPlayer();
    }

    // Llamado por CameraZone.OnTriggerEnter2D. La zona más reciente siempre tiene prioridad.
    public void EnterZone(CameraZone zone)
    {
        if (zone == _currentZone) return;
        _currentZone = zone;
        _activeMinX = zone.MinX; _activeMaxX = zone.MaxX;
        _activeMinY = zone.MinY; _activeMaxY = zone.MaxY;
    }

    // Teletransporta la cámara al jugador sin lerp — llamar al spawnear o al hacer respawn.
    public void SnapToPlayer()
    {
        if (player == null || cam == null) return;

        currentLookBehind     = 0f;
        currentLookDown       = 0f;
        _currentTerrainOffset = 0f;

        Vector3 snapPos = new Vector3(
            player.position.x,
            player.position.y + verticalOffset,
            transform.position.z
        );

        if (useBounds)
        {
            float halfH = cam.orthographicSize;
            float halfW = cam.orthographicSize * cam.aspect;
            float cMinX = _activeMinX + halfW, cMaxX = _activeMaxX - halfW;
            float cMinY = _activeMinY + halfH, cMaxY = _activeMaxY - halfH;
            if (cMaxX > cMinX) snapPos.x = Mathf.Clamp(snapPos.x, cMinX, cMaxX);
            if (cMaxY > cMinY) snapPos.y = Mathf.Clamp(snapPos.y, cMinY, cMaxY);
            else                snapPos.y = (_activeMinY + _activeMaxY) * 0.5f;
        }

        transform.position = snapPos;
    }

    private void Update()
    {
        _inputHorizontal = Input.GetAxis("Horizontal");
    }

    private void LateUpdate()
    {
        float targetLookBehind = _inputHorizontal * lookBehindDistance;
        currentLookBehind = Mathf.Lerp(currentLookBehind, targetLookBehind, lookBehindSpeed * Time.deltaTime);

        if (rb.linearVelocityY < 0f)
        {
            float fallPercent = Mathf.Clamp01(-rb.linearVelocityY / maxFallSpeed);
            currentLookDown = Mathf.Lerp(currentLookDown, fallPercent * lookDownDistance, lookDownSpeed * Time.deltaTime);
        }
        else
        {
            currentLookDown = Mathf.Lerp(currentLookDown, 0f, lookDownSpeed * Time.deltaTime);
        }

        float heightAboveFloor = Mathf.Max(0f, player.position.y - _activeMinY);
        float normalizedHeight  = Mathf.Clamp01(heightAboveFloor / terrainHeightRef);
        float targetTerrainOffset = -normalizedHeight * terrainOffsetStrength;
        _currentTerrainOffset = Mathf.Lerp(_currentTerrainOffset, targetTerrainOffset, terrainOffsetSpeed * Time.deltaTime);

        Vector3 targetPos = new Vector3(
            player.position.x - currentLookBehind,
            player.position.y - currentLookDown + verticalOffset + _currentTerrainOffset,
            transform.position.z
        );

        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, camSpeed * Time.deltaTime);

        if (useBounds)
        {
            float halfH = cam.orthographicSize;
            float halfW = cam.orthographicSize * cam.aspect;
            float cMinX = _activeMinX + halfW, cMaxX = _activeMaxX - halfW;
            float cMinY = _activeMinY + halfH, cMaxY = _activeMaxY - halfH;

            if (cMaxX > cMinX) newPos.x = Mathf.Clamp(newPos.x, cMinX, cMaxX);
            if (cMaxY > cMinY) newPos.y = Mathf.Clamp(newPos.y, cMinY, cMaxY);
            else newPos.y = Mathf.Lerp(transform.position.y, (_activeMinY + _activeMaxY) * 0.5f, camSpeed * 4f * Time.deltaTime);
        }

        transform.position = newPos;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!useBounds) return;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Vector3 worldCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 worldSize   = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(worldCenter, worldSize);

        Camera c = GetComponent<Camera>();
        if (c != null)
        {
            float hH = c.orthographicSize;
            float hW = c.orthographicSize * c.aspect;
            float cx0 = minX + hW, cx1 = maxX - hW;
            float cy0 = minY + hH, cy1 = maxY - hH;
            if (cx1 > cx0 && cy1 > cy0)
            {
                Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
                Vector3 innerCenter = new Vector3((cx0 + cx1) * 0.5f, (cy0 + cy1) * 0.5f, 0f);
                Vector3 innerSize   = new Vector3(cx1 - cx0, cy1 - cy0, 0f);
                Gizmos.DrawWireCube(innerCenter, innerSize);
            }
        }
    }
#endif
}
