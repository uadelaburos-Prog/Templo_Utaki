using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Genera el AnimatorController del Golem a partir de los spritesheets de
/// Assets/Sprites/Enemies/Golem, con los parámetros que dispara GolemBoss.cs.
///
/// El Animator maneja: Idle, Emerger (placeholder), Salto, las 4 poses vulnerables, el tick
/// de dolor y la muerte. El MARTILLO y el PROYECTIL NO usan el Animator: los anima GolemBoss
/// por código (frame por frame, ritmo pausado). Por eso este tool también asigna al prefab
/// los arrays framesMartillo y framesProyectil del GolemBoss.
///
/// Mapeo sprites → clips:
///   · Idle → Golem_idle (1f, loop) · Saltar → Salto[0..4] (2=auge, 4=aterrizaje)
///   · VulnManoIzq/Der/Cabeza/Pecho → Partes_Debiles frames 0 / 1 / 3 / 2 (estáticos)
///   · Tick → Golem_Dano (2f) · Morir → Golem_Muerte (6f, terminal)
///
/// Si el prefab Assets/Prefabs/GolemBoss.prefab existe, agrega el Animator, cablea 'animator'
/// y asigna framesMartillo / framesProyectil.
/// </summary>
public static class GolemAnimatorSetup
{
    private const string CarpetaSprites = "Assets/Sprites/Enemies/Golem";
    private const string CarpetaAnim    = "Assets/Animations/Golem";
    private const string RutaController = CarpetaAnim + "/GolemBoss.controller";
    private const string RutaPrefab     = "Assets/Prefabs/GolemBoss.prefab";

    private static readonly string[] Triggers =
    {
        "Emerger", "SaltoWindup", "Saltar",
        "VulnManoIzq", "VulnManoDer", "VulnCabeza", "VulnPecho", "Tick", "Reposar", "Morir"
    };

