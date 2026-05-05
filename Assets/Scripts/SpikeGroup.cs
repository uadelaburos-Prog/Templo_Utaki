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
    [Tooltip("Segundos completamente oculto.")]
    [SerializeField] private float tiempoRetraido  = 1.5f;
    [Tooltip("Segundos con la punta visible (aviso, sin daño).")]
    [SerializeField] private float tiempoAsomando  = 0.4f;
    [Tooltip("Segundos completamente desplegado (con daño).")]
    [SerializeField] private float tiempoExtendido = 1.5f;
    [SerializeField] private float velocidadMover  = 5f;
    [Tooltip("Desplazamiento Y al retraerse (negativo = baja hacia el suelo).")]
    [SerializeField] private float desplazamiento  = -0.6f;
    [Tooltip("Fracción del desplazamiento visible en estado Asomando.")]
    [SerializeField, Range(0.05f, 0.5f)] private float fraccionAsomando = 0.25f;
    [Tooltip("Si está activo, distribuye la fase inicial de forma uniforme entre todos los pinchos.")]
    [SerializeField] private bool  desfasarAutomatico = true;
    [Tooltip("Segundos de espera inicial antes del primer ciclo. Anula el desfase automático.")]
    [SerializeField] private float delayInicial = 0f;

    private void Start()
    {
        if (spikePrefab == null || posiciones == null) return;

        for (int i = 0; i < posiciones.Length; i++)
        {
            Vector3    worldPos = transform.position + (Vector3)posiciones[i];
            SpikeHazard spike   = Instantiate(spikePrefab, worldPos, transform.rotation, transform);

            float fase = desfasarAutomatico && posiciones.Length > 1
                ? (float)i / posiciones.Length
                : 0f;

            spike.Init(modo, tiempoRetraido, tiempoAsomando, tiempoExtendido,
                       velocidadMover, desplazamiento, fraccionAsomando, fase, delayInicial);
        }
    }

    private void OnDrawGizmos()
    {
        if (posiciones == null) return;

        foreach (var offset in posiciones)
        {
            Vector3 worldPos = transform.position + (Vector3)offset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(worldPos, 0.15f);

            if (modo == SpikeHazard.SpikeMode.Retractil)
            {
                Vector3 retPos = worldPos + Vector3.up * desplazamiento;
                Vector3 asoPos = Vector3.Lerp(retPos, worldPos, fraccionAsomando);

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
                Gizmos.DrawLine(worldPos, retPos);
                Gizmos.DrawWireSphere(retPos, 0.1f);

                Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
                Gizmos.DrawWireSphere(asoPos, 0.1f);
            }
        }
    }
}
