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
        timerFase   = desfase;
        col.enabled = false;
        sr.enabled  = false;

        // Calcula donde esta el borde inferior del sprite mas grande (pivot puede no ser el bottom).
        if (frames != null && frames.Length > 0)
        {
            Sprite mayor = frames[frames.Length - 1];
            float pivotYUnits = mayor.pivot.y / mayor.pixelsPerUnit;
            baseHitbox = -pivotYUnits;
        }

        ActualizarHitbox(0f);
    }

    private void Update()
    {
        if (modo == ModoActivacion.Ciclico)
            TickCiclo(Time.deltaTime);
    }

    // Llamar desde un activador externo (placa de presion, palanca, etc.) en modo Activador.
    public void Activar()
    {
        if (modo != ModoActivacion.Activador || faseActual != Fase.Inactivo) return;
        EntrarFase(Fase.Creciendo);
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
                if (timerFase >= tiempoActivo)
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
}