    [MenuItem("Templo Utaki/Crear Animator del Golem")]
    static void CrearAnimator()
    {
        if (File.Exists(RutaController))
        {
            bool ok = EditorUtility.DisplayDialog(
                "Crear Animator del Golem",
                "Ya existe " + RutaController + ".\n\nEsto lo SOBRESCRIBE (junto con los clips).\n\n¿Continuar?",
                "Sobrescribir", "Cancelar");
            if (!ok) return;
        }

        if (!Directory.Exists(CarpetaAnim)) Directory.CreateDirectory(CarpetaAnim);

        // ── Cargar sub-sprites de cada hoja ───────────────────────────
        Sprite[] idle      = CargarSprites("Golem_idle");
        Sprite[] salto     = CargarSprites("Golem_Salto_");
        Sprite[] partes    = CargarSprites("Golem_Partes_Debiles_1");
        Sprite[] dano      = CargarSprites("Golem_Dano");
        Sprite[] muerte    = CargarSprites("Golem_Muerte");
        Sprite[] martillo  = CargarSprites("Golem_Martillo_");   // para el prefab (por código)
        Sprite[] proyectil = CargarSprites("Golem_Proyectil_");  // para el prefab (por código)

        if (idle.Length == 0 || partes.Length < 4)
        {
            Debug.LogError("[GolemAnimatorSetup] Faltan sprites (idle o Partes_Debiles). " +
                           "Verificá que estén importados como Sprite Mode: Multiple y cortados.");
            return;
        }

        // ── Clips (solo los que maneja el Animator) ───────────────────
        var clipIdle        = CrearClip("Golem_Idle",        idle,              8f,  true);
        var clipSaltoWindup = CrearClip("Golem_SaltoWindup", Sub(salto, 0, 0),  8f,  true);
        var clipSaltar      = CrearClip("Golem_Saltar",      salto,             6f,  false);
        var clipVulnManoIzq = CrearClip("Golem_VulnManoIzq", Sub(partes, 0, 0), 1f,  true);
        var clipVulnManoDer = CrearClip("Golem_VulnManoDer", Sub(partes, 1, 1), 1f,  true);
        var clipVulnCabeza  = CrearClip("Golem_VulnCabeza",  Sub(partes, 3, 3), 1f,  true);
        var clipVulnPecho   = CrearClip("Golem_VulnPecho",   Sub(partes, 2, 2), 1f,  true);
        var clipTick        = CrearClip("Golem_Tick",        dano,              10f, false);
        var clipMuerte      = CrearClip("Golem_Muerte",      muerte,            8f,  false);

        // ── Controller ────────────────────────────────────────────────
        if (File.Exists(RutaController)) AssetDatabase.DeleteAsset(RutaController);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(RutaController);

        foreach (string t in Triggers)
            controller.AddParameter(t, AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Fase2", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        var stIdle      = NuevoEstado(sm, "Idle",        clipIdle,        new Vector3(300, 0));
        var stEmerger   = NuevoEstado(sm, "Emerger",     clipIdle,        new Vector3(60, -80));
        var stSaltoWind = NuevoEstado(sm, "SaltoWindup", clipSaltoWindup, new Vector3(560, -40));
        var stSaltar    = NuevoEstado(sm, "Saltar",      clipSaltar,      new Vector3(760, -40));
        var stVulnMI    = NuevoEstado(sm, "VulnManoIzq", clipVulnManoIzq, new Vector3(60, 100));
        var stVulnMD    = NuevoEstado(sm, "VulnManoDer", clipVulnManoDer, new Vector3(60, 160));
        var stVulnCab   = NuevoEstado(sm, "VulnCabeza",  clipVulnCabeza,  new Vector3(60, 220));
        var stVulnPec   = NuevoEstado(sm, "VulnPecho",   clipVulnPecho,   new Vector3(60, 280));
        var stTick      = NuevoEstado(sm, "Tick",        clipTick,        new Vector3(300, 160));
        var stMorir     = NuevoEstado(sm, "Morir",       clipMuerte,      new Vector3(300, 240));

        sm.defaultState = stIdle;

        DesdeAny(sm, stIdle,      "Reposar");
        DesdeAny(sm, stEmerger,   "Emerger");
        DesdeAny(sm, stSaltoWind, "SaltoWindup");
        DesdeAny(sm, stSaltar,    "Saltar");
        DesdeAny(sm, stVulnMI,    "VulnManoIzq");
        DesdeAny(sm, stVulnMD,    "VulnManoDer");
        DesdeAny(sm, stVulnCab,   "VulnCabeza");
        DesdeAny(sm, stVulnPec,   "VulnPecho");
        DesdeAny(sm, stTick,      "Tick");
        DesdeAny(sm, stMorir,     "Morir");

        // Retorno automático a Idle (no-hold, no-terminales)
        VolverA(stEmerger, stIdle);
        VolverA(stSaltar,  stIdle);
        VolverA(stTick,    stIdle);
        // SaltoWindup/Vuln*: hold. Morir: terminal.

        AssetDatabase.SaveAssets();

        CablearAlPrefab(controller, martillo, proyectil);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GolemAnimatorSetup] Animator creado en " + RutaController + ".\n" +
                  "Martillo y Proyectil NO están en el Animator: GolemBoss los anima por código " +
                  "(framesMartillo/framesProyectil, asignados al prefab). 'Emerger' usa el idle como placeholder.");
    }

    // ── PREFAB ─────────────────────────────────────────────────────────

    static void CablearAlPrefab(AnimatorController controller, Sprite[] framesMartillo, Sprite[] framesProyectil)
    {
        if (!File.Exists(RutaPrefab))
        {
            Debug.LogWarning("[GolemAnimatorSetup] No existe " + RutaPrefab +
                             ". Creá el prefab (Templo Utaki/Crear Prefab del Golem) y volvé a correr esto.");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(RutaPrefab);
        var anim = root.GetComponent<Animator>();
        if (anim == null) anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        var golem = root.GetComponent<GolemBoss>();
        if (golem != null)
        {
            var so = new SerializedObject(golem);
            so.FindProperty("animator").objectReferenceValue = anim;
            AsignarArraySprites(so.FindProperty("framesMartillo"),  framesMartillo);
            AsignarArraySprites(so.FindProperty("framesProyectil"), framesProyectil);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, RutaPrefab);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[GolemAnimatorSetup] Animator + framesMartillo/framesProyectil cableados al prefab.");
    }

    static void AsignarArraySprites(SerializedProperty prop, Sprite[] sprites)
    {
        if (prop == null || sprites == null) return;
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    // ── HELPERS ─────────────────────────────────────────────────────────

    static Sprite[] CargarSprites(string nombreHoja)
    {
        string path = CarpetaSprites + "/" + nombreHoja + ".png";
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => NumeroFinal(s.name))
            .ToArray();
    }

    // Toma los dígitos finales del nombre, con o sin guion previo
    // ("Golem_Salto0" → 0, "Golem_Proyectil12" → 12, "Golem_Dano_1" → 1).
    static int NumeroFinal(string nombre)
    {
        int i = nombre.Length;
        while (i > 0 && char.IsDigit(nombre[i - 1])) i--;
        if (i < nombre.Length && int.TryParse(nombre.Substring(i), out int n)) return n;
        return 0;
    }

    static Sprite[] Sub(Sprite[] src, int inicio, int fin)
    {
        if (src.Length == 0) return src;
        inicio = Mathf.Clamp(inicio, 0, src.Length - 1);
        fin    = Mathf.Clamp(fin,    inicio, src.Length - 1);
        int len = fin - inicio + 1;
        var res = new Sprite[len];
        System.Array.Copy(src, inicio, res, 0, len);
        return res;
    }

    static AnimationClip CrearClip(string nombre, Sprite[] frames, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = fps };

        var binding = new EditorCurveBinding
        {
            type         = typeof(SpriteRenderer),
            path         = "",
            propertyName = "m_Sprite"
        };

        var keys = new ObjectReferenceKeyframe[Mathf.Max(1, frames.Length)];
        if (frames.Length == 0)
            keys[0] = new ObjectReferenceKeyframe { time = 0f, value = null };
        else
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = CarpetaAnim + "/" + nombre + ".anim";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimatorState NuevoEstado(AnimatorStateMachine sm, string nombre, Motion motion, Vector3 pos)
    {
        var st = sm.AddState(nombre, pos);
        st.motion = motion;
        st.writeDefaultValues = false;
        return st;
    }

    static void DesdeAny(AnimatorStateMachine sm, AnimatorState destino, string trigger)
    {
        var t = sm.AddAnyStateTransition(destino);
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        t.hasExitTime         = false;
        t.duration            = 0f;
        t.canTransitionToSelf = false;
    }

    static void VolverA(AnimatorState desde, AnimatorState destino)
    {
        var t = desde.AddTransition(destino);
        t.hasExitTime = true;
        t.exitTime    = 1f;
        t.duration    = 0f;
    }
}
