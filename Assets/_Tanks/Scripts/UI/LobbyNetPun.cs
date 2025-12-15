using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LobbyNetPun : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static LobbyNetPun I { get; private set; }

    // NGOの senderId(ulong) の代わりに PUNは ActorNumber(int)
    public event Action<int, bool> OnReady;
    public event Action<int, int> OnStamp;

    private const string READY_KEY = "ready";
    private const byte EVT_STAMP = 1;

    [Header("Optional")]
    [SerializeField] private bool autoStartWhenAllReady = true;
    [SerializeField] private byte maxPlayers = 2;
    [SerializeField] private string gameSceneName = "Game";

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        base.OnDisable();
    }

    // ---------- Ready（Custom Properties） ----------
    public void SendReady(bool ready)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;

        var props = new Hashtable { { READY_KEY, ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // 自分のUI即時反映用（元コードのノリに合わせる）
        OnReady?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, ready);

        TryStartIfAllReady();
    }

    public static bool GetReady(Player p)
    {
        if (p == null) return false;
        if (p.CustomProperties == null) return false;
        if (p.CustomProperties.TryGetValue(READY_KEY, out var v) && v is bool b) return b;
        return false;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == null || changedProps == null) return;
        if (!changedProps.ContainsKey(READY_KEY)) return;

        bool ready = GetReady(targetPlayer);
        OnReady?.Invoke(targetPlayer.ActorNumber, ready);

        TryStartIfAllReady();
    }

    private void TryStartIfAllReady()
    {
        if (!autoStartWhenAllReady) return;
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.InRoom) return;

        var players = PhotonNetwork.PlayerList;
        if (players == null || players.Length < maxPlayers) return;

        foreach (var p in players)
        {
            if (!GetReady(p)) return;
        }

        // Masterだけがシーン遷移（AutomaticallySyncScene=true前提）
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    // ---------- Stamp（RaiseEvent） ----------
    public void SendStamp(int stampId)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;

        object payload = new object[] { PhotonNetwork.LocalPlayer.ActorNumber, stampId };

        PhotonNetwork.RaiseEvent(
            EVT_STAMP,
            payload,
            RaiseEventOptions.Default,
            SendOptions.SendReliable
        );
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent == null) return;
        if (photonEvent.Code != EVT_STAMP) return;

        if (photonEvent.CustomData is object[] arr &&
            arr.Length >= 2 &&
            arr[0] is int senderActor &&
            arr[1] is int stampId)
        {
            OnStamp?.Invoke(senderActor, stampId);
        }
    }
}
