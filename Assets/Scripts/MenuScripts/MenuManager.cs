using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject panelOpciones;

    public void IniciarJuego()
    {
        GameLoopManager.Instance?.IniciarJuego();
    }

    public void AbrirOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

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
