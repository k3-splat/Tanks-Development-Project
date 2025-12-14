using UnityEngine;
using TMPro;
using Unity.Netcode;

public class LobbyConnectionStatus : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playersText;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("NetworkManagerがありません", 0);
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // 起動直後表示
        UpdateTexts();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId) => UpdateTexts();
    private void OnClientDisconnected(ulong clientId) => UpdateTexts();

    private void UpdateTexts()
    {
        var nm = NetworkManager.Singleton;

        int count = nm.ConnectedClientsList != null ? nm.ConnectedClientsList.Count : 0;

        string mode =
            nm.IsHost ? "Host" :
            nm.IsClient ? "Client" :
            "Offline";

        SetStatus($"{mode} / Connected: {nm.IsConnectedClient}", count);
    }

    private void SetStatus(string status, int players)
    {
        if (statusText != null) statusText.text = status;
        if (playersText != null) playersText.text = $"Players: {players}";
    }
}
