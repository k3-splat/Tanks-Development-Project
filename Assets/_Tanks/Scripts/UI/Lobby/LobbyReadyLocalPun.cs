using System.Collections;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class LobbyReadyLocalPun : MonoBehaviour
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
        while (LobbyNetPun.I == null)
            yield return null;

        LobbyNetPun.I.OnReady += OnReadyReceived;
    }

    private void OnDestroy()
    {
        if (LobbyNetPun.I != null)
            LobbyNetPun.I.OnReady -= OnReadyReceived;
    }

    public void OnClickReady()
    {
        selfReady = !selfReady;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && LobbyNetPun.I != null)
        {
            LobbyNetPun.I.SendReady(selfReady);
        }

        ApplySelf();
    }

    private void OnReadyReceived(int senderActorNumber, bool ready)
    {
        if (PhotonNetwork.LocalPlayer != null &&
            senderActorNumber == PhotonNetwork.LocalPlayer.ActorNumber) return;

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
