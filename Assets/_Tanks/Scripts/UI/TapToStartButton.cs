using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    [SerializeField] private Button startButton;

    [SerializeField] private string homeSceneName = "HomeScene";

    void Reset()
    {
        if (startButton == null) startButton = GetComponent<Button>();
    }

    void Start()
    {
        if (startButton == null) startButton = GetComponent<Button>();
        startButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        startButton.interactable = false; // 多重押し防止（任意）
        SceneManager.LoadScene(homeSceneName);
    }
}
