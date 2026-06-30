using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Herramienta de Editor: detecta todos los Slider de una escena (incluidos los de
// paneles inactivos) y les adjunta el componente VolumeSlider, infiriendo el canal
// (Master / Música / SFX) por el nombre del slider o de su objeto padre.
//
// Uso:  menú  Templo Utaki ▸ Audio ▸ Asignar VolumeSlider…
//   · "…(escena abierta)"  procesa solo la escena actual.
//   · "…(todas las de Build Settings)"  abre, procesa y guarda cada escena del build.
//
// Es idempotente: si un Slider ya tiene VolumeSlider, no lo duplica (pero corrige el
// canal si quedó en Master por defecto y el nombre sugiere otro). Registra en consola
// qué canal asignó a cada slider para que puedas revisarlo.
public static class VolumeSliderSetup
{
    [MenuItem("Templo Utaki/Audio/Asignar VolumeSlider… (escena abierta)")]
    private static void AsignarEnEscenaAbierta()
    {
        var escena = SceneManager.GetActiveScene();
        int n = ProcesarEscena(escena);
        if (n > 0) EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log($"[VolumeSliderSetup] '{escena.name}': {n} slider(s) configurados. Revisá la consola y guardá la escena (Ctrl+S).");
    }

    [MenuItem("Templo Utaki/Audio/Asignar VolumeSlider… (todas las de Build Settings)")]
    private static void AsignarEnTodasLasEscenas()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int total = 0, escenasTocadas = 0;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled) continue;

            var escena = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int n = ProcesarEscena(escena);
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                escenasTocadas++;
                total += n;
            }
        }
        Debug.Log($"[VolumeSliderSetup] Listo: {total} slider(s) en {escenasTocadas} escena(s).");
    }

    // Recorre los Slider de la escena (incluidos inactivos) y les asegura un VolumeSlider.
    private static int ProcesarEscena(Scene escena)
    {
        int configurados = 0;

        foreach (var raiz in escena.GetRootGameObjects())
        {
            foreach (var slider in raiz.GetComponentsInChildren<Slider>(true))
            {
                var vs = slider.GetComponent<VolumeSlider>();
                bool esNuevo = vs == null;
                if (esNuevo)
                    vs = Undo.AddComponent<VolumeSlider>(slider.gameObject);

                var canal = InferirCanal(slider.gameObject);

                // Setear el campo privado 'canal' vía SerializedObject.
                var so = new SerializedObject(vs);
                var prop = so.FindProperty("canal");
                // Solo sobreescribe si es nuevo o si quedó en Master por defecto.
                if (esNuevo || prop.enumValueIndex == (int)VolumeSlider.Canal.Master)
                    prop.enumValueIndex = (int)canal;
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(vs);
                configurados++;
                Debug.Log($"  • '{ObtenerRuta(slider.transform)}' → canal {canal}" + (esNuevo ? " (nuevo)" : " (ya tenía)"), slider);
            }
        }
        return configurados;
    }

    // Deduce el canal por el nombre del slider y el de su padre.
    private static VolumeSlider.Canal InferirCanal(GameObject go)
    {
        string n = go.name.ToLowerInvariant();
        if (go.transform.parent != null)
            n += " " + go.transform.parent.name.ToLowerInvariant();

        if (n.Contains("mus"))                                                    // música / music
            return VolumeSlider.Canal.Musica;
        if (n.Contains("sfx") || n.Contains("fx") || n.Contains("efecto") || n.Contains("sonido"))
            return VolumeSlider.Canal.SFX;
        if (n.Contains("master") || n.Contains("maestro") || n.Contains("general") || n.Contains("todo"))
            return VolumeSlider.Canal.Master;

        Debug.LogWarning($"[VolumeSliderSetup] No pude inferir el canal de '{go.name}' — quedó en Master. Renombralo (ej. 'SliderMusica') y re-ejecutá, o ajustalo a mano.", go);
        return VolumeSlider.Canal.Master;
    }

    private static string ObtenerRuta(Transform t)
    {
        string ruta = t.name;
        while (t.parent != null) { t = t.parent; ruta = t.name + "/" + ruta; }
        return ruta;
    }
}
