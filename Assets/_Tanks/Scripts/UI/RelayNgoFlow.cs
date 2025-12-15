using System.Threading.Tasks;
using UnityEngine;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public static class RelayNgoflow
{
    private static UnityTransport Transport => NetworkManager.Singleton.GetComponent<UnityTransport>();

    public static async Task StartHostAsync(Allocation allocation)
    {
        var relayServerData = new RelayServerData(allocation, "dtls");
        Transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();
        Debug.Log("NGO Host Started (Relay)");
    }

    public static async Task StartClientAsync(string joinCode)
    {
        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var relayServerData = new RelayServerData(joinAlloc, "dtls");
        Transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();
        Debug.Log("NGO Client Started (Relay)");
    }
}
