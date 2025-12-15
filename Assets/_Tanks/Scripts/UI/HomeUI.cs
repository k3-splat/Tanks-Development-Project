using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    public void OnClickVersusPlayer()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void OnClickVersusCPU()
    {
        SceneManager.LoadScene("Main");
    }
}
