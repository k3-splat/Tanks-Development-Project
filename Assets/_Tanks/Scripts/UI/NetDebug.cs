using Unity.Netcode;
using UnityEngine;

public class NetDebug : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += id =>
            Debug.Log($"[NM] ClientConnected: {id}");

        NetworkManager.Singleton.OnClientDisconnectCallback += id =>
            Debug.Log($"[NM] ClientDisconnected: {id}");
    }
}
