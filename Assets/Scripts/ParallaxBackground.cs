using System.Collections.Generic;
using UnityEngine;

// Adjuntar a un GameObject vacío "ParallaxManager".
// factorX  0 = capa fija en mundo (scrollea a velocidad de cámara, capa más cercana)
//          1 = capa sigue exacto a la cámara (aparece estática, capa más lejana / cielo)
// Los sprites deben ser lo suficientemente anchos para cubrir el nivel completo.
//
// DefaultExecutionOrder(1000): debe correr DESPUÉS de CamaraScript (orden 0), que mueve la
// cámara con Lerp en LateUpdate. Así el fondo lee la posición YA actualizada de la cámara
// (no la del frame anterior) y evita el temblor; además Start() captura _camStart después
// del SnapToPlayer inicial de la cámara.
[DefaultExecutionOrder(1000)]
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    private struct ParallaxLayer
    {
        [Tooltip("SpriteRenderer de la capa.")]
        public SpriteRenderer spriteRenderer;

        [Tooltip("0 = fijo en mundo (scrollea rápido). 1 = sigue cámara (aparece estático).")]
        [Range(0f, 1f)] public float factorX;

        [Tooltip("Factor vertical. Usar 0 para todos los fondos horizontales.")]
        [Range(0f, 1f)] public float factorY;
    }

    [SerializeField]
    [Tooltip("Dejar vacío para usar Camera.main automáticamente.")]
    private Camera cam;

    [SerializeField] private List<ParallaxLayer> layers = new();

    private Vector3[] _initialPositions;
    private Vector2 _camStart;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            GameDebug.Log("[Parallax] ERROR: No se encontró Camera.main.");
            enabled = false;
            return;
        }

        _camStart = cam.transform.position;
        _initialPositions = new Vector3[layers.Count];

        for (int i = 0; i < layers.Count; i++)
        {
            var sr = layers[i].spriteRenderer;
            if (sr == null)
            {
                GameDebug.Log($"[Parallax] WARN: Capa {i} no tiene SpriteRenderer asignado.");
                continue;
            }
            _initialPositions[i] = sr.transform.position;
            GameDebug.Log($"[Parallax] Capa {i} '{sr.name}': factorX={layers[i].factorX:F2}");
        }
    }

    private void LateUpdate()
    {
        float deltaX = cam.transform.position.x - _camStart.x;
        float deltaY = cam.transform.position.y - _camStart.y;

        for (int i = 0; i < layers.Count; i++)
        {
            var sr = layers[i].spriteRenderer;
            if (sr == null) continue;

            sr.transform.position = new Vector3(
                _initialPositions[i].x + deltaX * layers[i].factorX,
                _initialPositions[i].y + deltaY * layers[i].factorY,
                _initialPositions[i].z
            );
        }
    }
}
