using UnityEngine;

// Setup: Layer "Hazard" + Collider2D marcado como Trigger.
// Mata al jugador y destruye al Guerrero Espectral (tag "SpectralEnemy") al contacto.
// Cuando SpectralPatrollerAI esté implementado, reemplazar Destroy() por su método Morir().
[RequireComponent(typeof(Collider2D))]
public class HazardFire : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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
        }
    }
}
