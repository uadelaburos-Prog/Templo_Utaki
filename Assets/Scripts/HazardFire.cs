using UnityEngine;

// Setup: Layer "Hazard". El fuego crece desde la base del GameObject hacia arriba.
// Asignar los frames de la animacion en orden de menor a mayor llama.
// alturaMaxima debe coincidir con el alto real del sprite en su frame mas grande.
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HazardFire : MonoBehaviour
{
    public enum ModoActivacion { Ciclico, Activador }

    [Header("Modo")]
    [SerializeField] private ModoActivacion modo  = ModoActivacion.Ciclico;
    [SerializeField] private float          desfase = 0f;
    [Tooltip("Solo en modo Activador: el fuego arranca encendido y permanece así hasta que un activador llame Desactivar().")]
    [SerializeField] private bool           iniciarPrendido = false;

    [Header("Tiempos del ciclo")]
    [SerializeField] private float tiempoInactivo  = 2f;
    [SerializeField] private float tiempoCreciendo = 0.5f;
    [SerializeField] private float tiempoActivo    = 1.5f;
    [SerializeField] private float tiempoMenguando = 0.5f;

    [Header("Visual")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float    tiempoEntreFramesCulmine = 0.1f;

    [Header("Hitbox")]
    [SerializeField] private float alturaMaxima = 2f;
    [SerializeField] private float anchoBase    = 0.8f;

    private enum Fase { Inactivo, Creciendo, Activo, Menguando }
    private Fase  faseActual  = Fase.Inactivo;
    private float timerFase;
    private float timerCulmine;
    private bool  culmineToggle;
    private float baseHitbox; // Y local del borde inferior del sprite mayor

    private BoxCollider2D  col;
    private SpriteRenderer sr;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr  = GetComponent<SpriteRenderer>();
        col.isTrigger = true;
    }

    private void Start()
    {
        // Calcula donde esta el borde inferior del sprite mas grande (pivot puede no ser el bottom).
        if (frames != null && frames.Length > 0)
        {
            Sprite mayor = frames[frames.Length - 1];
            baseHitbox = -(mayor.pivot.y / mayor.pixelsPerUnit);
        }

        if (modo == ModoActivacion.Ciclico && desfase > 0f)
            AplicarDesfase();
        else if (modo == ModoActivacion.Activador && iniciarPrendido)
        {
            // Arranca encendido; un activador externo lo apagará con Desactivar()
            col.enabled = true;
            sr.enabled  = true;
            EntrarFase(Fase.Activo);
        }
        else
        {
            timerFase   = 0f;
            col.enabled = false;
            sr.enabled  = false;
            ActualizarHitbox(0f);
        }
    }

    // Ubica el fuego en la fase correcta del ciclo según el desfase configurado.
    private void AplicarDesfase()
    {
        float ciclo = tiempoInactivo + tiempoCreciendo + tiempoActivo + tiempoMenguando;
        float t     = desfase % ciclo;   // soporta desfases mayores a un ciclo completo

        if (t < tiempoInactivo)
        {
            faseActual  = Fase.Inactivo;
            timerFase   = t;
            col.enabled = false;
            sr.enabled  = false;
            ActualizarHitbox(0f);
            return;
        }
        t -= tiempoInactivo;

        if (t < tiempoCreciendo)
        {
            faseActual  = Fase.Creciendo;
            timerFase   = t;
            float pct   = Mathf.Clamp01(t / tiempoCreciendo);
            col.enabled = true;
            sr.enabled  = true;
            ActualizarHitbox(pct);
            MostrarFrame(pct);
            return;
        }
        t -= tiempoCreciendo;

        if (t < tiempoActivo)
        {
            faseActual    = Fase.Activo;
            timerFase     = t;
            timerCulmine  = 0f;
            culmineToggle = false;
            col.enabled   = true;
            sr.enabled    = true;
            ActualizarHitbox(1f);
            MostrarFrameCulmine(false);
            return;
        }
        t -= tiempoActivo;

        faseActual  = Fase.Menguando;
        timerFase   = t;
        float mpct  = 1f - Mathf.Clamp01(t / tiempoMenguando);
        col.enabled = true;
        sr.enabled  = mpct > 0f;
        ActualizarHitbox(mpct);
        MostrarFrame(mpct);
    }

    private void Update()
    {
        if (modo == ModoActivacion.Ciclico || faseActual != Fase.Inactivo)
            TickCiclo(Time.deltaTime);
    }

    // Llamar desde un activador externo (placa de presion, palanca, etc.) en modo Activador.
    public void Activar()
    {
        if (modo != ModoActivacion.Activador || faseActual != Fase.Inactivo) return;
        EntrarFase(Fase.Creciendo);
    }

    // Apaga el fuego gradualmente. Se puede conectar a alDesactivar de Lever u otro UnityEvent.
    public void Desactivar()
    {
        if (modo != ModoActivacion.Activador || faseActual == Fase.Inactivo || faseActual == Fase.Menguando) return;
        EntrarFase(Fase.Menguando);
    }

    private void TickCiclo(float dt)
    {
        timerFase += dt;

        switch (faseActual)
        {
            case Fase.Inactivo:
                if (timerFase >= tiempoInactivo)
                    EntrarFase(Fase.Creciendo);
                break;

            case Fase.Creciendo:
            {
                float t = Mathf.Clamp01(timerFase / tiempoCreciendo);
                ActualizarHitbox(t);
                MostrarFrame(t);
                if (timerFase >= tiempoCreciendo)
                    EntrarFase(Fase.Activo);
                break;
            }

            case Fase.Activo:
                timerCulmine += dt;
                if (timerCulmine >= tiempoEntreFramesCulmine)
                {
                    timerCulmine = 0f;
                    culmineToggle = !culmineToggle;
                    MostrarFrameCulmine(culmineToggle);
                }
                if (modo == ModoActivacion.Ciclico && timerFase >= tiempoActivo)
                    EntrarFase(Fase.Menguando);
                break;

            case Fase.Menguando:
            {
                float t = 1f - Mathf.Clamp01(timerFase / tiempoMenguando);
                ActualizarHitbox(t);
                MostrarFrame(t);
                if (timerFase >= tiempoMenguando)
                    EntrarFase(Fase.Inactivo);
                break;
            }
        }
    }

    private void EntrarFase(Fase nueva)
    {
        faseActual = nueva;
        timerFase  = 0f;

        switch (nueva)
        {
            case Fase.Inactivo:
                col.enabled = false;
                sr.enabled  = false;
                ActualizarHitbox(0f);
                break;

            case Fase.Creciendo:
                col.enabled = true;
                sr.enabled  = true;
                break;

            case Fase.Activo:
                timerCulmine  = 0f;
                culmineToggle = false;
                ActualizarHitbox(1f);
                MostrarFrameCulmine(false);
                break;

            case Fase.Menguando:
                break;
        }
    }

    private void MostrarFrame(float t)
    {
        if (frames == null || frames.Length == 0) return;
        int idx = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
        sr.sprite = frames[idx];
    }

    private void MostrarFrameCulmine(bool toggle)
    {
        if (frames == null || frames.Length < 2) return;
        sr.sprite = toggle ? frames[frames.Length - 2] : frames[frames.Length - 1];
    }

    // El collider crece desde el borde inferior del sprite hacia arriba.
    private void ActualizarHitbox(float t)
    {
        float h    = alturaMaxima * t;
        col.size   = new Vector2(anchoBase, Mathf.Max(h, 0.01f));
        col.offset = new Vector2(0f, baseHitbox + h / 2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (faseActual == Fase.Inactivo) return;

        if (other.CompareTag("Player"))
        {
            GameLoopManager.Instance?.PlayerDied();
            return;
        }

        if (other.CompareTag("SpectralEnemy"))
        {
            var espectral = other.GetComponent<SpectralPatrollerAI>();
            if (espectral != null) espectral.Morir();
            else                   Destroy(other.gameObject);
            return;
        }

        if (other.CompareTag("MummyEnemy"))
        {
            var momia = other.GetComponent<MummyAI>();
            if (momia != null) momia.Morir();
            else               Destroy(other.gameObject);
        }
    }

    // El fuego crece desde altura 0 (collider deshabilitado -> habilitado y redimensionado).
    // Si el jugador ya esta dentro del area cuando el collider crece —p.ej. columpiandose
    // por encima de la base— OnTriggerEnter2D NO se dispara. Reverificamos al jugador cada
    // frame mientras permanezca dentro (BUG-018). PlayerDied() es idempotente (guard isDying).
    private void OnTriggerStay2D(Collider2D other)
    {
        if (faseActual == Fase.Inactivo) return;

        if (other.CompareTag("Player"))
            GameLoopManager.Instance?.PlayerDied();
    }
}
