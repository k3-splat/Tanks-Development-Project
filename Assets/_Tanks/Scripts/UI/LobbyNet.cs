using System;
using UnityEngine;
using Unity.Netcode;

public class LobbyNet : NetworkBehaviour
{
    public static LobbyNet I { get; private set; }

    public event Action<ulong, bool> OnReady; // (senderClientId, ready)
    public event Action<ulong, int> OnStamp;  // (senderClientId, stampId)

    private void Awake() => I = this;

    [ServerRpc(RequireOwnership = false)]
    public void SendReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
    {
        var sender = rpcParams.Receive.SenderClientId;
        ReadyClientRpc(sender, ready);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendStampServerRpc(int stampId, ServerRpcParams rpcParams = default)
    {
        var sender = rpcParams.Receive.SenderClientId;
        StampClientRpc(sender, stampId);
    }

    [ClientRpc]
    private void ReadyClientRpc(ulong senderId, bool ready)
        => OnReady?.Invoke(senderId, ready);

    [ClientRpc]
    private void StampClientRpc(ulong senderId, int stampId)
        => OnStamp?.Invoke(senderId, stampId);
}
