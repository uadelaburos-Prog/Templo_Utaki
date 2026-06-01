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
            Vector3     worldPos = transform.position + (Vector3)posiciones[i];
            SpikeHazard spike    = Instantiate(spikePrefab, worldPos, transform.rotation, transform);

            float fase = desfasarAutomatico && posiciones.Length > 1
                ? (float)i / posiciones.Length
                : 0f;

            spike.Init(modo, tiempoRetraido, tiempoAsomando, tiempoExtendido,
                       velocidadMover, desplazamiento, fraccionAsomando, fase, delayInicial);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (spikePrefab == null || posiciones == null || posiciones.Length == 0) return;

        SpriteRenderer sr = spikePrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Camera cam = Camera.current;
        if (cam == null) return;

        Sprite    sp  = sr.sprite;
        Texture2D tex = sp.texture;
        if (tex == null) return;

        float worldW = sp.rect.width  / sp.pixelsPerUnit;
        float worldH = sp.rect.height / sp.pixelsPerUnit;

        // Offset de pivot: cuánto desplazar el centro de dibujado respecto al origen del sprite
        Vector2 pivot       = sp.pivot / sp.pixelsPerUnit;
        Vector2 centerShift = new Vector2(worldW * 0.5f - pivot.x, worldH * 0.5f - pivot.y);

        // Rect UV dentro de la textura atlas
        Rect uvRect = new Rect(
            sp.textureRect.x / tex.width,
            sp.textureRect.y / tex.height,
            sp.textureRect.width  / tex.width,
            sp.textureRect.height / tex.height);

        UnityEditor.Handles.BeginGUI();
        foreach (var offset in posiciones)
        {
            Vector3 worldPos   = transform.position + (Vector3)offset + (Vector3)centerShift;
            Vector3 screenCtr  = cam.WorldToScreenPoint(worldPos);
            if (screenCtr.z < 0f) continue;

            // Tamaño en pixels de pantalla usando dos puntos de referencia
            float scrW = Mathf.Abs(cam.WorldToScreenPoint(worldPos + Vector3.right * worldW).x
                                 - cam.WorldToScreenPoint(worldPos - Vector3.right * worldW).x) * 0.5f;
            float scrH = Mathf.Abs(cam.WorldToScreenPoint(worldPos + Vector3.up * worldH).y
                                 - cam.WorldToScreenPoint(worldPos - Vector3.up * worldH).y) * 0.5f;

            // GUI usa Y invertida respecto a screen
            float guiX = screenCtr.x - scrW * 0.5f;
            float guiY = cam.pixelHeight - screenCtr.y - scrH * 0.5f;

            GUI.DrawTextureWithTexCoords(new Rect(guiX, guiY, scrW, scrH), tex, uvRect, alphaBlend: true);
        }
        UnityEditor.Handles.EndGUI();
    }
#endif
}
