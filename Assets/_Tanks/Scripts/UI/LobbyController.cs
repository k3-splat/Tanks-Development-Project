using UnityEngine;
using Unity.Netcode;

public class LobbyController : MonoBehaviour
{
    [Header("Stamps")]
    [SerializeField] private Sprite[] stampSprites;   // 0..5
    [SerializeField] private StampDisplay selfStamp;  // 自分
    [SerializeField] private StampDisplay otherStamp; // 相手

    [Header("Ready UI")]
    [SerializeField] private TMPro.TMP_Text selfReadyText;
    [SerializeField] private TMPro.TMP_Text otherReadyText;

    private bool selfReady;
    private bool otherReady;

    private void Start()
    {
        ApplyReadyText(selfReadyText, false);
        ApplyReadyText(otherReadyText, false);

        if (LobbyNet.I != null)
        {
            LobbyNet.I.OnStamp += OnStampReceived;
            LobbyNet.I.OnReady += OnReadyReceived;
        }
    }

    public void OnClickStamp(int stampId)
    {
        if (stampId < 0 || stampId >= stampSprites.Length) return;

        // 自分は即ローカル表示
        selfStamp.Show(stampSprites[stampId]);

        // 接続中なら送る
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && LobbyNet.I != null)
            LobbyNet.I.SendStampServerRpc(stampId);
    }

    public void OnClickReadyToggle()
    {
        selfReady = !selfReady;
        ApplyReadyText(selfReadyText, selfReady);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && LobbyNet.I != null)
            LobbyNet.I.SendReadyServerRpc(selfReady);

        TryStartGame();
    }

    private void OnStampReceived(ulong senderId, int stampId)
    {
        if (NetworkManager.Singleton != null && senderId == NetworkManager.Singleton.LocalClientId) return;
        if (stampId < 0 || stampId >= stampSprites.Length) return;

        otherStamp.Show(stampSprites[stampId]);
    }

    private void OnReadyReceived(ulong senderId, bool ready)
    {
        if (NetworkManager.Singleton != null && senderId == NetworkManager.Singleton.LocalClientId) return;

        otherReady = ready;
        ApplyReadyText(otherReadyText, otherReady);

        TryStartGame();
    }

    private void TryStartGame()
    {
        // ここではまだシーン遷移しない（次のステップでNetworkSceneManagerにする）
        if (selfReady && otherReady)
        {
            Debug.Log("Both ready!");
        }
    }

    private void ApplyReadyText(TMPro.TMP_Text txt, bool ready)
    {
        if (txt == null) return;
        txt.text = ready ? "Ready" : "Not Ready";
        txt.color = ready ? Color.green : Color.red;
    }
}
