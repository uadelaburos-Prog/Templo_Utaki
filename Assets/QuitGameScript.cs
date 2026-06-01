using UnityEngine;

public class QuitGameScript : MonoBehaviour
{
    public void Salir()
    {
        //Application.Quit();

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
    }
}
