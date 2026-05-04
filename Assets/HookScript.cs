using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class HookScript : MonoBehaviour
{
    [SerializeField] private float grabDistance = 10f;
    [SerializeField] private float grabForce = 15f;
    [SerializeField] private LayerMask hookMask;

    [Header("Hook Prefab")]
    [SerializeField] private GameObject hookPrefab;
    [SerializeField] private float hookScale = 1f;

    [Header("Vuelo del Hook")]
    [SerializeField] private float launchSpeed = 20f;
    [SerializeField] private float hookGravity = 18f;
    [SerializeField] private float maxFlightTime = 0.6f;

    [Header("Animación de la Cuerda")]
    [SerializeField] private int segments = 20;
    [Range(0, 20)]
    [SerializeField] private float straightenLineSpeed = 5f;
    public AnimationCurve ropeAnimationCurve;
    [Range(0.01f, 4)]
    [SerializeField] private float startWaveSize = 2f;
    private float waveSize = 0f;

    [Header("Progresión de la Cuerda")]
    public AnimationCurve ropeProgressionCurve;

    [Header("Sprites")]
    [SerializeField] private Color ropeColor = new Color(0.55f, 0.27f, 0.07f);
    [SerializeField] private float ropeWidth = 0.15f;

    private enum HookState { idle, launching, attached, retracting }
    private HookState state = HookState.idle;

    private GameObject grabObject;
    private Rigidbody2D hookedObject;
    private LineRenderer line;
    private Vector2 hookOffSet;

    private GameObject currentHook;
    private Sprite hookFallbackSprite;

    private Vector2 hookCurrentPos;
    private Vector2 hookVelocity;
    private float flightTimer;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.startWidth = ropeWidth;
        line.endWidth = ropeWidth;
        line.startColor = ropeColor;
        line.endColor = ropeColor;
        line.positionCount = segments;
        line.enabled = false;

        hookFallbackSprite = GenerateCircleSprite(16, ropeColor);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && state == HookState.idle)
            TryHook();

        if (Input.GetMouseButtonUp(1) && state != HookState.idle)
            Release();

        switch (state)
        {
            case HookState.launching:
                UpdateLaunching();
                break;
            case HookState.attached:
                waveSize = Mathf.MoveTowards(waveSize, 0f, straightenLineSpeed * Time.deltaTime);
                UpdateLine(grabObject.transform.TransformPoint(hookOffSet));
                if (Input.GetAxis("Mouse ScrollWheel") < 0f) PullObject();
                break;
            case HookState.retracting:
                UpdateRetracting();
                break;
        }

        // Mover y rotar el hook visual
        if (currentHook != null && state != HookState.idle)
        {
            currentHook.transform.position = hookCurrentPos;

            Vector2 dir = state == HookState.launching
                ? hookVelocity
                : (Vector2)transform.position - hookCurrentPos;

            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                currentHook.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    private void TryHook()
    {
        Vector2 origin = transform.position;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - origin).normalized;

        // Verificar que hay algo hookeable en esa dirección antes de lanzar
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, grabDistance, hookMask);
        if (hit.collider == null || !hit.collider.CompareTag("Hookable")) return;

        hookCurrentPos = origin;
        hookVelocity = direction * launchSpeed;
        flightTimer = 0f;
        waveSize = 0f;
        grabObject = null;
        hookedObject = null;

        SpawnHook(origin);
        line.positionCount = segments;
        line.enabled = true;
        state = HookState.launching;
    }

    private void UpdateLaunching()
    {
        Vector2 prevPos = hookCurrentPos;

        // Física de vuelo parabólico
        hookVelocity += Vector2.down * hookGravity * Time.deltaTime;
        hookCurrentPos += hookVelocity * Time.deltaTime;
        flightTimer += Time.deltaTime;

        UpdateLine(hookCurrentPos);

        // Linecast frame anterior → actual: detecta la superficie exacta sin tunelización
        RaycastHit2D hit = Physics2D.Linecast(prevPos, hookCurrentPos, hookMask);
        if (hit.collider != null && hit.collider.CompareTag("Hookable"))
        {
            grabObject = hit.collider.gameObject;
            hookedObject = grabObject.GetComponent<Rigidbody2D>();
            hookOffSet = (Vector2)grabObject.transform.InverseTransformPoint(hit.point);
            Attach(hit.point);
            return;
        }

        // Fallo: tiempo excedido o fuera de rango → retraer
        if (flightTimer >= maxFlightTime ||
            Vector2.Distance(hookCurrentPos, transform.position) > grabDistance)
        {
            Release();
        }
    }

    private void Attach(Vector2 punto)
    {
        hookCurrentPos = punto;
        waveSize = startWaveSize;
        state = HookState.attached;
    }

    private void UpdateRetracting()
    {
        hookCurrentPos = Vector2.MoveTowards(hookCurrentPos, transform.position, 25f * Time.deltaTime);
        UpdateLine(hookCurrentPos);

        if (Vector2.Distance(hookCurrentPos, transform.position) < 0.1f)
        {
            if (currentHook != null) { Destroy(currentHook); currentHook = null; }
            line.enabled = false;
            state = HookState.idle;
        }
    }

    private void PullObject()
    {
        if (hookedObject == null) return;

        Vector2 worldHookPoint = grabObject.transform.TransformPoint(hookOffSet);
        Vector2 directionToSelf = ((Vector2)transform.position - worldHookPoint).normalized;
        float distance = Vector2.Distance(transform.position, worldHookPoint);

        if (distance < 1f) Release();
        else hookedObject.AddForceAtPosition(directionToSelf * grabForce * distance, worldHookPoint, ForceMode2D.Force);
    }

    private void UpdateLine(Vector2 gancho)
    {
        Vector2 jugador = transform.position;
        float dist = Vector2.Distance(jugador, gancho);

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);

            float curvedT = ropeProgressionCurve != null && ropeProgressionCurve.length > 0
                ? ropeProgressionCurve.Evaluate(t) : t;

            float cuelgue = Mathf.Clamp(dist * 0.15f, 0.05f, 2.5f);
            Vector2 control = (jugador + gancho) * 0.5f - Vector2.up * cuelgue;

            Vector2 punto =
                Mathf.Pow(1 - curvedT, 2) * jugador +
                2 * (1 - curvedT) * curvedT * control +
                Mathf.Pow(curvedT, 2) * gancho;

            if (ropeAnimationCurve != null && ropeAnimationCurve.length > 0 && waveSize > 0f)
            {
                Vector2 ropeDir = (gancho - jugador).normalized;
                Vector2 perp = new Vector2(-ropeDir.y, ropeDir.x);
                punto += perp * ropeAnimationCurve.Evaluate(t) * waveSize;
            }

            line.SetPosition(i, punto);
        }
    }

    private void SpawnHook(Vector2 pos)
    {
        if (currentHook != null) Destroy(currentHook);

        if (hookPrefab != null)
        {
            currentHook = Instantiate(hookPrefab, pos, Quaternion.identity);
        }
        else
        {
            currentHook = new GameObject("Hook");
            currentHook.transform.position = pos;
            var sr = currentHook.AddComponent<SpriteRenderer>();
            sr.sprite = hookFallbackSprite;
        }
        currentHook.transform.localScale = Vector3.one * hookScale;
    }

    private void Release()
    {
        grabObject = null;
        hookedObject = null;
        waveSize = 0f;
        state = HookState.retracting;
    }

    private Sprite GenerateCircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float r = size * 0.5f - 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y,
                    Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) <= r
                        ? color : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, grabDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }
}