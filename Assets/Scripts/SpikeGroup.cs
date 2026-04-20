using UnityEngine;

public class SpikeGroup : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab con SpikeHazard adjunto.")]
    [SerializeField] private SpikeHazard spikePrefab;

    [Header("Posiciones (relativas al grupo)")]
    [Tooltip("Offsets XY desde el centro del grupo donde se instanciará cada pincho.")]
    [SerializeField] private Vector2[] posiciones;

    [Header("Configuración compartida")]
    [SerializeField] private SpikeHazard.SpikeMode modo = SpikeHazard.SpikeMode.Estatico;
    [SerializeField] private float tiempoExtendido = 2f;
    [SerializeField] private float tiempoRetraido  = 1.5f;
    [SerializeField] private float velocidadMover  = 5f;
    [Tooltip("Desplazamiento Y al retraerse (negativo = baja hacia el suelo).")]
    [SerializeField] private float desplazamiento  = -0.6f;
    [Tooltip("Si está activo, distribuye la fase inicial de forma uniforme entre todos los pinchos.")]
    [SerializeField] private bool  desfasarAutomatico = true;
    [Tooltip("Segundos que cada pincho espera retraído antes de su primera salida. Anula el desfase automático.")]
    [SerializeField] private float delayInicial = 0f;

    private void Start()
    {
        if (spikePrefab == null || posiciones == null) return;

        for (int i = 0; i < posiciones.Length; i++)
        {
            Vector3 worldPos = transform.position + (Vector3)posiciones[i];
            SpikeHazard spike = Instantiate(spikePrefab, worldPos, transform.rotation, transform);

            float fase = desfasarAutomatico && posiciones.Length > 1
                ? (float)i / posiciones.Length
                : 0f;

            spike.Init(modo, tiempoExtendido, tiempoRetraido, velocidadMover, desplazamiento, fase, delayInicial);
        }
    }

    private void OnDrawGizmos()
    {
        if (posiciones == null) return;

        foreach (var offset in posiciones)
        {
            Vector3 worldPos = transform.position + (Vector3)offset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(worldPos, 0.2f);

            if (modo == SpikeHazard.SpikeMode.Retractil)
            {
                Vector3 dest = worldPos + Vector3.up * desplazamiento;
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
                Gizmos.DrawWireSphere(dest, 0.2f);
                Gizmos.DrawLine(worldPos, dest);
            }
        }
    }
}
