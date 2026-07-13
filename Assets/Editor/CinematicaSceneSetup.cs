using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Genera las escenas de cinemática en formato cómic (Intro y Final) con su Canvas,
/// fondo negro, un contenedor "Viñetas" (vacío, donde el diseñador coloca a mano las
/// Images de cada viñeta), fundido a negro y el componente ComicPlayer ya configurado,
/// y las agrega a Build Settings.
///
/// Modelo: PÁGINA ACUMULATIVA. El diseñador crea las Images de las viñetas como hijos
/// del contenedor "Viñetas", las coloca/dimensiona a gusto y las ordena en el árbol
/// (ese orden = orden de aparición). ComicPlayer las revela una a una con fade.
///
/// "Crear" omite las escenas que ya existan (no pisa trabajo). "Regenerar" las
/// sobrescribe (útil si se generaron con el modelo viejo de slideshow y aún no tienen
/// viñetas colocadas).
/// </summary>
public static class CinematicaSceneSetup
{
    private const string CarpetaEscenas = "Assets/Scenes";

    [MenuItem("Templo Utaki/Crear Escenas de Cinemática (Intro + Final)")]
    static void CrearEscenas() => Generar(sobrescribir: false);

    [MenuItem("Templo Utaki/Regenerar Escenas de Cinemática (SOBRESCRIBE Intro + Final)")]
    static void RegenerarEscenas()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Regenerar cinemáticas",
            "Esto SOBRESCRIBE Intro.unity y Final.unity con la estructura base " +
            "(perdés cualquier viñeta que ya hayas colocado en ellas).\n\n¿Continuar?",
            "Sobrescribir", "Cancelar");
        if (ok) Generar(sobrescribir: true);
    }

    static void Generar(bool sobrescribir)
    {
        // Intro: al terminar va al selector de niveles. Final: vuelve al menú.
        CrearEscenaComic("Intro", "Level Selector", sobrescribir);
        CrearEscenaComic("Final", "Menu", sobrescribir);

        AgregarABuildSettings($"{CarpetaEscenas}/Intro.unity");
        AgregarABuildSettings($"{CarpetaEscenas}/Final.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CinematicaSceneSetup] Escenas Intro y Final listas. Crear las Images de las viñetas " +
                  "como hijos del contenedor 'Viñetas' y ordenarlas (orden del árbol = orden de aparición).");
    }

    static void CrearEscenaComic(string nombreEscena, string escenaSiguiente, bool sobrescribir)
    {
        string path = $"{CarpetaEscenas}/{nombreEscena}.unity";
        if (File.Exists(path) && !sobrescribir)
        {
            Debug.Log($"[CinematicaSceneSetup] '{path}' ya existe; se omite para no pisar trabajo. " +
                      "Usá 'Regenerar' si querés sobrescribirla.");
            return;
        }

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── EventSystem (Input legacy) ────────────────────────────────
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ── Canvas raíz ───────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas Cinemática",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        // Fondo negro: las viñetas van apareciendo sobre él.
        NuevaImagenStretch("Fondo Negro", canvasGO.transform, Color.black);

        // Contenedor de viñetas: vacío, a pantalla completa. El diseñador crea aquí,
        // como hijos, las Images de cada viñeta (orden del árbol = orden de aparición).
        var contenedorGO = new GameObject("Viñetas", typeof(RectTransform));
        var contRT = contenedorGO.GetComponent<RectTransform>();
        contRT.SetParent(canvasGO.transform, false);
        Estirar(contRT);

        // Fundido a negro para la transición de salida (al frente, invisible).
        var fadeGO    = NuevaImagenStretch("Fade Negro", canvasGO.transform, Color.black);
        var fadeGroup = fadeGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha          = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGO.transform.SetAsLastSibling();

        // ── ComicPlayer ───────────────────────────────────────────────
        var comicGO = new GameObject("ComicPlayer");
        var comic   = comicGO.AddComponent<ComicPlayer>();

        var so = new SerializedObject(comic);
        so.FindProperty("contenedorVinetas").objectReferenceValue = contenedorGO.transform;
        so.FindProperty("fadeNegro").objectReferenceValue          = fadeGroup;
        so.FindProperty("escenaSiguiente").stringValue             = escenaSiguiente;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, path);
        Debug.Log($"[CinematicaSceneSetup] Creada '{path}' (siguiente escena: '{escenaSiguiente}').");
    }

    // Crea un GameObject con Image estirada a pantalla completa dentro del padre dado.
    static GameObject NuevaImagenStretch(string nombre, Transform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        Estirar(rt);
        go.GetComponent<Image>().color = color;
        return go;
    }

    // Ancla el RectTransform a los cuatro bordes del padre (stretch full).
    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void AgregarABuildSettings(string path)
    {
        var escenas = EditorBuildSettings.scenes.ToList();
        if (escenas.Any(s => s.path == path)) return;
        escenas.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = escenas.ToArray();
        Debug.Log($"[CinematicaSceneSetup] '{path}' agregada a Build Settings.");
    }
}
