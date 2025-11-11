using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeController : MonoBehaviour
{
    public void OnClickVersusPlayer()
    {
        SceneManager.LoadScene("Main");
    }
}
