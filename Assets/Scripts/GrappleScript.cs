using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpringJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class GrappleScript : MonoBehaviour
{
    [SerializeField] private float maxGrappleDistance = 10f;
    [SerializeField] private float maxSwingVelocity = 15f;
    [SerializeField] private LayerMask grappleLayerMask;

    [SerializeField] private float launchSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;

    [HideInInspector] public SpringJoint2D joint;
    private Rigidbody2D rb;
    [HideInInspector] public LineRenderer line;

    [HideInInspector] public bool isGrappling = false;
    private Vector2 grapplePoint;

    private enum GrappleState { idle, launching, attached, retracting }
    private GrappleState state = GrappleState.idle;

    private Vector2 hookCurrentPos;
    private Vector2 hookVelocity;

    [Header("Snap")]
    [SerializeField] private float snapRadius = 4f;

    [Header("Rebote")]
    [SerializeField] private LayerMask bounceLayerMask;

    [Header("Swing")]
    [SerializeField] private float swingDamping = 0.8f;

    [Header("Configuración General")]
    [SerializeField] private int segments = 20;
    [Range(0, 20)][SerializeField] private float straightenLineSpeed = 5;

    [Header("Animación de la Cuerda:")]
    public AnimationCurve ropeAnimationCurve;
    [Range(0.01f, 4)][SerializeField] private float StartWaveSize = 2;
    float waveSize = 0;

    [Header("Progresión de la Cuerda:")]
    public AnimationCurve ropeProgressionCurve;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<SpringJoint2D>();
        line = GetComponent<LineRenderer>();

        joint.enabled = false;
        joint.autoConfigureDistance = false;
        joint.enableCollision = false;
        line.useWorldSpace = true;
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
    }

    private void LinePointsToFirePoint()
    {
        for (int i = 0; i < segments; i++)
            line.SetPosition(i, transform.position);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
            GrappleLaunch();
        else if (Input.GetKeyUp(KeyCode.Mouse0))
            GrappleRetract();

        switch (state)
        {
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
    }

    private void UpdateRetracting()
    {
        hookCurrentPos = Vector2.MoveTowards(hookCurrentPos, transform.position, retractSpeed * Time.deltaTime);
        UpdateLine(hookCurrentPos);

        if (Vector2.Distance(hookCurrentPos, transform.position) < 0.05f)
        {
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
        hookCurrentPos = Vector2.MoveTowards(hookCurrentPos, grapplePoint, launchSpeed * Time.deltaTime);
        UpdateLine(hookCurrentPos);

        // Chequear enganche — collider grappleable cerca del gancho
        Collider2D grappleable = Physics2D.OverlapCircle(hookCurrentPos, snapRadius * 0.5f, grappleLayerMask);
        if (grappleable != null)
        {
            Vector2 snapPoint = grappleable.ClosestPoint(hookCurrentPos);
            SnapAndAttach(snapPoint);
            return;
        }

        // Chequear rebote — collider NO grappleable en el camino recorrido este frame
        Vector2 travelDir = (hookCurrentPos - prevPos).normalized;
        float travelDist = Vector2.Distance(prevPos, hookCurrentPos);
        RaycastHit2D bounceHit = Physics2D.Raycast(prevPos, travelDir, travelDist + 0.1f, bounceLayerMask);
        if (bounceHit)
        {
            // Calcular dirección reflejada respecto a la normal de la superficie
            Vector2 reflected = Vector2.Reflect(travelDir, bounceHit.normal);

            // Reposicionar el gancho justo en el punto de impacto
            hookCurrentPos = bounceHit.point;

            // El nuevo destino es el punto de impacto + la dirección reflejada * distancia restante
            float remaining = Vector2.Distance(bounceHit.point, grapplePoint);
            grapplePoint = bounceHit.point + reflected * Mathf.Max(remaining, maxGrappleDistance * 0.5f);

        }

        // Llegó al destino sin encontrar nada — retraer
        if (Vector2.Distance(hookCurrentPos, grapplePoint) < 0.1f)
            GrappleRetract();
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

    private void GrappleLaunch()
    {
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin = transform.position;

        // Verificar que el destino esté dentro del rango
        if (Vector2.Distance(origin, mouse) > maxGrappleDistance) return;

        // La cuerda siempre viaja hacia donde clickeaste
        // El enganche se resuelve en UpdateLaunching mientras viaja
        grapplePoint = mouse;
        hookCurrentPos = origin;
        waveSize = StartWaveSize;
        line.enabled = true;
        isGrappling = false;
        state = GrappleState.launching;
        rb.linearDamping = 0f;
    }

    public void GrappleRetract()
    {
        joint.enabled = false;
        isGrappling = false;

        state = GrappleState.retracting;

        if (rb.linearVelocity.magnitude > maxSwingVelocity)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSwingVelocity;
    }

    private void GrappleSwing()
    {
        // Intentionally empty — pendulum is handled by SpringJoint2D physics
    }

    private void SnapAndAttach(Vector2 punto)
    {
        hookCurrentPos = punto;
        joint.connectedAnchor = punto;

        // Limitar la longitud de la cuerda a maxGrappleDistance
        float ropeLength = Vector2.Distance(transform.position, punto);
        joint.distance = Mathf.Min(ropeLength, maxGrappleDistance);

        joint.enabled = true;
        isGrappling = true;
        rb.linearDamping = swingDamping;
        state = GrappleState.attached;
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

                // Distancia del SpringJoint activa
                Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.2f);
                Gizmos.DrawWireSphere(grapplePoint, joint != null ? joint.distance : 0f);
            }
        }
    }
}
