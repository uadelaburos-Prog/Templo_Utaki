using UnityEngine;

// Zona de entrada a la arena del Golem. Cuando el jugador cae/entra:
//   1. Guarda un checkpoint en la arena (al morir reaparece acá, no repite el recorrido).
//   2. Arranca la pelea del Golem (con cinemática de aparición la primera vez).
//   3. Si es un respawn de checkpoint, arranca el combate directo (sin cinemática).
//
// Setup: BoxCollider2D (Trigger). Asignar el GolemBoss y, opcionalmente, un punto de spawn.
[RequireComponent(typeof(Collider2D))]
public class BossFightTrigger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GolemBoss golem;
    [Tooltip("Punto de reaparición dentro de la arena (checkpoint). Vacío = posición del trigger.")]
    [SerializeField] private Transform puntoSpawn;

    private bool _disparado;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_disparado) return;
        if (!other.CompareTag("Player")) return;

        _disparado = true;

        Vector3 spawn = puntoSpawn != null ? puntoSpawn.position : transform.position;

        // ¿Ya veníamos de un checkpoint en esta arena? Entonces es un respawn: sin cinemática.
        bool esRespawn = GameLoopManager.Instance != null
                      && GameLoopManager.Instance.EsEsteCheckpoint(spawn);

        if (!esRespawn)
            GameLoopManager.Instance?.GuardarCheckpoint(spawn);

        golem?.IniciarPelea(saltarIntro: esRespawn);
    }
}
