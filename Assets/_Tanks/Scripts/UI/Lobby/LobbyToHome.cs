using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyToHome : MonoBehaviour
{
    public void OnClickReturnToHome()
    {
        SceneManager.LoadScene("Home");
    }
}
