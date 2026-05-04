using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class GrappleScript : MonoBehaviour
{
    [Tooltip("Distancia máxima a la que puede llegar el gancho con carga al 100% (unidades).")]
    [SerializeField] private float maxGrappleDistance = 10f;
    [Tooltip("Velocidad máxima permitida al soltar el gancho. Limita el impulso transferido al jugador (u/s).")]
    [SerializeField] private float maxSwingVelocity = 15f;
    [Tooltip("Layers que el gancho puede enganchar. Asignar la layer Grappleable.")]
    [SerializeField] private LayerMask grappleLayerMask;
    [Tooltip("Layers sólidas que bloquean el hook pero NO permiten enganche. El gancho rebota y retrae con cooldown. Asignar Floor, Obstacle, etc. GDD §13.2.")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("Velocidad a la que el hook viaja desde el jugador hasta el punto objetivo (u/s).")]
    [SerializeField] private float launchSpeed = 20f;
    [Tooltip("Velocidad a la que el hook regresa al jugador al retraer la cuerda (u/s).")]
    [SerializeField] private float retractSpeed = 25f;
    [Tooltip("Gravedad aplicada al hook en vuelo. Curva la trayectoria como un lanzamiento real (u/s²). GDD §5.2.")]
    [SerializeField] private float hookGravity = 18f;
    [Tooltip("Tiempo máximo de vuelo antes de considerar fallo y retraer (segundos). GDD §5.2 pide 0.3–0.5s.")]
    [SerializeField] private float maxFlightTime = 0.6f;
    [Tooltip("Cooldown tras fallar el lanzamiento antes de poder volver a cargar el gancho (segundos). GDD §5.2.")]
    [SerializeField] private float failCooldown = 0.3f;
    private float cooldownTimer = 0f;
    private float flightTimer   = 0f;

    [HideInInspector] public DistanceJoint2D joint;
    private Rigidbody2D rb;
    [HideInInspector] public LineRenderer line;
    private PlayerMovement playerMovement;

    [HideInInspector] public bool isGrappling = false;
    private Vector2 grapplePoint;
    public GameObject hookPrefab;
    private GameObject currentHook;

    private enum GrappleState { idle, charging, launching, attached, retracting }
    private GrappleState state = GrappleState.idle;

    private Vector2 hookCurrentPos;
    private Vector2 hookVelocity;

    [Header("Carga")]
    [Tooltip("Distancia de lanzamiento con carga al 0% (toque rápido sin cargar) (unidades).")]
    [SerializeField] private float minGrappleDistance = 3f;
    [Tooltip("Tiempo en segundos para alcanzar la carga máxima (100%) sosteniendo el click izquierdo.")]
    [SerializeField] private float maxChargeTime = 1.5f;
    [Tooltip("GameObject raíz del Canvas de la barra de carga. Se activa al cargar y se oculta al lanzar.")]
    [SerializeField] private GameObject chargeBarRoot;
    [Tooltip("Image con Fill Method que se llena proporcionalmente al nivel de carga actual (0–1).")]
    [SerializeField] private Image chargeBarImage;
    private float chargeTimer = 0f;

    [Header("Highlight al Cargar")]
    [Tooltip("Color aplicado a los SpriteRenderers de objetos grappleables dentro del rango al iniciar la carga.")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.4f, 1f);
    private readonly List<SpriteRenderer> highlightedRenderers = new List<SpriteRenderer>();
    private readonly List<Color>          originalColors        = new List<Color>();

    [Header("Snap")]
    [Tooltip("Radio en el que el hook detecta superficies grappleables durante el vuelo y se engancha automáticamente (unidades). GDD §5.2 = 1.5u.")]
    [SerializeField] private float snapRadius = 1.5f;

    [Header("Swing")]
    [Tooltip("Resistencia al movimiento del péndulo (rb.linearDamping mientras está enganchado). GDD §5.2 ≈ 0.02 = 98% de energía retenida por frame.")]
    [SerializeField] private float swingDamping = 0.02f;

    [Header("Escalado de Cuerda")]
    [Tooltip("Velocidad a la que el jugador sube o baja por la cuerda presionando W/S (u/s).")]
    [SerializeField] private float climbSpeed    = 6f;
    [Tooltip("Longitud mínima de la cuerda al subir. Evita que el jugador colisione con el punto de anclaje (unidades).")]
    [SerializeField] private float minRopeLength = 1f;
    private float currentRopeLength;

    [Header("Configuración General")]
    [Tooltip("Cantidad de puntos del LineRenderer de la cuerda. Más segmentos = curva más suave, mayor costo de cálculo.")]
    [SerializeField] private int segments = 20;
    [Tooltip("Velocidad a la que la onda de la cuerda se endereza tras el lanzamiento. 0 = no se endereza, 20 = instantáneo.")]
    [Range(0, 20)][SerializeField] private float straightenLineSpeed = 5;

    [Header("Animación de la Cuerda:")]
    [Tooltip("Curva de la onda animada de la cuerda. Eje X = posición a lo largo de la cuerda (0–1), Eje Y = amplitud del desplazamiento perpendicular.")]
    public AnimationCurve ropeAnimationCurve;
    [Tooltip("Amplitud inicial de la onda al lanzar el gancho. Decrece hasta 0 según straightenLineSpeed.")]
    [Range(0.01f, 4)][SerializeField] private float StartWaveSize = 2;
    float waveSize = 0;

    [Header("Progresión de la Cuerda:")]
    [Tooltip("Redistribuye los segmentos a lo largo de la cuerda. Permite concentrar puntos en la zona de cuelgue para mayor detalle visual.")]
    public AnimationCurve ropeProgressionCurve;

    [Header("Sprites")]
    [Tooltip("Color de la cuerda (LineRenderer) y del hook procedural cuando no hay hookPrefab asignado.")]
    [SerializeField] private Color ropeColor = new Color(0.55f, 0.27f, 0.07f);
    [Tooltip("Ancho visual del LineRenderer de la cuerda en unidades de mundo.")]
    [SerializeField] private float ropeWidth = 0.15f;
    [Tooltip("Escala del GameObject hook al instanciarlo. Aumentar para hacer el gancho más grande visualmente.")]
    [SerializeField] private float hookScale = 1f;
    private Sprite hookFallbackSprite;

    private void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        joint          = GetComponent<DistanceJoint2D>();
        line           = GetComponent<LineRenderer>();
        playerMovement = GetComponent<PlayerMovement>();

        joint.enabled = false;
        joint.autoConfigureDistance = false;
        joint.maxDistanceOnly = true;
        joint.enableCollision = false;
        line.useWorldSpace = true;
        line.startWidth    = ropeWidth;
        line.endWidth      = ropeWidth;
        line.startColor    = ropeColor;
        line.endColor      = ropeColor;

        hookFallbackSprite = GenerateCircleSprite(16, ropeColor);
    }

    private void OnEnable()
    {
        waveSize = StartWaveSize;
        line.positionCount = segments;
        LinePointsToFirePoint();
    }

    private void OnDisable()
    {
        line.enabled = false;
        isGrappling = false;
        SetGrappleableHighlight(false);
        if (chargeBarRoot != null) chargeBarRoot.SetActive(false);
        if (currentHook != null) { Destroy(currentHook); currentHook = null; }
    }

    private void LinePointsToFirePoint()
    {
        for (int i = 0; i < segments; i++)
        {
            line.SetPosition(i, transform.position);
        }
    }

    void FixedUpdate()
    {
        if (state != GrappleState.attached) return;

        // Cancela la velocidad radial saliente cuando la cuerda está tensa
        // Evita el rebote/salto al llegar al límite del DistanceJoint2D
        float dist = Vector2.Distance(rb.position, (Vector2)joint.connectedAnchor);
        if (dist >= joint.distance * 0.98f)
        {
            Vector2 fueraDelAncla = (rb.position - (Vector2)joint.connectedAnchor).normalized;
            float velocidadSaliente = Vector2.Dot(rb.linearVelocity, fueraDelAncla);
            if (velocidadSaliente > 0f)
                rb.linearVelocity -= fueraDelAncla * velocidadSaliente;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Mouse0) && cooldownTimer <= 0f && (state == GrappleState.idle || state == GrappleState.retracting))
        {
            state = GrappleState.charging;
            chargeTimer = 0f;
            line.enabled = false;
            if (currentHook != null) { Destroy(currentHook); currentHook = null; }
            if (chargeBarImage != null) chargeBarImage.fillAmount = 0f;
            if (chargeBarRoot != null) chargeBarRoot.SetActive(true);
            SetGrappleableHighlight(true);
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (state == GrappleState.charging)
                GrappleLaunch(chargeTimer / maxChargeTime);
            else if (state == GrappleState.attached || state == GrappleState.launching)
                GrappleRetract();
        }

        switch (state)
        {
            case GrappleState.charging:
                UpdateCharging();
                break;
            case GrappleState.launching:
                UpdateLaunching();
                break;
            case GrappleState.attached:
                GrappleSwing();
                waveSize = Mathf.MoveTowards(waveSize, 0f, straightenLineSpeed * Time.deltaTime);
                UpdateLine(hookCurrentPos);
                break;
            case GrappleState.retracting:
                UpdateRetracting();
                break;
        }

        if (currentHook != null && state != GrappleState.idle && state != GrappleState.charging)
        {
            currentHook.transform.position = hookCurrentPos;
            Vector2 dir = Vector2.zero;
            if (state == GrappleState.launching)
                dir = grapplePoint - hookCurrentPos;
            else if (state == GrappleState.retracting)
                dir = (Vector2)transform.position - hookCurrentPos;
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                currentHook.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    private void UpdateCharging()
    {
        chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, maxChargeTime);
        if (chargeBarImage != null)
            chargeBarImage.fillAmount = chargeTimer / maxChargeTime;
    }

    private void UpdateRetracting()
    {
        hookCurrentPos = Vector2.MoveTowards(hookCurrentPos, transform.position, retractSpeed * Time.deltaTime);
        UpdateLine(hookCurrentPos);

        if (Vector2.Distance(hookCurrentPos, transform.position) < 0.05f)
        {
            if (currentHook != null) { Destroy(currentHook); currentHook = null; }
            line.enabled = false;
            line.positionCount = 2;
            line.SetPosition(0, Vector2.zero);
            line.SetPosition(1, Vector2.zero);
            state = GrappleState.idle;
        }
    }

    private void UpdateLaunching()
    {
        Vector2 prevPos = hookCurrentPos;

        // Trayectoria parabólica: integra velocidad con gravedad (GDD §5.2, hookGravity = 18)
        hookVelocity += Vector2.down * hookGravity * Time.deltaTime;
        hookCurrentPos += hookVelocity * Time.deltaTime;
        flightTimer += Time.deltaTime;
        UpdateLine(hookCurrentPos);

        // Linecast prevPos → hookCurrentPos: detecta cualquier superficie sólida
        // Evita tunelización a alta velocidad y resuelve grappleable vs obstacle en el mismo chequeo
        LayerMask combinada = grappleLayerMask | obstacleMask;
        RaycastHit2D hit = Physics2D.Linecast(prevPos, hookCurrentPos, combinada);
        if (hit.collider != null)
        {
            bool esGrappleable = ((1 << hit.collider.gameObject.layer) & grappleLayerMask) != 0;
            if (esGrappleable)
            {
                SnapAndAttach(hit.point);
            }
            else
            {
                // Superficie sólida no-grappleable (Floor, Obstacle, etc.) → retrae con cooldown
                cooldownTimer = failCooldown;
                GrappleRetract();
            }
            return;
        }

        // Snap assist: si ningún linecast impactó, buscar grappleable cercano al hook
        Collider2D snap = Physics2D.OverlapCircle(hookCurrentPos, snapRadius, grappleLayerMask);
        if (snap != null)
        {
            SnapAndAttach(snap.ClosestPoint(hookCurrentPos));
            return;
        }

        // Falla: tiempo excedido o fuera del rango máximo — retraer con cooldown
        float distFromPlayer = Vector2.Distance(hookCurrentPos, transform.position);
        if (flightTimer >= maxFlightTime || distFromPlayer > maxGrappleDistance)
        {
            cooldownTimer = failCooldown;
            GrappleRetract();
        }
    }

    private void UpdateLine(Vector2 gancho)
    {
        line.positionCount = segments;

        Vector2 jugador = (Vector2)transform.position;
        float dist = Vector2.Distance(jugador, gancho);

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);

            float curvedT = ropeProgressionCurve != null && ropeProgressionCurve.length > 0
                ? ropeProgressionCurve.Evaluate(t)
                : t;

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
                float wave = ropeAnimationCurve.Evaluate(t) * waveSize;
                punto += perp * wave;
            }

            line.SetPosition(i, punto);
        }
    }

    private void GrappleLaunch(float chargePercent)
    {
        SetGrappleableHighlight(false);
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin = transform.position;

        float effectiveDistance = Mathf.Lerp(minGrappleDistance, maxGrappleDistance, chargePercent);
        Vector2 direction = (mouse - origin).normalized;
        grapplePoint = origin + direction * effectiveDistance;

        hookCurrentPos = origin;
        hookVelocity   = direction * launchSpeed;
        flightTimer    = 0f;
        if (currentHook != null) Destroy(currentHook);
        if (hookPrefab != null)
        {
            currentHook = Instantiate(hookPrefab, hookCurrentPos, Quaternion.identity);
        }
        else
        {
            currentHook = new GameObject("Hook");
            currentHook.transform.position = new Vector3(hookCurrentPos.x, hookCurrentPos.y, 0f);
            var sr = currentHook.AddComponent<SpriteRenderer>();
            sr.sprite = hookFallbackSprite;
        }
        currentHook.transform.localScale = Vector3.one * hookScale;
        waveSize = 0f;
        line.enabled = true;
        isGrappling = false;
        state = GrappleState.launching;
        rb.linearDamping = 0f;

        if (chargeBarImage != null) chargeBarImage.fillAmount = 0f;
        if (chargeBarRoot != null) chargeBarRoot.SetActive(false);
    }

    public void GrappleRetract()
    {
        joint.enabled = false;
        isGrappling = false;
        rb.linearDamping = 0f;

        state = GrappleState.retracting;

        if (rb.linearVelocity.magnitude > maxSwingVelocity)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSwingVelocity;
    }

    private void GrappleSwing()
    {
        // --- ESCALADO DE CUERDA ---
        float climbInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))        climbInput = -1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) climbInput =  1f;

        if (climbInput != 0f)
        {
            currentRopeLength = Mathf.Clamp(
                currentRopeLength + climbInput * climbSpeed * Time.deltaTime,
                minRopeLength,
                maxGrappleDistance
            );
            joint.distance = currentRopeLength;
        }
        else if (playerMovement != null && playerMovement.IsGrounded)
        {
            // En suelo: la cuerda se extiende pasivamente al caminar alejándose del ancla
            float distActual = Vector2.Distance(transform.position, hookCurrentPos);
            if (distActual > currentRopeLength)
            {
                currentRopeLength = Mathf.Min(distActual, maxGrappleDistance);
                joint.distance    = currentRopeLength;
            }
        }
    }

    private void SnapAndAttach(Vector2 punto)
    {
        hookCurrentPos = punto;
        joint.connectedAnchor = punto;

        // Largo inicial = distancia real al engancharse — el jugador queda en su altura actual
        currentRopeLength = Vector2.Distance(transform.position, punto);
        joint.distance    = currentRopeLength;

        joint.maxDistanceOnly = true;
        joint.enabled = true;
        isGrappling = true;
        rb.linearDamping = swingDamping;
        state = GrappleState.attached;
    }

    private Sprite GenerateCircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float r = size * 0.5f - 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) <= r ? color : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private void SetGrappleableHighlight(bool on)
    {
        if (on)
        {
            highlightedRenderers.Clear();
            originalColors.Clear();
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, maxGrappleDistance, grappleLayerMask);
            foreach (var col in cols)
            {
                SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                highlightedRenderers.Add(sr);
                originalColors.Add(sr.color);
                sr.color = highlightColor;
            }
        }
        else
        {
            for (int i = 0; i < highlightedRenderers.Count; i++)
            {
                if (highlightedRenderers[i] != null)
                    highlightedRenderers[i].color = originalColors[i];
            }
            highlightedRenderers.Clear();
            originalColors.Clear();
        }
    }

    private void OnDrawGizmos()
    {
        Vector2 origin = transform.position;

        // Rango máximo del grapple — círculo blanco
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(origin, maxGrappleDistance);

        // Radio de snap alrededor del mouse — círculo amarillo (solo en Play)
        if (Application.isPlaying && Camera.main != null)
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
            Gizmos.DrawWireSphere(mouse, snapRadius);

            // Punto de snap encontrado — cruz verde
            if (state == GrappleState.launching || state == GrappleState.attached || state == GrappleState.retracting)
            {
                Gizmos.color = Color.green;
                float s = 0.15f;
                Gizmos.DrawLine(grapplePoint + Vector2.left * s, grapplePoint + Vector2.right * s);
                Gizmos.DrawLine(grapplePoint + Vector2.down * s, grapplePoint + Vector2.up * s);
                Gizmos.DrawWireSphere(grapplePoint, 0.12f);
            }

            // Posición actual del gancho en vuelo — punto cian
            if (state == GrappleState.launching)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(hookCurrentPos, 0.1f);

                // Línea desde el jugador hasta el gancho
                Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
                Gizmos.DrawLine(origin, hookCurrentPos);
            }

            // Línea desde el jugador hasta el punto enganchado — azul
            if (state == GrappleState.attached)
            {
                Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.6f);
                Gizmos.DrawLine(origin, grapplePoint);

                // Distancia del DistanceJoint activa
                Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.2f);
                Gizmos.DrawWireSphere(grapplePoint, joint != null ? joint.distance : 0f);
            }
        }
    }
}
