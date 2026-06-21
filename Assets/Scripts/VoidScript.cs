using UnityEngine;

public class VoidScript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameLoopManager.Instance.PlayerDied(fromVoid: true);
            return;
        }

        if (collision.CompareTag("MummyEnemy"))
        {
            var momia = collision.GetComponent<MummyAI>();
            if (momia != null) momia.Morir();
            else               Destroy(collision.gameObject);
            return;
        }

        if (collision.CompareTag("Key"))
        {
            var llave = collision.GetComponent<KeyItem>();
            if (llave != null) llave.MorirPorVoid();
            else               Destroy(collision.gameObject);
        }
    }
}
