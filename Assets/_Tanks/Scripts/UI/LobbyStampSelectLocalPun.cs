using System.Collections;
using UnityEngine;
using Photon.Pun;

public class LobbyStampSelectLocalPun : MonoBehaviour
{
    [SerializeField] private Sprite[] stampSprites;

    [Header("Self")]
    [SerializeField] private StampDisplay selfStampDisplay;

    [Header("Other")]
    [SerializeField] private StampDisplay otherStampDisplay;

    private void Start()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (LobbyNetPun.I == null)
            yield return null;

        LobbyNetPun.I.OnStamp += OnStampReceived;
    }

    private void OnDestroy()
    {
        if (LobbyNetPun.I != null)
            LobbyNetPun.I.OnStamp -= OnStampReceived;
    }

    public void OnClickStamp(int stampId)
    {
        if (stampSprites == null || stampSprites.Length == 0) return;
        if (stampId < 0 || stampId >= stampSprites.Length) return;
        if (selfStampDisplay == null) return;

        selfStampDisplay.Show(stampSprites[stampId]);

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && LobbyNetPun.I != null)
        {
            LobbyNetPun.I.SendStamp(stampId);
        }
    }

    private void OnStampReceived(int senderActorNumber, int stampId)
    {
        if (PhotonNetwork.LocalPlayer != null &&
            senderActorNumber == PhotonNetwork.LocalPlayer.ActorNumber) return;

        if (stampSprites == null || stampSprites.Length == 0) return;
        if (stampId < 0 || stampId >= stampSprites.Length) return;
        if (otherStampDisplay == null) return;

        otherStampDisplay.Show(stampSprites[stampId]);
    }
}
