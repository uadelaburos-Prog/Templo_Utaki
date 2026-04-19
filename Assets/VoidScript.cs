using UnityEngine;
using UnityEngine.SceneManagement;

public class VoidScript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision != null && collision.CompareTag("Player"))
        {
            GameLoopManager.Instance.PlayerDied();
        }
    }
}
