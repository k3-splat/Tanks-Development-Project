using System.Collections;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class LobbyReadyLocal : MonoBehaviour
{
    [Header("Self")]
    [SerializeField] private TextMeshProUGUI readyStatusText;

    [Header("Other")]
    [SerializeField] private TextMeshProUGUI otherReadyStatusText;

    private bool selfReady = false;
    private bool otherReady = false;

    private void Start()
    {
        ApplySelf();
        ApplyOther();
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // LobbyNet が起動するまで待つ（順序問題の保険）
        while (LobbyNet.I == null)
            yield return null;

        LobbyNet.I.OnReady += OnReadyReceived;
    }

    private void OnDestroy()
    {
        if (LobbyNet.I != null)
            LobbyNet.I.OnReady -= OnReadyReceived;
    }

    public void OnClickReady()
    {
        selfReady = !selfReady;

        // 送信（接続中のみ）
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient &&
            LobbyNet.I != null)
        {
            LobbyNet.I.SendReadyServerRpc(selfReady);
        }

        ApplySelf();
    }

    private void OnReadyReceived(ulong senderId, bool ready)
    {
        // 自分が送った反射は無視
        if (NetworkManager.Singleton != null &&
            senderId == NetworkManager.Singleton.LocalClientId) return;

        otherReady = ready;
        ApplyOther();
    }

    private void ApplySelf()
    {
        if (readyStatusText == null) return;

        readyStatusText.text = selfReady ? "Ready" : "Not Ready";
        readyStatusText.color = selfReady ? Color.green : Color.red;
    }

    private void ApplyOther()
    {
        if (otherReadyStatusText == null) return;

        otherReadyStatusText.text = otherReady ? "Ready" : "Not Ready";
        otherReadyStatusText.color = otherReady ? Color.green : Color.red;
    }
}
