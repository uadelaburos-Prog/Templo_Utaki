using System.Collections;
using UnityEngine;

public class AfterimageTrail : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private float spawnInterval = 0.03f;
    [SerializeField] private float fadeDuration  = 0.15f;
    [SerializeField] private Color tintColor     = new Color(1f, 0.7f, 0.7f, 0.6f);

    private bool      _active;
    private Coroutine _spawnRoutine;

    // ── API pública ───────────────────────────────────────────────

    public void StartTrail()
    {
        if (_active) return;
        _active       = true;
        _spawnRoutine = StartCoroutine(LoopSpawn());
    }

    public void StopTrail()
    {
        _active = false;
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    // ── Spawn ─────────────────────────────────────────────────────

    private IEnumerator LoopSpawn()
    {
        while (_active)
        {
            SpawnImage();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnImage()
    {
        if (sourceRenderer == null || sourceRenderer.sprite == null) return;

        var go = new GameObject("Afterimage");
        go.transform.SetPositionAndRotation(
            sourceRenderer.transform.position,
            sourceRenderer.transform.rotation);
        go.transform.localScale = sourceRenderer.transform.lossyScale;

        var sr            = go.AddComponent<SpriteRenderer>();
        sr.sprite         = sourceRenderer.sprite;
        sr.flipX          = sourceRenderer.flipX;
        sr.flipY          = sourceRenderer.flipY;
        sr.sortingLayerID = sourceRenderer.sortingLayerID;
        sr.sortingOrder   = sourceRenderer.sortingOrder - 1;
        sr.color          = tintColor;

        // El fade corre sobre el propio GO — independiente del ciclo de vida del fantasma
        go.AddComponent<AfterimageImage>().Begin(sr, tintColor, fadeDuration);
    }
}

// Componente temporal autocontenido — se agrega dinámicamente a cada afterimage GO.
// Al correr la corrutina sobre sí mismo, Destroy(fantasma) no la interrumpe.
sealed class AfterimageImage : MonoBehaviour
{
    public void Begin(SpriteRenderer sr, Color startColor, float duration)
    {
        StartCoroutine(Fade(sr, startColor, duration));
    }

    private IEnumerator Fade(SpriteRenderer sr, Color startColor, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (sr == null) { Destroy(gameObject); yield break; }
            t       += Time.deltaTime;
            sr.color = new Color(startColor.r, startColor.g, startColor.b,
                                 Mathf.Lerp(startColor.a, 0f, t / duration));
            yield return null;
        }
        Destroy(gameObject);
    }
}
