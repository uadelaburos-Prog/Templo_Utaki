using UnityEngine;

public class CrystalPickup : MonoBehaviour
{
    [Header("Flotación")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed     = 2f;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Recolección")]
    [SerializeField] private float     pickupRadius = 0.6f;
    [SerializeField] private LayerMask playerMask;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxCristal;

    private Vector3 startPos;
    private float   floatOffset;

    private void Start()
    {
        startPos    = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * floatSpeed + floatOffset) * floatAmplitude;
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);

        if (Physics2D.OverlapCircle(transform.position, pickupRadius, playerMask))
        {
            AudioManager.instance?.FxSoundEffect(sfxCristal, transform, 1f);
            GameLoopManager.Instance.CollectCrystal();
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
