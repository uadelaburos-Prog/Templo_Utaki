using UnityEngine;

// Cheats de desarrollo para testear niveles y el jefe rápido.
// Colocar en el GameObject del Player. Quitar (o desactivar) antes del build final.
[RequireComponent(typeof(Rigidbody2D))]
public class DebugCheats : MonoBehaviour
{
    [Header("Activación")]
    [Tooltip("Si está desactivado, ningún cheat responde. Apagar para el build.")]
    [SerializeField] private bool cheatsHabilitados = true;

    [Header("Teletransporte al mouse")]
    [Tooltip("Tecla que teletransporta al jugador a la posición del cursor.")]
    [SerializeField] private KeyCode teleportKey = KeyCode.F1;

    private Rigidbody2D    rb;
    private GrappleScript  grapple;
    private Camera         cam;

    private void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        grapple = GetComponent<GrappleScript>();
        cam     = Camera.main;
    }

    private void Update()
    {
        if (!cheatsHabilitados) return;

        if (Input.GetKeyDown(teleportKey))
            TeletransportarAlMouse();
    }

    // Mueve al jugador a la posición del cursor en el mundo, anulando velocidad y gancho.
    private void TeletransportarAlMouse()
    {
        if (cam == null) cam = Camera.main;   // por si la cámara se recreó al recargar escena
        if (cam == null) return;

        // Soltar el gancho si está enganchado para no arrastrar el joint al nuevo punto
        if (grapple != null && grapple.isGrappling)
            grapple.GrappleRetract();

        Vector3 mundo = cam.ScreenToWorldPoint(Input.mousePosition);
        rb.position        = new Vector2(mundo.x, mundo.y);
        rb.linearVelocity  = Vector2.zero;
    }
}
