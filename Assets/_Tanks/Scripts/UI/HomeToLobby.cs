using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeToLobby : MonoBehaviour
{
    public void OnClickVersusOtherPlayer()
    {
        SceneManager.LoadScene("Lobby");
    }
}
