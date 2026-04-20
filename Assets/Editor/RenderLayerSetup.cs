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
        int cambios = 0;

        // ── Tilemaps ──────────────────────────────────────────────
        foreach (var tr in Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
        {
            string nombre = tr.gameObject.name.ToLower();
            string layer;
            int    order;

            if (ContieneCualquiera(nombre, "fondo mazmorra", "barfondo"))
            {
                layer = "Background"; order = 0;
            }
            else if (ContieneCualquiera(nombre, "decorado") && !nombre.Contains("delante"))
            {
                layer = "BackgroundDeco"; order = 0;
            }
            else if (ContieneCualquiera(nombre, "suelo", "plataforma", "collision"))
            {
                layer = "Tilemap"; order = 0;
            }
            else if (ContieneCualquiera(nombre, "delante", "front", "foreground"))
            {
                layer = "Foreground"; order = 0;
            }
            else
            {
                layer = "Default"; order = 0;
            }

            if (AplicarLayer(tr, layer, order)) cambios++;
            ResetZ(tr.transform);
        }

        // ── SpriteRenderers ───────────────────────────────────────
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string nombre = tr_Nombre(sr);
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
                continue; // No tocar los que no matchean
            }

            if (AplicarLayer(sr, layer, order)) cambios++;
            ResetZ(sr.transform);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[RenderLayerSetup] {cambios} renderers actualizados. Guardá la escena (Ctrl+S).");
    }

    // ── Helpers ───────────────────────────────────────────────────

    static bool AplicarLayer(Renderer r, string layerName, int order)
    {
        if (r.sortingLayerName == layerName && r.sortingOrder == order) return false;

        Undo.RecordObject(r, "Configurar Render Layer");
        r.sortingLayerName = layerName;
        r.sortingOrder     = order;
        EditorUtility.SetDirty(r);
        return true;
    }

    static void ResetZ(Transform t)
    {
        if (Mathf.Abs(t.localPosition.z) < 0.001f) return;
        Undo.RecordObject(t, "Reset Z");
        var p = t.localPosition;
        t.localPosition = new Vector3(p.x, p.y, 0f);
        EditorUtility.SetDirty(t);
    }

    static string tr_Nombre(Component c) => c.gameObject.name.ToLower();

    static bool ContieneCualquiera(string s, params string[] palabras)
    {
        foreach (var p in palabras)
            if (s.Contains(p)) return true;
        return false;
    }
}
