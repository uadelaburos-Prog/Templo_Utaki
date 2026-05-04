using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseManager : MonoBehaviour
{
       // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] public GameObject BackToSelect;
    [SerializeField] public GameObject Resume;
    [SerializeField] public GameObject PausePanel;
    private bool isPaused;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isPaused)
            {
                resume();

            }

            else
            {

                Pause();

            }

        }
    }
    public void Pause()
    {
        PausePanel.SetActive(true);

        Time.timeScale = 0f;

        AudioListener.pause = true;

        isPaused = true;
    }

    public void resume()
    {
        PausePanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;

        isPaused = false;
    }

    public void GoBack()
    {
       //Volver al menu!!
    }
}
