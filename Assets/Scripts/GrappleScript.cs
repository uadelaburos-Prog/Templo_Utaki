using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Un único gancho, dos comportamientos según lo que golpea:
//   → superficie en grappleLayerMask  = columpio (DistanceJoint2D, péndulo)
//   → objeto con tag "Hookable"        = tracción  (AddForce al Rigidbody2D del objeto, scroll)
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class GrappleScript : MonoBehaviour
{
    [Header("Detección")]
    [Tooltip("Distancia máxima con carga al 100%.")]
    [SerializeField] private float     maxGrappleDistance = 10f;
    [SerializeField] private float     maxSwingVelocity   = 15f;
    [SerializeField] private LayerMask grappleLayerMask;
    [Tooltip("Sólidos que bloquean el gancho sin engancharlo (Floor, Obstacle…).")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Layer de objetos hookeables (ReactiveWall, plataformas de tracción…).")]
    [SerializeField] private LayerMask hookMask;
    [Tooltip("Fuerza aplicada al objeto hookeable al jalar con scroll abajo.")]
    [SerializeField] private float     grabForce = 15f;

    [Header("Vuelo")]
    [SerializeField] private float launchSpeed       = 20f;
    [SerializeField] private float retractSpeed      = 25f;
    [SerializeField] private float failRetractSpeed  = 8f;
    [SerializeField] private float hookGravity       = 18f;
    [SerializeField] private float maxFlightTime     = 0.6f;
    [SerializeField] private float failCooldown      = 0.3f;

    [Header("Carga")]
    [SerializeField] private float      minGrappleDistance = 3f;
    [SerializeField] private float      maxChargeTime      = 1.5f;
    [SerializeField] private GameObject chargeBarRoot;
    [SerializeField] private Image      chargeBarImage;

    [Header("Swing")]
    [SerializeField] private float snapRadius    = 0.4f;
    [SerializeField] private float swingDamping  = 0.02f;
    [SerializeField] private float climbSpeed    = 6f;
    [SerializeField] private float minRopeLength = 1f;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.4f, 1f);

    [Header("Cuerda")]
    [SerializeField] private int   segments       = 20;
    [Range(0, 20)]
    [SerializeField] private float straightenSpeed = 5f;
    [Range(0.01f, 4)]
    [SerializeField] private float startWaveSize   = 2f;
    [SerializeField] private Color ropeColor       = new Color(0.55f, 0.27f, 0.07f);
    [SerializeField] private float ropeWidth       = 0.15f;
    public AnimationCurve ropeAnimationCurve;
    public AnimationCurve ropeProgressionCurve;

    [Header("Visual Hook")]
    [SerializeField] private GameObject hookPrefab;
    [SerializeField] private float      hookScale = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxLanzar;
    [SerializeField] private AudioClip sfxEnganchar;

    // ── Estado ────────────────────────────────────────────────────

    private enum GrappleState { Idle, Charging, Launching, Attached, Retracting }
    private enum AttachType   { Swing, Pull }

    private GrappleState state      = GrappleState.Idle;
    private AttachType   attachType = AttachType.Swing;

    [HideInInspector] public bool            isGrappling;
    [HideInInspector] public DistanceJoint2D joint;
    [HideInInspector] public LineRenderer    line;

    private Rigidbody2D    rb;
    private PlayerMovement playerMovement;

    private Vector2    grapplePoint;
    private Vector2    hookCurrentPos;
    private Vector2    hookVelocity;
    private float      flightTimer;
    private float      waveSize;
    private float      cooldownTimer;
    private float      chargeTimer;
    private float      currentRopeLength;
    private float      launchMaxDistance;   // distancia real según carga
    private bool       retractFailed;       // true = fallo, false = soltó el botón
    private bool       hookGrounded;        // el hook tocó el suelo durante la retracción
    private GameObject currentHook;
    private Sprite     hookFallbackSprite;

    // Estado de tracción
    private GameObject  grabObject;
    private Rigidbody2D hookedRb;
    private Vector2     hookOffset;

    private readonly List<SpriteRenderer> highlightedRenderers = new List<SpriteRenderer>();
    private readonly List<Color>          originalColors        = new List<Color>();

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        joint          = GetComponent<DistanceJoint2D>();
        line           = GetComponent<LineRenderer>();
        playerMovement = GetComponent<PlayerMovement>();

        joint.enabled               = false;
        joint.autoConfigureDistance = false;
        joint.maxDistanceOnly       = true;
        joint.enableCollision       = false;

        line.useWorldSpace = true;
        line.startWidth    = ropeWidth;
        line.endWidth      = ropeWidth;
        line.startColor    = ropeColor;
        line.endColor      = ropeColor;

        hookFallbackSprite = GenerateCircleSprite(16, ropeColor);
    }

    private void OnEnable()
    {
        waveSize           = startWaveSize;
        line.positionCount = segments;
        for (int i = 0; i < segments; i++) line.SetPosition(i, transform.position);
    }

    private void OnDisable()
    {
        line.enabled = false;
        isGrappling  = false;
        SetGrappleableHighlight(false);
        if (chargeBarRoot != null) chargeBarRoot.SetActive(false);
        if (currentHook   != null) { Destroy(currentHook); currentHook = null; }
        ClearPullState();
    }

    // ── Update / FixedUpdate ──────────────────────────────────────

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        HandleInput();
        UpdateHookVisual();

        switch (state)
        {
            case GrappleState.Charging:
                UpdateCharging();
                break;

            case GrappleState.Launching:
                UpdateLaunching();
                break;

            case GrappleState.Attached:
                waveSize = Mathf.MoveTowards(waveSize, 0f, straightenSpeed * Time.deltaTime);
                DrawRope(RopeEndpoint());

                if (attachType == AttachType.Swing)
                    GrappleSwing();
                else
                    UpdatePull();
                break;

            case GrappleState.Retracting:
                UpdateRetracting();
                break;
        }
    }

    private void FixedUpdate()
    {
        // Cancela velocidad radial saliente cuando la cuerda del columpio está tensa
        if (state == GrappleState.Attached && attachType == AttachType.Swing)
        {
            float dist = Vector2.Distance(rb.position, (Vector2)joint.connectedAnchor);
            if (dist >= joint.distance * 0.98f)
            {
                Vector2 dir = (rb.position - (Vector2)joint.connectedAnchor).normalized;
                float   vel = Vector2.Dot(rb.linearVelocity, dir);
                if (vel > 0f) rb.linearVelocity -= dir * vel;
            }
        }
    }

    // ── Input ─────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && cooldownTimer <= 0f &&
            (state == GrappleState.Idle || state == GrappleState.Retracting))
        {
            state        = GrappleState.Charging;
            chargeTimer  = 0f;
            line.enabled = false;
            if (currentHook    != null) { Destroy(currentHook); currentHook = null; }
            if (chargeBarImage != null) chargeBarImage.fillAmount = 0f;
            if (chargeBarRoot  != null) chargeBarRoot.SetActive(true);
            SetGrappleableHighlight(true);
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (state == GrappleState.Charging)
                Launch(chargeTimer / maxChargeTime);
            else if (state == GrappleState.Attached || state == GrappleState.Launching)
                GrappleRetract();
        }
    }

    // ── Lanzamiento ───────────────────────────────────────────────

    private void UpdateCharging()
    {
        chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, maxChargeTime);
        if (chargeBarImage != null)
            chargeBarImage.fillAmount = chargeTimer / maxChargeTime;
    }

    private void Launch(float chargePercent)
    {
        SetGrappleableHighlight(false);
        Vector2 mouse     = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin    = transform.position;
        float   dist      = Mathf.Lerp(minGrappleDistance, maxGrappleDistance, chargePercent);
        Vector2 direction = (mouse - origin).normalized;

        grapplePoint      = origin + direction * dist;
        hookCurrentPos    = origin;
        hookVelocity      = direction * launchSpeed;
        flightTimer       = 0f;
        launchMaxDistance = dist;

        SpawnHookGO(origin);
        AudioManager.instance?.FxSoundEffect(sfxLanzar, transform, 1f);

        waveSize           = 0f;
        line.enabled       = true;
        line.positionCount = segments;
        isGrappling        = false;
        state              = GrappleState.Launching;
        rb.linearDamping   = 0f;

        if (chargeBarImage != null) chargeBarImage.fillAmount = 0f;
        if (chargeBarRoot  != null) chargeBarRoot.SetActive(false);
    }

    private void UpdateLaunching()
    {
        Vector2 prevPos = hookCurrentPos;

        hookVelocity   += Vector2.down * hookGravity * Time.deltaTime;
        hookCurrentPos += hookVelocity  * Time.deltaTime;
        flightTimer    += Time.deltaTime;
        DrawRope(hookCurrentPos);

        // Linecast contra grappleable, hookable y obstáculos en un solo paso
        LayerMask todo       = grappleLayerMask | obstacleMask | hookMask;
        RaycastHit2D hit     = Physics2D.Linecast(prevPos, hookCurrentPos, todo);

        if (hit.collider != null)
        {
            bool esGrappleable = ((1 << hit.collider.gameObject.layer) & grappleLayerMask) != 0;
            bool esHookable    = ((1 << hit.collider.gameObject.layer) & hookMask) != 0
                                 && hit.collider.CompareTag("Hookable");

            if      (esGrappleable) { SnapAndAttach(hit.point); return; }
            else if (esHookable)    { AttachPull(hit.collider, hit.point); return; }
            else                    { cooldownTimer = failCooldown; GrappleRetract(failed: true); return; }
        }

        // Snap assist solo para superficies de columpio
        Collider2D snap = Physics2D.OverlapCircle(hookCurrentPos, snapRadius, grappleLayerMask);
        if (snap != null) { SnapAndAttach(snap.ClosestPoint(hookCurrentPos)); return; }

        if (flightTimer >= maxFlightTime ||
            Vector2.Distance(hookCurrentPos, transform.position) > launchMaxDistance)
        {
            cooldownTimer = failCooldown;
            GrappleRetract(failed: true);
        }
    }

    // ── Columpio ──────────────────────────────────────────────────

    private void SnapAndAttach(Vector2 punto)
    {
        hookCurrentPos        = punto;
        joint.connectedAnchor = punto;
        currentRopeLength     = Vector2.Distance(transform.position, punto);
        joint.distance        = currentRopeLength;
        joint.maxDistanceOnly = true;
        joint.enabled         = true;
        isGrappling           = true;
        rb.linearDamping      = swingDamping;
        attachType            = AttachType.Swing;
        state                 = GrappleState.Attached;
        AudioManager.instance?.FxSoundEffect(sfxEnganchar, transform, 1f);
    }

    private void GrappleSwing()
    {
        float climbInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))        climbInput = -1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) climbInput =  1f;

        if (climbInput != 0f)
        {
            currentRopeLength = Mathf.Clamp(
                currentRopeLength + climbInput * climbSpeed * Time.deltaTime,
                minRopeLength, maxGrappleDistance);
            joint.distance = currentRopeLength;
        }
        else if (playerMovement != null && playerMovement.IsGrounded)
        {
            float distActual = Vector2.Distance(transform.position, hookCurrentPos);
            if (distActual > currentRopeLength)
            {
                currentRopeLength = Mathf.Min(distActual, maxGrappleDistance);
                joint.distance    = currentRopeLength;
            }
        }
    }

    // ── Tracción ──────────────────────────────────────────────────

    private void AttachPull(Collider2D objetivo, Vector2 punto)
    {
        grabObject     = objetivo.gameObject;
        hookedRb       = grabObject.GetComponent<Rigidbody2D>();
        hookOffset     = grabObject.transform.InverseTransformPoint(punto);
        hookCurrentPos = punto;
        waveSize       = startWaveSize;
        attachType     = AttachType.Pull;
        state          = GrappleState.Attached;

        grabObject.GetComponent<ReactiveWall>()?.OnHooked();
        AudioManager.instance?.FxSoundEffect(sfxEnganchar, transform, 1f);
    }

    private void UpdatePull()
    {
        if (grabObject == null) { GrappleRetract(); return; }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            PullObject();
    }

    private void PullObject()
    {
        if (hookedRb == null) return;

        Vector2 worldPoint = grabObject.transform.TransformPoint(hookOffset);
        Vector2 dir        = ((Vector2)transform.position - worldPoint).normalized;
        float   distance   = Vector2.Distance(transform.position, worldPoint);

        if (distance < 1f) { GrappleRetract(); return; }

        hookedRb.AddForceAtPosition(dir * grabForce * distance, worldPoint, ForceMode2D.Force);
    }

    private void ClearPullState()
    {
        grabObject?.GetComponent<ReactiveWall>()?.OnReleased();
        grabObject = null;
        hookedRb   = null;
    }

    // ── Retracción ────────────────────────────────────────────────

    public void GrappleRetract(bool failed = false)
    {
        joint.enabled    = false;
        isGrappling      = false;
        rb.linearDamping = 0f;
        retractFailed    = failed;
        hookVelocity     = Vector2.zero;
        hookGrounded     = false;
        ClearPullState();
        state = GrappleState.Retracting;

        if (attachType == AttachType.Swing &&
            rb.linearVelocity.magnitude > maxSwingVelocity)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSwingVelocity;
    }

    private void UpdateRetracting()
    {
        float speed = retractFailed ? failRetractSpeed : retractSpeed;

        // Detectar contacto con el suelo
        if (!hookGrounded && Physics2D.OverlapCircle(hookCurrentPos, 0.1f, obstacleMask))
            hookGrounded = true;

        if (hookGrounded)
        {
            // Tocó el piso: retracción directa sin gravedad
            hookCurrentPos = Vector2.MoveTowards(hookCurrentPos, transform.position, retractSpeed * Time.deltaTime);
        }
        else
        {
            // En el aire: gravedad continúa afectando el hook
            hookVelocity   += Vector2.down * hookGravity * Time.deltaTime;
            Vector2 toPlayer = ((Vector2)transform.position - hookCurrentPos).normalized;
            hookCurrentPos  += (toPlayer * speed + hookVelocity) * Time.deltaTime;
        }

        DrawRope(hookCurrentPos);

        if (Vector2.Distance(hookCurrentPos, transform.position) < 0.2f)
        {
            if (currentHook != null) { Destroy(currentHook); currentHook = null; }
            line.enabled       = false;
            line.positionCount = 2;
            line.SetPosition(0, Vector2.zero);
            line.SetPosition(1, Vector2.zero);
            hookVelocity       = Vector2.zero;
            state              = GrappleState.Idle;
        }
    }

    // ── Utilidades ────────────────────────────────────────────────

    // En tracción la cuerda sigue al objeto; en columpio sigue el punto fijo
    private Vector2 RopeEndpoint()
    {
        if (attachType == AttachType.Pull && grabObject != null)
            return grabObject.transform.TransformPoint(hookOffset);
        return hookCurrentPos;
    }

    private void UpdateHookVisual()
    {
        if (currentHook == null || state == GrappleState.Idle || state == GrappleState.Charging) return;

        currentHook.transform.position = hookCurrentPos;

        Vector2 dir = state == GrappleState.Launching
            ? grapplePoint - hookCurrentPos
            : (Vector2)transform.position - hookCurrentPos;

        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            currentHook.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void DrawRope(Vector2 endPoint)
    {
        if (line == null) return;
        line.positionCount = segments;
        Vector2 jugador = transform.position;
        float   dist    = Vector2.Distance(jugador, endPoint);

        for (int i = 0; i < segments; i++)
        {
            float t       = (float)i / (segments - 1);
            float curvedT = ropeProgressionCurve != null && ropeProgressionCurve.length > 0
                ? ropeProgressionCurve.Evaluate(t) : t;

            float   cuelgue = Mathf.Clamp(dist * 0.15f, 0.05f, 2.5f);
            Vector2 control = (jugador + endPoint) * 0.5f - Vector2.up * cuelgue;

            Vector2 punto =
                Mathf.Pow(1 - curvedT, 2) * jugador +
                2 * (1 - curvedT) * curvedT * control +
                Mathf.Pow(curvedT, 2) * endPoint;

            if (ropeAnimationCurve != null && ropeAnimationCurve.length > 0 && waveSize > 0f)
            {
                Vector2 ropeDir = (endPoint - jugador).normalized;
                Vector2 perp    = new Vector2(-ropeDir.y, ropeDir.x);
                punto += perp * ropeAnimationCurve.Evaluate(t) * waveSize;
            }

            line.SetPosition(i, punto);
        }
    }

    private void SpawnHookGO(Vector2 pos)
    {
        if (currentHook != null) Destroy(currentHook);

        if (hookPrefab != null)
            currentHook = Instantiate(hookPrefab, pos, Quaternion.identity);
        else
        {
            currentHook                    = new GameObject("Hook");
            currentHook.transform.position = pos;
            currentHook.AddComponent<SpriteRenderer>().sprite = hookFallbackSprite;
        }

        currentHook.transform.localScale = Vector3.one * hookScale;
    }

    private Sprite GenerateCircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
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

    private void SetGrappleableHighlight(bool on)
    {
        if (on)
        {
            highlightedRenderers.Clear();
            originalColors.Clear();
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, maxGrappleDistance, grappleLayerMask))
            {
                var sr = col.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                highlightedRenderers.Add(sr);
                originalColors.Add(sr.color);
                sr.color = highlightColor;
            }
        }
        else
        {
            for (int i = 0; i < highlightedRenderers.Count; i++)
                if (highlightedRenderers[i] != null)
                    highlightedRenderers[i].color = originalColors[i];
            highlightedRenderers.Clear();
            originalColors.Clear();
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Vector2 origin = transform.position;

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(origin, maxGrappleDistance);

        if (!Application.isPlaying || Camera.main == null) return;

        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
        Gizmos.DrawWireSphere(mouse, snapRadius);

        if (state == GrappleState.Launching ||
            state == GrappleState.Attached  ||
            state == GrappleState.Retracting)
        {
            // Verde = columpio, naranja = tracción
            Gizmos.color = attachType == AttachType.Pull
                ? new Color(1f, 0.5f, 0f, 0.8f)
                : Color.green;
            float s = 0.15f;
            Gizmos.DrawLine(grapplePoint + Vector2.left * s, grapplePoint + Vector2.right * s);
            Gizmos.DrawLine(grapplePoint + Vector2.down * s, grapplePoint + Vector2.up    * s);
            Gizmos.DrawWireSphere(grapplePoint, 0.12f);
        }

        if (state == GrappleState.Launching)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hookCurrentPos, 0.1f);
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawLine(origin, hookCurrentPos);
        }

        if (state == GrappleState.Attached && attachType == AttachType.Swing)
        {
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.6f);
            Gizmos.DrawLine(origin, grapplePoint);
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.2f);
            Gizmos.DrawWireSphere(grapplePoint, joint != null ? joint.distance : 0f);
        }
    }
}
