using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Networking.Transport.Relay;   // RelayServerData
using TMPro;

public class RelayConnectUI : MonoBehaviour
{
    private const string BUILD_STAMP = "2025-12-15_04";  // ★毎回変えて混在を潰す
    private const string UGS_ENV = "production";         // ★必ず同じにする
    private const string RELAY_PROTOCOL = "dtls";        // ★基本これ

    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text statusText;

    [Header("Relay")]
    [SerializeField] private int maxConnections = 1; // Host以外に1人

    [Header("Region")]
    [SerializeField] private bool pinJapanRegion = true; // ★JP固定したいならtrue

    private bool ready = false;
    private string role = "Unknown";

    private string Tag => $"[RelayUI][{role}][pid={SafePlayerId()}][cloud={Application.cloudProjectId}]";

    private void Awake()
    {
        Debug.Log($"{Tag} Awake() BUILD_STAMP={BUILD_STAMP} unity={Application.unityVersion}");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError($"{Tag} NetworkManager.Singleton is NULL");
            return;
        }

        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;

        NetworkManager.Singleton.OnClientConnectedCallback += id =>
            Debug.Log($"{Tag} OnClientConnected id={id} localId={NetworkManager.Singleton.LocalClientId} isHost={NetworkManager.Singleton.IsHost} count={NetworkManager.Singleton.ConnectedClientsList?.Count}");

        NetworkManager.Singleton.OnClientDisconnectCallback += id =>
            Debug.Log($"{Tag} OnClientDisconnect id={id} localId={NetworkManager.Singleton.LocalClientId}");

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        Debug.Log(utp ? $"{Tag} UnityTransport found" : $"{Tag} UnityTransport NOT found");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
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

        // ★SetEnvironmentName はこの using が必須：Unity.Services.Core.Environments :contentReference[oaicite:2]{index=2}
        var options = new InitializationOptions()
            .SetEnvironmentName(UGS_ENV);

        await UnityServices.InitializeAsync(options);
        Debug.Log($"{Tag} UnityServices.InitializeAsync DONE state={UnityServices.State}");

        // ★プロファイルは SignIn 前に切替（ここはあなたの方式でOK）
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log($"{Tag} SignOut()");
            AuthenticationService.Instance.SignOut();
        }

        Debug.Log($"{Tag} SwitchProfile({profileName})");
        AuthenticationService.Instance.SwitchProfile(profileName);

        statusText.text = "Signing in...";
        Debug.Log($"{Tag} SignInAnonymouslyAsync START");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"{Tag} SignInAnonymouslyAsync DONE playerId={AuthenticationService.Instance.PlayerId}");

        ready = true;
        statusText.text = $"UGS Ready ({UGS_ENV})";
        Debug.Log($"{Tag} UGS Ready");
    }

    // ★JPリージョンIDを動的に見つける（ハードコードしない） :contentReference[oaicite:3]{index=3}
    private async Task<string> GetJapanRegionIdOrNull()
    {
        try
        {
            var regions = await RelayService.Instance.ListRegionsAsync();
            foreach (var r in regions)
                Debug.Log($"{Tag} Region: id={r.Id} name={r.Name}");

            var jp = regions.FirstOrDefault(r =>
                (!string.IsNullOrEmpty(r.Name) && r.Name.IndexOf("japan", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(r.Id)   && r.Id.IndexOf("japan", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(r.Name) && r.Name.IndexOf("tokyo", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(r.Id)   && r.Id.IndexOf("tokyo", StringComparison.OrdinalIgnoreCase) >= 0)
            );

            if (jp == null)
            {
                Debug.LogWarning($"{Tag} Japan region not found. Will use default region.");
                return null;
            }

            Debug.Log($"{Tag} Japan region selected: id={jp.Id} name={jp.Name}");
            return jp.Id;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{Tag} ListRegionsAsync failed -> use default. {e.Message}");
            return null;
        }
    }

    public async void OnClickHost()
    {
        role = "Host";
        Debug.Log($"{Tag} OnClickHost()");

        try
        {
            await EnsureServices("Host");

            statusText.text = "Creating Relay allocation...";
            string regionId = null;
            if (pinJapanRegion)
                regionId = await GetJapanRegionIdOrNull();

            Allocation allocation;
            if (!string.IsNullOrEmpty(regionId))
            {
                Debug.Log($"{Tag} CreateAllocationAsync START max={maxConnections} regionId={regionId}");
                allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, regionId); // :contentReference[oaicite:4]{index=4}
            }
            else
            {
                Debug.Log($"{Tag} CreateAllocationAsync START max={maxConnections} region=default");
                allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            }

            Debug.Log($"{Tag} CreateAllocationAsync DONE allocId={allocation.AllocationId} ipv4={allocation.RelayServer.IpV4} port={allocation.RelayServer.Port}");

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"{Tag} JoinCode={joinCode}");
            joinCodeText.text = joinCode;
            GUIUtility.systemCopyBuffer = joinCode;

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (!transport) throw new Exception("NetworkManagerにUnityTransportが付いていません");

            // ★推奨：RelayServerData を使う（引数の取り違え事故が減る）
            var rsd = new RelayServerData(allocation, RELAY_PROTOCOL);
            transport.SetRelayServerData(rsd);

            statusText.text = "Starting Host...";
            bool ok = NetworkManager.Singleton.StartHost();
            Debug.Log($"{Tag} StartHost ok={ok} IsListening={NetworkManager.Singleton.IsListening} localId={NetworkManager.Singleton.LocalClientId}");
            statusText.text = ok ? "Host started" : "Host start failed";
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

            code = new string(code.Where(ch => (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')).ToArray());
            Debug.Log($"{Tag} JoinCode raw='{raw}' normalized='{code}' len={code.Length}");

            if (code.Length != 6)
                throw new Exception("Join Code must be 6 chars (A-Z0-9)");

            statusText.text = "Joining Relay...";
            Debug.Log($"{Tag} JoinAllocationAsync START code={code}");
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            Debug.Log($"{Tag} JoinAllocationAsync DONE ipv4={joinAllocation.RelayServer.IpV4} port={joinAllocation.RelayServer.Port}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (!transport) throw new Exception("NetworkManagerにUnityTransportが付いていません");

            var rsd = new RelayServerData(joinAllocation, RELAY_PROTOCOL);
            transport.SetRelayServerData(rsd);

            statusText.text = "Starting Client...";
            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log($"{Tag} StartClient ok={ok} IsListening={NetworkManager.Singleton.IsListening} localId={NetworkManager.Singleton.LocalClientId}");
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
        catch { return "UGS-not-initialized"; }
    }
}
