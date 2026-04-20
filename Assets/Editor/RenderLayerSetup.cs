using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

public static class RenderLayerSetup
{
    static readonly string[] LayersNecesarias =
        { "Background", "BackgroundDeco", "Tilemap", "Entities", "Player", "Foreground" };

    [MenuItem("Templo Utaki/Configurar Render Layers")]
    static void Configurar()
    {
        EnsurarSortingLayers();

        int cambios = 0;

        foreach (var tr in Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
        {
            string nombre = tr.gameObject.name.ToLower();
            string layer;

            if (ContieneCualquiera(nombre, "fondo", "back", "bg", "cielo", "sky"))
                layer = "Background";
            else if (ContieneCualquiera(nombre, "deco", "decorado") && !nombre.Contains("delante"))
                layer = "BackgroundDeco";
            else if (ContieneCualquiera(nombre, "delante", "front", "foreground"))
                layer = "Foreground";
            else
                layer = "Tilemap";

            if (AsignarLayer(tr, layer)) cambios++;
            ResetZ(tr.transform);
        }

        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string nombre = sr.gameObject.name.ToLower();
            string layer;

            if (ContieneCualquiera(nombre, "fondo", "background", "bg", "cielo", "sky"))
                layer = "Background";
            else if (nombre.Contains("player") || nombre.Contains("jugador"))
                layer = "Player";
            else if (ContieneCualquiera(nombre, "cristal", "crystal", "pincho", "spike", "enemy", "enemigo"))
                layer = "Entities";
            else
                continue;

            if (AsignarLayer(sr, layer)) cambios++;
            ResetZ(sr.transform);
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[RenderLayerSetup] {cambios} renderers actualizados. Guardá la escena (Ctrl+S).");
    }

    static void EnsurarSortingLayers()
    {
        var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var so = new SerializedObject(tagManagerAsset);
        var sortingLayersProp = so.FindProperty("m_SortingLayers");

        bool modificado = false;

        foreach (var nombre in LayersNecesarias)
        {
            bool existe = false;
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                if (sortingLayersProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("name").stringValue == nombre)
                {
                    existe = true;
                    break;
                }
            }

            if (!existe)
            {
                sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                var nuevo = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                nuevo.FindPropertyRelative("name").stringValue     = nombre;
                nuevo.FindPropertyRelative("uniqueID").intValue    = Mathf.Abs(nombre.GetHashCode());
                nuevo.FindPropertyRelative("locked").intValue      = 0;
                modificado = true;
                Debug.Log($"[RenderLayerSetup] Sorting Layer '{nombre}' creada.");
            }
        }

        if (modificado)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[RenderLayerSetup] Layers creadas. Si los renderers quedaron en Default, volvé a correr el menú.");
        }
        else
        {
            Debug.Log("[RenderLayerSetup] Todas las sorting layers ya existen.");
        }
    }

    static bool AsignarLayer(Renderer r, string layerName)
    {
        if (r.sortingLayerName == layerName) return false;
        Undo.RecordObject(r, "Configurar Render Layer");
        r.sortingLayerName = layerName;
        r.sortingOrder     = 0;
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

    static bool ContieneCualquiera(string s, params string[] palabras)
    {
        foreach (var p in palabras)
            if (s.Contains(p)) return true;
        return false;
    }
}
