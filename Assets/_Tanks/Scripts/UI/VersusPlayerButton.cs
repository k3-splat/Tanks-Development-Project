
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VersusButton : MonoBehaviour
{
    [SerializeField] private Button versusButton;

    [SerializeField] private string gameSceneName = "Main";

    void Reset()
    {
        if (versusButton == null) versusButton = GetComponent<Button>();
    }

    void Start()
    {
        if (versusButton == null) versusButton = GetComponent<Button>();
        versusButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        versusButton.interactable = false;
        SceneManager.LoadScene(gameSceneName);
    }
}
