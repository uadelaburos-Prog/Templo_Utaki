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

        StartCoroutine(FadeAndDestroy(go, sr));
    }

    private IEnumerator FadeAndDestroy(GameObject go, SpriteRenderer sr)
    {
        Color startColor = sr.color;
        float t          = 0f;

        while (t < fadeDuration)
        {
            if (sr == null) yield break;
            t       += Time.deltaTime;
            float a  = Mathf.Lerp(startColor.a, 0f, t / fadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, a);
            yield return null;
        }

        if (go != null) Destroy(go);
    }
}
