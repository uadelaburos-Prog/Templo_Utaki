using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpringJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class GrappleScript : MonoBehaviour
{
    private float maxGrappleDistance = 10f;
    [SerializeField] private float maxSwingVelocity = 15f;
    [SerializeField] private LayerMask grappleLayerMask;
    private Vector2 grapplePos;
    [SerializeField] private float launchSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;

    public GameObject hookPrefab;
    private GameObject currentHook; 

    [HideInInspector] public SpringJoint2D joint;
    private Rigidbody2D rb;
    [HideInInspector] public LineRenderer line;
    [SerializeField] private Camera cam;

    [HideInInspector] public bool isGrappling = false;
    private Vector2 grapplePoint;
    [SerializeField] private float playerSpeed = 5;
    [SerializeField] private float acceleration = 50;

    private enum GrappleState { idle, launching, attached, retracting }
    private GrappleState state = GrappleState.idle;
    private Vector2 hookCurrentPos;

    [Header("Configuración General")]
    [SerializeField] private int segments = 20;
    [Range(0, 20)][SerializeField] private float straightenLineSpeed = 5;

    [Header("Animación de la Cuerda:")]
    public AnimationCurve ropeAnimationCurve;
    [Range(0.01f, 4)][SerializeField] private float StartWaveSize = 2;
    float waveSize = 0;

    [Header("Progresión de la Cuerda:")]
    public AnimationCurve ropeProgressionCurve;
    [SerializeField][Range(1, 50)] private float ropeProgressionSpeed = 1;

    private float ropeTimer = 0f;

    bool straightLine = true;

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
        straightLine = false;
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
        {
            line.SetPosition(i, transform.position);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            GrappleLaunch();
            Debug.Log("Grapple launched!");
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            GrappleRetract();
            Debug.Log("Grapple retracted!");
        }

        switch (state)
        {
            case GrappleState.launching:
                UpdateLaunching(); break;
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

        if(Vector2.Distance(hookCurrentPos, transform.position) < 0.05f)
        {
            line.enabled = false;
            line.positionCount = 2;
            line.SetPosition(0,Vector2.zero);
            line.SetPosition(1,Vector2.zero);
            state = GrappleState.idle;
        }
    }

    private void UpdateLaunching()
    {
        ropeTimer += Time.deltaTime * ropeProgressionSpeed;
        ropeTimer = Mathf.Clamp01(ropeTimer);

        hookCurrentPos = Vector2.MoveTowards(hookCurrentPos, grapplePoint, launchSpeed * Time.deltaTime);
        UpdateLine(hookCurrentPos);

        if(Vector2.Distance(hookCurrentPos, grapplePoint) < 0.05f)
        {
            hookCurrentPos = grapplePoint;

            joint.connectedAnchor = grapplePoint;
            joint.distance = Vector2.Distance(transform.position, grapplePoint);
            joint.enabled = true;

            isGrappling = true;
            rb.linearDamping = 0f;
            state = GrappleState.attached;
        }
    }

    private void UpdateLine(Vector2 endRope)
    {
        line.positionCount = segments;
        Vector2 origin = (Vector2)transform.position;
        
        Vector2 ropeDir = (endRope - origin).normalized;
        Vector2 perpendicular = Vector2.Perpendicular(ropeDir);

        float progression = ropeProgressionCurve.Evaluate(ropeTimer);


        for (int i = 0; i < segments; i++)
        {
            float delta = (float)i / (segments - 1f);

            Vector2 offSet = perpendicular * ropeAnimationCurve.Evaluate(delta) * waveSize;

            Vector2 targetPosition = Vector2.Lerp(origin, endRope, delta) + offSet;

            Vector2 pointOnLine = Vector2.Lerp(origin, targetPosition, progression);
            line.SetPosition(i, pointOnLine);
        }

        line.SetPosition(0, origin);
        line.SetPosition(segments - 1, endRope);
    }

    private void GrappleLaunch() //el gancho en si, lanza un rayo desde el jugador hacia el mouse y si golpea algo dentro de la distancia máxima, se engancha a ese punto y permite el movimiento pendular
    {
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin = transform.position;
        Vector2 dir = (mouse - origin).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxGrappleDistance, grappleLayerMask);
        if (!hit) return;

        grapplePoint = hit.point;
        hookCurrentPos = origin;

        ropeTimer = 0f;
        waveSize = StartWaveSize;   
        straightLine = false;

        joint.connectedAnchor = grapplePoint;
        joint.distance = Vector2.Distance(origin, grapplePoint);

        line.enabled = true;
        state = GrappleState.launching;

        isGrappling = true;
        rb.linearDamping = 0f;
    }

    public void GrappleRetract() // retrae el gancho y detiene el movimiento pendular
    {
        joint.enabled = false;
        isGrappling = false;

        hookCurrentPos = grapplePoint; // Asegura que el gancho comience a retraerse desde su posición actual
        state = GrappleState.retracting;

        if (rb.linearVelocity.magnitude > maxSwingVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSwingVelocity;
        }
    }

    private void GrappleSwing()
    {
        float input = Input.GetAxis("Horizontal");

        Vector2 toAnchor = (Vector2)transform.position - grapplePoint;

        // tangente del círculo (movimiento pendular real)
        Vector2 tangent = new Vector2(-toAnchor.y, toAnchor.x).normalized;

        if (!Mathf.Approximately(input, 0)) // calcula la aceleración cuando hay input
        {
            float targetVelocity = input * playerSpeed;
            float velocityDiff = targetVelocity - Vector2.Dot(rb.linearVelocity, tangent);
            float force = velocityDiff * acceleration;

            rb.AddForce(tangent * force);
        }
        else // calcula la desaceleración cuando no hay input
        {
            float velocityDiff = -Vector2.Dot(rb.linearVelocity, tangent);
            float force = velocityDiff * acceleration * 0.125f * Time.deltaTime; // Reducción de fuerza para desacelerar más suavemente
            rb.AddForce(tangent * force);
        }
    }
}