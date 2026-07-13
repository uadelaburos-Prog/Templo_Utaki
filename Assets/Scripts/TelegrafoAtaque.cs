using System.Collections;
using UnityEngine;

// Marca en rojo la(s) casilla(s) sobre las que va a ocurrir un ataque del Golem, para
// darle al jugador tiempo de leer y esquivar (telegrafío). El GolemBoss lo posiciona y
// llama Mostrar(duracion) durante el windup; el telegrafo parpadea en rojo y se apaga solo.
//
// Expansión lateral (opcional): con copiasPorLado > 0 genera copias del marcador que se
// van revelando progresivamente hacia los costados durante el windup, tipo
// "! ! ! ! golem ! ! ! !". direccionExpansion (0,0) = ambos lados; con dirección, solo
// hacia ese lado (útil para un telegrafo por puño del martillo).
//
// Setup: hijo del Golem o suelto en la arena, con un SpriteRenderer (cuadro/zona). Si no
// se asigna sprite se genera un cuadro blanco tintable. Empieza oculto.
public class TelegrafoAtaque : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("SpriteRenderer del marcador. Si se deja vacío se genera un cuadro blanco.")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color colorAviso    = new Color(1f, 0.15f, 0.1f, 0.55f);
    [SerializeField] private string sortingLayerName = "Tilemap";

    [Header("Parpadeo")]
    [SerializeField] private float intervaloParpadeo = 0.12f;

    [Header("Expansión lateral (opcional)")]
    [Tooltip("Copias del marcador a cada lado (0 = marcador único, sin expansión).")]
    [SerializeField] private int copiasPorLado = 0;
    [Tooltip("Separación entre copias (u).")]
    [SerializeField] private float espaciadoCopias = 1f;
    [Tooltip("Dirección de la expansión. (0,0) = hacia ambos lados; (1,0) = solo derecha; (-1,0) = solo izquierda.")]
    [SerializeField] private Vector2 direccionExpansion = Vector2.zero;
    [Tooltip("Fracción de la duración en la que la expansión se completa (el resto parpadea entera).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float fraccionExpansion = 0.6f;

    // Anillos de expansión: [0] = marcador central, [i] = copias a distancia i*espaciado.
    private SpriteRenderer[][] _anillos;
    private Coroutine _rutina;

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GenerarCuadro();
        }
        sr.sortingLayerName = sortingLayerName;
        sr.color   = colorAviso;
        sr.enabled = false;

        ConstruirAnillos();
    }

    // Muestra el aviso rojo parpadeando durante 'duracion' segundos y se apaga solo.
    // Si hay expansión configurada, las copias aparecen progresivamente hacia los costados.
    public void Mostrar(float duracion)
    {
        if (_rutina != null) StopCoroutine(_rutina);
        _rutina = StartCoroutine(RutinaParpadeo(duracion));
    }

    public void Ocultar()
    {
        if (_rutina != null) { StopCoroutine(_rutina); _rutina = null; }
        OcultarTodos();
    }

    // ── PRIVADOS ──────────────────────────────────────────────────

    private void ConstruirAnillos()
    {
        _anillos    = new SpriteRenderer[Mathf.Max(0, copiasPorLado) + 1][];
        _anillos[0] = new[] { sr };

        for (int i = 1; i < _anillos.Length; i++)
        {
            float dist = i * espaciadoCopias;
            if (direccionExpansion == Vector2.zero)
                _anillos[i] = new[] { CrearCopia(Vector2.right * dist), CrearCopia(Vector2.left * dist) };
            else
                _anillos[i] = new[] { CrearCopia(direccionExpansion.normalized * dist) };
        }
    }

    private SpriteRenderer CrearCopia(Vector2 offsetLocal)
    {
        var go = new GameObject("Copia Telegrafo");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offsetLocal;

        var copia = go.AddComponent<SpriteRenderer>();
        copia.sprite           = sr.sprite;
        copia.sortingLayerName = sr.sortingLayerName;
        copia.sortingOrder     = sr.sortingOrder;
        copia.color            = colorAviso;
        copia.enabled          = false;
        return copia;
    }

    private IEnumerator RutinaParpadeo(float duracion)
    {
        // La expansión completa ocurre en la primera fracción del windup; el resto
        // parpadea la franja entera hasta que el golpe cae.
        float tExpansion = Mathf.Max(0.01f, duracion * fraccionExpansion);
        int   anillosExt = _anillos.Length - 1;

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;

            // Cuántos anillos exteriores ya se revelaron (el central siempre visible).
            int visibles = anillosExt == 0 ? 0
                : Mathf.Min(anillosExt, Mathf.FloorToInt(Mathf.Clamp01(t / tExpansion) * (anillosExt + 1)));

            // Parpadeo: alterna visible/tenue para que se lea como advertencia.
            bool  encendido = (t % intervaloParpadeo) < (intervaloParpadeo * 0.5f);
            Color c = colorAviso;
            c.a = encendido ? colorAviso.a : colorAviso.a * 0.35f;

            for (int i = 0; i < _anillos.Length; i++)
            {
                bool mostrar = i <= visibles;
                foreach (var r in _anillos[i])
                {
                    if (r == null) continue;
                    r.enabled = mostrar;
                    if (mostrar) r.color = c;
                }
            }

            yield return null;
        }

        OcultarTodos();
        _rutina = null;
    }

    private void OcultarTodos()
    {
        if (_anillos == null) return;
        foreach (var anillo in _anillos)
            foreach (var r in anillo)
                if (r != null) r.enabled = false;
    }

    private Sprite GenerarCuadro()
    {
        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
    }
}
