using UnityEngine;

public class VoidScript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            GameLoopManager.Instance.PlayerDied();
    }
}
