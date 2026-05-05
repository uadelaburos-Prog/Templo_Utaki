using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] GameObject Options;
    [SerializeField] string Escena;

    private int nextSceneIndex = 0;
    public int NextSceneIndex => nextSceneIndex;
    
    // Update is called once per frame
    public void options()
    {
        Options.SetActive(true);
    }

    public void optionsOut()
    {
        Options.SetActive(false);
    }


    public void Game()
    {
        SceneManager.LoadScene(Escena);
    }
}
