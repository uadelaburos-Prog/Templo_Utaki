using UnityEngine;

// Gancho secundario: jala objetos del mundo (plataformas de tracción, paredes reactivas).
// NO mueve al jugador — eso es trabajo del GrappleScript.
// Requisitos del objeto hookeable: Tag "Hookable" + layer asignado en hookMask.
// Asignar el LineRenderer propio en el Inspector (no compartir con GrappleScript).
[RequireComponent(typeof(Rigidbody2D))]
public class HookScript : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float    grabDistance = 10f;
    [SerializeField] private float    grabForce    = 15f;
    [SerializeField] private LayerMask hookMask;

    [Header("Hook Visual")]
    [SerializeField] private GameObject hookPrefab;
    [SerializeField] private float      hookScale = 1f;

    [Header("Vuelo del Hook")]
    [SerializeField] private float launchSpeed   = 20f;
    [SerializeField] private float hookGravity   = 18f;
    [SerializeField] private float maxFlightTime = 0.6f;

    [Header("Cuerda")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private int          segments           = 20;
    [Range(0, 20)]
    [SerializeField] private float        straightenSpeed    = 5f;
    [Range(0.01f, 4)]
    [SerializeField] private float        startWaveSize      = 2f;
    [SerializeField] private Color        ropeColor          = new Color(0.55f, 0.27f, 0.07f);
    [SerializeField] private float        ropeWidth          = 0.15f;
    public AnimationCurve ropeAnimationCurve;
    public AnimationCurve ropeProgressionCurve;

    private enum HookState { Idle, Launching, Attached, Retracting }
    private HookState state = HookState.Idle;

    private GameObject grabObject;
    private Rigidbody2D hookedRb;
    private Vector2 hookOffset;

    private GameObject currentHook;
    private Sprite     hookFallbackSprite;

    private Vector2 hookCurrentPos;
    private Vector2 hookVelocity;
    private float   flightTimer;
    private float   waveSize;

    private void Awake()
    {
        hookFallbackSprite = GenerateCircleSprite(16, ropeColor);

        if (line == null) return;
        line.useWorldSpace  = true;
        line.startWidth     = ropeWidth;
        line.endWidth       = ropeWidth;
        line.startColor     = ropeColor;
        line.endColor       = ropeColor;
        line.positionCount  = segments;
        line.enabled        = false;
    }

    private void Update()
    {
        HandleInput();
        UpdateHookVisual();

        switch (state)
        {
            case HookState.Attached:
                waveSize = Mathf.MoveTowards(waveSize, 0f, straightenSpeed * Time.deltaTime);
                UpdateLine(grabObject.transform.TransformPoint(hookOffset));
                if (Input.GetAxis("Mouse ScrollWheel") < 0f) PullObject();
                break;

            case HookState.Retracting:
                UpdateRetracting();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (state == HookState.Launching)
            UpdateLaunching();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(1) && state == HookState.Idle)
            TryHook();

        if (Input.GetMouseButtonUp(1) && state != HookState.Idle)
            Release();
    }

    private void TryHook()
    {
        Vector2 origin    = transform.position;
        Vector2 mousePos  = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - origin).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, grabDistance, hookMask);
        if (hit.collider == null || !hit.collider.CompareTag("Hookable")) return;

        hookCurrentPos = origin;
        hookVelocity   = direction * launchSpeed;
        flightTimer    = 0f;
        waveSize       = 0f;
        grabObject     = null;
        hookedRb       = null;

        SpawnHook(origin);
        if (line != null) { line.positionCount = segments; line.enabled = true; }
        state = HookState.Launching;
    }

    private void UpdateLaunching()
    {
        Vector2 prevPos = hookCurrentPos;

        hookVelocity   += Vector2.down * hookGravity * Time.fixedDeltaTime;
        hookCurrentPos += hookVelocity  * Time.fixedDeltaTime;
        flightTimer    += Time.fixedDeltaTime;

        UpdateLine(hookCurrentPos);

        RaycastHit2D hit = Physics2D.Linecast(prevPos, hookCurrentPos, hookMask);
        if (hit.collider != null && hit.collider.CompareTag("Hookable"))
        {
            grabObject = hit.collider.gameObject;
            hookedRb   = grabObject.GetComponent<Rigidbody2D>();
            hookOffset = grabObject.transform.InverseTransformPoint(hit.point);
            Attach(hit.point);
            return;
        }

        if (flightTimer >= maxFlightTime ||
            Vector2.Distance(hookCurrentPos, transform.position) > grabDistance)
        {
            Release();
        }
    }

    private void Attach(Vector2 punto)
    {
        hookCurrentPos = punto;
        waveSize       = startWaveSize;
        state          = HookState.Attached;
    }

    private void UpdateRetracting()
    {
        hookCurrentPos = Vector2.MoveTowards(hookCurrentPos, transform.position, 25f * Time.deltaTime);
        UpdateLine(hookCurrentPos);

        if (Vector2.Distance(hookCurrentPos, transform.position) < 0.1f)
        {
            if (currentHook != null) { Destroy(currentHook); currentHook = null; }
            if (line != null) line.enabled = false;
            state = HookState.Idle;
        }
    }

    private void PullObject()
    {
        if (hookedRb == null) return;

        Vector2 worldHookPoint  = grabObject.transform.TransformPoint(hookOffset);
        Vector2 dirToSelf       = ((Vector2)transform.position - worldHookPoint).normalized;
        float   distance        = Vector2.Distance(transform.position, worldHookPoint);

        if (distance < 1f)
        {
            Release();
            return;
        }

        hookedRb.AddForceAtPosition(dirToSelf * grabForce * distance, worldHookPoint, ForceMode2D.Force);
    }

    private void Release()
    {
        grabObject = null;
        hookedRb   = null;
        waveSize   = 0f;
        state      = HookState.Retracting;
    }

    private void UpdateHookVisual()
    {
        if (currentHook == null || state == HookState.Idle) return;

        currentHook.transform.position = hookCurrentPos;

        Vector2 dir = state == HookState.Launching
            ? hookVelocity
            : (Vector2)transform.position - hookCurrentPos;

        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            currentHook.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void UpdateLine(Vector2 gancho)
    {
        if (line == null) return;

        Vector2 jugador = transform.position;
        float   dist    = Vector2.Distance(jugador, gancho);

        for (int i = 0; i < segments; i++)
        {
            float t       = (float)i / (segments - 1);
            float curvedT = ropeProgressionCurve != null && ropeProgressionCurve.length > 0
                ? ropeProgressionCurve.Evaluate(t) : t;

            float   cuelgue = Mathf.Clamp(dist * 0.15f, 0.05f, 2.5f);
            Vector2 control = (jugador + gancho) * 0.5f - Vector2.up * cuelgue;

            Vector2 punto =
                Mathf.Pow(1 - curvedT, 2) * jugador +
                2 * (1 - curvedT) * curvedT * control +
                Mathf.Pow(curvedT, 2) * gancho;

            if (ropeAnimationCurve != null && ropeAnimationCurve.length > 0 && waveSize > 0f)
            {
                Vector2 ropeDir = (gancho - jugador).normalized;
                Vector2 perp    = new Vector2(-ropeDir.y, ropeDir.x);
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

    private Sprite GenerateCircleSprite(int size, Color color)
    {
        var    tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float   r = size * 0.5f - 0.5f;

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
