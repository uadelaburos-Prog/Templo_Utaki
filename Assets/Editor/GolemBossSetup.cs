using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera el prefab del Jefe Final (Golem) con TODA la jerarquía de hijos y las
/// referencias del GolemBoss ya cableadas: partes débiles (mano1/mano2/cabeza/pecho),
/// hitboxes (cuerpo + martillo izq/der + aterrizaje), telégrafos de aviso, puntos de
/// disparo y el prefab de proyectil.
///
/// Deja una instancia en la escena abierta (seleccionada) y guarda el asset en
/// Assets/Prefabs/GolemBoss.prefab. Las posiciones/tamaños de los hijos son PLACEHOLDERS
/// pensados para un Golem ~6×8u: el diseñador los ajusta para matchear la animación.
///
/// Lo que queda por hacer a mano en el Inspector (no se puede cablear por código):
///   · Sprites/Animator del Golem y de las partes.
///   · AudioClips (aparición, martillo, salto, aterrizaje, proyectil, tick, muerte, música).
///   · UnityEvent alMorir  → ObjetoActivable.Desactivar() de la pared de escape.
///   · UnityEvent alEntrarFase2 → colapso de plataformas.
/// </summary>
public static class GolemBossSetup
{
    private const string CarpetaPrefabs = "Assets/Prefabs";
    private const string RutaPrefab     = CarpetaPrefabs + "/GolemBoss.prefab";
    private const string RutaProyectil  = CarpetaPrefabs + "/Projectile.prefab";

    private const string TagHookable   = "Hookable";
    private const string LayerHookable = "Hookable";
    private const string LayerHazard   = "Hazard";

