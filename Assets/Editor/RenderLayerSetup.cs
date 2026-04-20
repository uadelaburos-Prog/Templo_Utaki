using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

public static class RenderLayerSetup
{
    // Orden de layers (atrás → frente):
    // Background → BackgroundDeco → Default → Tilemap → Entities → Player → Foreground

    [MenuItem("Templo Utaki/Configurar Render Layers")]
    static void Configurar()
    {
        // Verificar que las sorting layers existen
        LogLayersDisponibles();

        int cambios = 0;

        // ── Tilemaps ──────────────────────────────────────────────
        var tilemapRenderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        Debug.Log($"[RenderLayerSetup] Tilemaps encontrados: {tilemapRenderers.Length}");

        foreach (var tr in tilemapRenderers)
        {
            string nombre = tr.gameObject.name.ToLower();
            string layer;
            int    order;

            Debug.Log($"[RenderLayerSetup] Procesando Tilemap: '{tr.gameObject.name}'");

            if (ContieneCualquiera(nombre, "fondo", "back", "bg", "cielo", "sky", "barfondo"))
            {
                layer = "Background"; order = 0;
            }
            else if (ContieneCualquiera(nombre, "deco", "decorado") && !nombre.Contains("delante"))
            {
                layer = "BackgroundDeco"; order = 0;
            }
            else if (ContieneCualquiera(nombre, "delante", "front", "foreground"))
            {
                layer = "Foreground"; order = 0;
            }
            else
            {
                // Todo tilemap que no matchee → Tilemap (suelos, plataformas, collision, etc.)
                layer = "Tilemap"; order = 0;
            }

            if (AplicarLayer(tr, layer, order))
            {
                cambios++;
                Debug.Log($"[RenderLayerSetup] '{tr.gameObject.name}' → {layer}");
            }
            ResetZ(tr.transform);
        }

        // ── SpriteRenderers ───────────────────────────────────────
        var spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        Debug.Log($"[RenderLayerSetup] SpriteRenderers encontrados: {spriteRenderers.Length}");

        foreach (var sr in spriteRenderers)
        {
            string nombre = sr.gameObject.name.ToLower();
            string layer;
            int    order;

            if (ContieneCualquiera(nombre, "fondo", "background", "bg", "barfondo", "cielo", "sky"))
            {
                layer = "Background"; order = 0;
            }
            else if (nombre.Contains("player") || nombre.Contains("jugador"))
            {
                layer = "Player"; order = 0;
            }
            else if (ContieneCualquiera(nombre, "cristal", "crystal", "pincho", "spike", "enemy", "enemigo"))
            {
                layer = "Entities"; order = 0;
            }
            else
            {
                continue;
            }

            if (AplicarLayer(sr, layer, order))
            {
                cambios++;
                Debug.Log($"[RenderLayerSetup] '{sr.gameObject.name}' → {layer}");
            }
            ResetZ(sr.transform);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[RenderLayerSetup] TOTAL: {cambios} renderers actualizados. Guardá la escena (Ctrl+S).");
    }

    static void LogLayersDisponibles()
    {
        var layers = SortingLayer.layers;
        string lista = "";
        foreach (var l in layers) lista += $"'{l.name}' ";
        Debug.Log($"[RenderLayerSetup] Sorting Layers registradas: {lista}");
    }

    // ── Helpers ───────────────────────────────────────────────────

    static bool AplicarLayer(Renderer r, string layerName, int order)
    {
        // Verificar que la layer existe
        if (!SortingLayerExiste(layerName))
        {
            Debug.LogWarning($"[RenderLayerSetup] Layer '{layerName}' NO existe en el proyecto. Abrí Edit → Project Settings → Tags and Layers para verificar.");
            return false;
        }

        if (r.sortingLayerName == layerName && r.sortingOrder == order) return false;

        Undo.RecordObject(r, "Configurar Render Layer");
        r.sortingLayerName = layerName;
        r.sortingOrder     = order;
        EditorUtility.SetDirty(r);
        return true;
    }

    static bool SortingLayerExiste(string nombre)
    {
        foreach (var l in SortingLayer.layers)
            if (l.name == nombre) return true;
        return false;
    }

    static void ResetZ(Transform t)
    {
        if (Mathf.Abs(t.localPosition.z) < 0.001f) return;
        Undo.RecordObject(t, "Reset Z");
        var p = t.localPosition;
        t.localPosition = new Vector3(p.x, p.y, 0f);
        EditorUtility.SetDirty(t);
    }

    static bool ContieneCualquiera(string s, params string[] palabras)
    {
        foreach (var p in palabras)
            if (s.Contains(p)) return true;
        return false;
    }
}
