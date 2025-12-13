using UnityEngine;
using TMPro;

public class LobbyReadyLocal : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI readyStatusText;

    private bool selfReady = false;

    private void Start()
    {
        Apply();
    }

    public void OnClickReady()
    {
        selfReady = !selfReady;
        Apply();
    }

    private void Apply()
    {
        if (selfReady)
        {
            readyStatusText.text = "Ready";
            readyStatusText.color = Color.green;
        }
        else
        {
            readyStatusText.text = "Not Ready";
            readyStatusText.color = Color.red;
        }
    }
}