    [MenuItem("Templo Utaki/Crear Prefab del Golem (Jefe Final)")]
    static void CrearPrefabGolem()
    {
        if (File.Exists(RutaPrefab))
        {
            bool ok = EditorUtility.DisplayDialog(
                "Crear prefab del Golem",
                "Ya existe Assets/Prefabs/GolemBoss.prefab.\n\n" +
                "Esto lo SOBRESCRIBE con la jerarquía base (perdés ajustes hechos en el asset).\n\n¿Continuar?",
                "Sobrescribir", "Cancelar");
            if (!ok) return;
        }

        // ── Raíz ──────────────────────────────────────────────────────
        var root = new GameObject("GolemBoss");
        var rootSr = root.AddComponent<SpriteRenderer>();
        rootSr.sortingLayerName = "Entities";
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var golem = root.AddComponent<GolemBoss>();

        // ── Cuerpo (contacto = reinicio, GDD) ─────────────────────────
        var cuerpo = CrearHitbox("CuerpoDanio", root.transform, Vector2.zero, new Vector2(4f, 7f), LayerHazard);

        // ── Partes débiles (orden: mano1, mano2, cabeza, pecho) ───────
        var mano1  = CrearWeakPoint("Mano1_Izq", root.transform, new Vector2(-2.5f, -0.5f));
        var mano2  = CrearWeakPoint("Mano2_Der", root.transform, new Vector2( 2.5f, -0.5f));
        var cabeza = CrearWeakPoint("Cabeza",    root.transform, new Vector2( 0f,    3f));
        var pecho  = CrearWeakPoint("Pecho",     root.transform, new Vector2( 0f,    0.5f));

        // ── Hitboxes de ataque (empiezan apagadas) ────────────────────
        var hbMartilloIzq = CrearHitbox("HitboxMartilloIzq", root.transform, new Vector2(-3f, -3f), new Vector2(2f, 1.5f), LayerHazard);
        var hbMartilloDer = CrearHitbox("HitboxMartilloDer", root.transform, new Vector2( 3f, -3f), new Vector2(2f, 1.5f), LayerHazard);
        var hbAterrizaje  = CrearHitbox("HitboxAterrizaje",  root.transform, new Vector2( 0f, -3.5f), new Vector2(5f, 1.5f), LayerHazard);
        // Franja del suelo peligroso del martillo: GolemBoss la posiciona y redimensiona en runtime.
        var hbSuelo       = CrearHitbox("HitboxSuelo",       root.transform, new Vector2( 0f, -3.5f), new Vector2(1f, 1f), LayerHazard);

        // ── Telégrafos de aviso ───────────────────────────────────────
        // Los del martillo se expanden hacia su costado (efecto "! ! ! ! golem ! ! ! !").
        var tgMartilloIzq = CrearTelegrafo("TelegrafoMartilloIzq", root.transform, new Vector2(-3f, -3.6f), new Vector2(2f, 0.5f));
        var tgMartilloDer = CrearTelegrafo("TelegrafoMartilloDer", root.transform, new Vector2( 3f, -3.6f), new Vector2(2f, 0.5f));
        ConfigurarExpansion(tgMartilloIzq, 3, 1f, Vector2.left);
        ConfigurarExpansion(tgMartilloDer, 3, 1f, Vector2.right);
        var tgProyIzq     = CrearTelegrafo("TelegrafoProyectilIzq", root.transform, new Vector2(-3.5f, -1f), new Vector2(1f, 1f));
        var tgProyDer     = CrearTelegrafo("TelegrafoProyectilDer", root.transform, new Vector2( 3.5f, -1f), new Vector2(1f, 1f));
        var tgAterrizaje  = CrearTelegrafo("TelegrafoAterrizaje",  root.transform, new Vector2( 0f, -3.6f), new Vector2(5f, 0.5f));

        // ── Puntos de disparo ─────────────────────────────────────────
        var puntoIzq = CrearVacio("PuntoDisparoIzq", root.transform, new Vector2(-3f, -1f));
        var puntoDer = CrearVacio("PuntoDisparoDer", root.transform, new Vector2( 3f, -1f));

        // ── Cableado del GolemBoss ────────────────────────────────────
        var so = new SerializedObject(golem);

        so.FindProperty("sr").objectReferenceValue          = rootSr;
        so.FindProperty("cuerpoDanio").objectReferenceValue = cuerpo;
        so.FindProperty("hitboxSuelo").objectReferenceValue = hbSuelo;

        var partes = so.FindProperty("partesDebiles");
        partes.arraySize = 4;
        partes.GetArrayElementAtIndex(0).objectReferenceValue = mano1;
        partes.GetArrayElementAtIndex(1).objectReferenceValue = mano2;
        partes.GetArrayElementAtIndex(2).objectReferenceValue = cabeza;
        partes.GetArrayElementAtIndex(3).objectReferenceValue = pecho;

        so.FindProperty("hitboxMartilloIzq").objectReferenceValue    = hbMartilloIzq;
        so.FindProperty("hitboxMartilloDer").objectReferenceValue    = hbMartilloDer;
        so.FindProperty("hitboxAterrizaje").objectReferenceValue     = hbAterrizaje;
        so.FindProperty("telegrafoMartilloIzq").objectReferenceValue = tgMartilloIzq;
        so.FindProperty("telegrafoMartilloDer").objectReferenceValue = tgMartilloDer;
        so.FindProperty("telegrafoProyectilIzq").objectReferenceValue = tgProyIzq;
        so.FindProperty("telegrafoProyectilDer").objectReferenceValue = tgProyDer;
        so.FindProperty("telegrafoAterrizaje").objectReferenceValue   = tgAterrizaje;
        so.FindProperty("puntoDisparoIzq").objectReferenceValue = puntoIzq.transform;
        so.FindProperty("puntoDisparoDer").objectReferenceValue = puntoDer.transform;

        // maskSuelo: default sensato (Ground + Platform si existen). El diseñador ajusta.
        so.FindProperty("maskSuelo").intValue = MaskDe("Ground", "Platform");

        // Proyectil: auto-asignar el prefab existente si está.
        var proyectil = AssetDatabase.LoadAssetAtPath<GameObject>(RutaProyectil);
        if (proyectil != null)
            so.FindProperty("prefabProyectil").objectReferenceValue = proyectil;
        else
            Debug.LogWarning($"[GolemBossSetup] No se encontró {RutaProyectil}; asigná 'prefabProyectil' a mano.");

        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Guardar prefab + dejar instancia en escena ────────────────
        if (!Directory.Exists(CarpetaPrefabs)) Directory.CreateDirectory(CarpetaPrefabs);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, RutaPrefab, InteractionMode.UserAction);

