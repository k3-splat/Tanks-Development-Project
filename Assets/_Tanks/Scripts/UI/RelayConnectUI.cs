using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Text;
using System.Linq;

using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using TMPro;

public class RelayConnectUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text statusText;

    [Header("Relay")]
    [SerializeField] private int maxConnections = 4;

    private const string UGS_ENV = "production";
    private bool ready = false;
    private string role = "Unknown"; // "Host" or "Client"

    private string Tag => $"[RelayUI][{role}][pid={SafePlayerId()}][cloud={Application.cloudProjectId}]";

    private void Awake()
    {
        Debug.Log($"{Tag} Awake()");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError($"{Tag} NetworkManager.Singleton is NULL in Awake");
            return;
        }

        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;

        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Debug.Log($"{Tag} OnClientConnectedCallback id={id} localId={NetworkManager.Singleton.LocalClientId} isHost={NetworkManager.Singleton.IsHost} isClient={NetworkManager.Singleton.IsClient} connectedCount={NetworkManager.Singleton.ConnectedClientsList?.Count}");
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            Debug.Log($"{Tag} OnClientDisconnectCallback id={id} localId={NetworkManager.Singleton.LocalClientId} connectedCount={NetworkManager.Singleton.ConnectedClientsList?.Count}");
        };

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        Debug.Log(utp ? $"{Tag} UnityTransport found" : $"{Tag} UnityTransport NOT found");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
    }

    private void OnTransportFailure()
    {
        Debug.LogError($"{Tag} OnTransportFailure fired! IsListening={NetworkManager.Singleton.IsListening} isHost={NetworkManager.Singleton.IsHost} isClient={NetworkManager.Singleton.IsClient}");
    }

    private async Task EnsureServices(string profileName)
    {
        if (ready) return;

        role = profileName;

        statusText.text = "Initializing UGS...";
        Debug.Log($"{Tag} UnityServices.InitializeAsync START env={UGS_ENV} profile={profileName}");

        var options = new InitializationOptions()
            .SetEnvironmentName(UGS_ENV); // ★赤波線なら下の説明を見てね

        // プロファイル分離（同一PCで複数起動する可能性に備える）
        // ※Authの拡張が生きてるならこれが一番安全
        options.SetProfile(profileName);

        await UnityServices.InitializeAsync(options);

        Debug.Log($"{Tag} UnityServices.InitializeAsync DONE state={UnityServices.State}");

        statusText.text = "Signing in...";
        Debug.Log($"{Tag} SignInAnonymouslyAsync START");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"{Tag} SignInAnonymouslyAsync DONE playerId={AuthenticationService.Instance.PlayerId}");

        ready = true;
        statusText.text = $"UGS Ready ({UGS_ENV})";
        Debug.Log($"{Tag} UGS Ready");
    }

    public async void OnClickHost()
    {
        role = "Host";
        Debug.Log($"{Tag} OnClickHost()");

        try
        {
            await EnsureServices("Host");

            statusText.text = "Creating Relay allocation...";
            Debug.Log($"{Tag} CreateAllocationAsync START maxConnections={maxConnections}");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            Debug.Log($"{Tag} CreateAllocationAsync DONE allocId={allocation.AllocationId} ipv4={allocation.RelayServer.IpV4} port={allocation.RelayServer.Port}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) throw new Exception("NetworkManagerにUnityTransportが付いていません");

            // ★先にRelayへBindする（SetRelayServerData → StartHost）
            Debug.Log($"{Tag} SetRelayServerData(host) START");
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.ConnectionData,
                true
            );
            Debug.Log($"{Tag} SetRelayServerData(host) DONE");

            statusText.text = "Starting Host...";
            Debug.Log($"{Tag} StartHost() START");
            bool ok = NetworkManager.Singleton.StartHost();
            Debug.Log($"{Tag} StartHost() DONE ok={ok} IsListening={NetworkManager.Singleton.IsListening} localId={NetworkManager.Singleton.LocalClientId}");
            if (!ok) throw new Exception("StartHost failed");

            // ★ここで JoinCode を取る（順序が肝）
            statusText.text = "Getting Join Code...";
            Debug.Log($"{Tag} GetJoinCodeAsync START");
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"{Tag} GetJoinCodeAsync DONE joinCode={joinCode}");

            joinCodeText.text = joinCode;
            GUIUtility.systemCopyBuffer = joinCode;
            Debug.Log($"{Tag} Copied JoinCode to clipboard");

            statusText.text = "Host started (JoinCode ready)";
        }
        catch (Exception e)
        {
            statusText.text = $"Host error: {e.Message}";
            Debug.LogError($"{Tag} Host exception:\n{e}");
        }
    }

    public async void OnClickJoin()
    {
        role = "Client";
        Debug.Log($"{Tag} OnClickJoin()");

        try
        {
            await EnsureServices("Client");

            string raw = joinCodeInput.text;
            string code = raw.Trim()
                .Normalize(NormalizationForm.FormKC)
                .ToUpperInvariant();

            code = new string(code.Where(ch =>
                (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')
            ).ToArray());

            Debug.Log($"{Tag} JoinCode raw='{raw}' normalized='{code}' len={code.Length}");

            if (code.Length != 6) throw new Exception("Join Code must be 6 chars");

            statusText.text = "Joining Relay...";
            Debug.Log($"{Tag} JoinAllocationAsync START code={code}");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            Debug.Log($"{Tag} JoinAllocationAsync DONE ipv4={joinAllocation.RelayServer.IpV4} port={joinAllocation.RelayServer.Port}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) throw new Exception("NetworkManagerにUnityTransportが付いていません");

            Debug.Log($"{Tag} SetRelayServerData(client) START");
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                true
            );
            Debug.Log($"{Tag} SetRelayServerData(client) DONE");

            statusText.text = "Starting Client...";
            Debug.Log($"{Tag} StartClient() START");
            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log($"{Tag} StartClient() DONE ok={ok} IsListening={NetworkManager.Singleton.IsListening} localId={NetworkManager.Singleton.LocalClientId}");

            statusText.text = ok ? "Client started" : "Client start failed";
        }
        catch (Exception e)
        {
            statusText.text = $"Join error: {e.Message}";
            Debug.LogError($"{Tag} Join exception:\n{e}");
        }
    }

    private string SafePlayerId()
    {
        try
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
                return AuthenticationService.Instance.PlayerId;
            return "not-signed-in";
        }
        catch
        {
            return "UGS-not-initialized";
        }
    }
}
