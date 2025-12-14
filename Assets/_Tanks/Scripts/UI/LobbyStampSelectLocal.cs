using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class LobbyStampSelectLocal : MonoBehaviour
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
        while (LobbyNet.I == null)
            yield return null;

        LobbyNet.I.OnStamp += OnStampReceived;
    }

    private void OnDestroy()
    {
        if (LobbyNet.I != null)
            LobbyNet.I.OnStamp -= OnStampReceived;
    }

    public void OnClickStamp(int stampId)
    {
        if (stampSprites == null || stampSprites.Length == 0) return;
        if (stampId < 0 || stampId >= stampSprites.Length) return;
        if (selfStampDisplay == null) return;

        selfStampDisplay.Show(stampSprites[stampId]);

        // 送信（接続中のみ）
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient &&
            LobbyNet.I != null)
        {
            LobbyNet.I.SendStampServerRpc(stampId);
        }
    }

    private void OnStampReceived(ulong senderId, int stampId)
    {
        // 自分が送った反射は無視
        if (NetworkManager.Singleton != null &&
            senderId == NetworkManager.Singleton.LocalClientId) return;

        if (stampSprites == null || stampSprites.Length == 0) return;
        if (stampId < 0 || stampId >= stampSprites.Length) return;
        if (otherStampDisplay == null) return;

        otherStampDisplay.Show(stampSprites[stampId]);
    }
}