        Undo.RegisterCreatedObjectUndo(root, "Crear Golem");
        Selection.activeGameObject = root;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[GolemBossSetup] Prefab del Golem creado en " + RutaPrefab + ".\n" +
                  "PENDIENTE a mano: sprites/Animator, AudioClips, cablear los UnityEvent " +
                  "'alMorir' (→ pared de escape) y 'alEntrarFase2' (→ colapso), y asignar " +
                  "'tilemapSuelo' + 'tilePeligroso' EN LA ESCENA (el suelo peligroso del martillo). " +
                  "Ajustar posiciones de partes/hitboxes/telégrafos a la animación, y colocar un " +
                  "BossFightTrigger en la arena.");
    }

    // ── HELPERS ───────────────────────────────────────────────────────

    // Parte débil hookeable INVISIBLE: Rigidbody2D (Dynamic) + BoxCollider2D (trigger)
    // + GolemWeakPoint, con Tag y Layer "Hookable". El resaltado lo da el cuerpo del Golem.
    static GolemWeakPoint CrearWeakPoint(string nombre, Transform padre, Vector2 localPos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = localPos;

        AsignarTag(go, TagHookable);
        AsignarLayer(go, LayerHookable);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(1.2f, 1.2f);

        return go.AddComponent<GolemWeakPoint>();
    }

    // Hitbox de daño: BoxCollider2D (trigger) + GolemHitbox. Empieza apagada.
    static GolemHitbox CrearHitbox(string nombre, Transform padre, Vector2 localPos, Vector2 size, string layer)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = localPos;
        AsignarLayer(go, layer);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = size;

        return go.AddComponent<GolemHitbox>();
    }

    // Telégrafo de aviso: TelegrafoAtaque con SpriteRenderer (cuadro autogenerado en runtime).
    static TelegrafoAtaque CrearTelegrafo(string nombre, Transform padre, Vector2 localPos, Vector2 escala)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = new Vector3(escala.x, escala.y, 1f);
        return go.AddComponent<TelegrafoAtaque>();
    }

    // Configura la expansión lateral de un telégrafo (copias que se revelan hacia un costado).
    static void ConfigurarExpansion(TelegrafoAtaque telegrafo, int copias, float espaciado, Vector2 direccion)
    {
        var so = new SerializedObject(telegrafo);
        so.FindProperty("copiasPorLado").intValue        = copias;
        so.FindProperty("espaciadoCopias").floatValue    = espaciado;
        so.FindProperty("direccionExpansion").vector2Value = direccion;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CrearVacio(string nombre, Transform padre, Vector2 localPos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = localPos;
        return go;
    }

    static void AsignarTag(GameObject go, string tag)
    {
        try { go.tag = tag; }
        catch { Debug.LogWarning($"[GolemBossSetup] Falta el Tag '{tag}'. Asignalo a mano en {go.name}."); }
    }

    static void AsignarLayer(GameObject go, string layer)
    {
        int l = LayerMask.NameToLayer(layer);
        if (l >= 0) go.layer = l;
        else Debug.LogWarning($"[GolemBossSetup] Falta el Layer '{layer}'. Asignalo a mano en {go.name}.");
    }

    // Bitmask de LayerMask con los layers que existan de la lista dada.
    static int MaskDe(params string[] layers)
    {
        int mask = 0;
        foreach (string s in layers)
        {
            int l = LayerMask.NameToLayer(s);
            if (l >= 0) mask |= 1 << l;
        }
        return mask;
    }
}
