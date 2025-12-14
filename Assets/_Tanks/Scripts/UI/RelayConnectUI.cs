using System;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

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
    [SerializeField] private int maxConnections = 1; // Host以外に1人

    // ★ここをDashboardで使ってる環境に合わせて統一（まずは production 推奨）
    private const string UGS_ENV = "production";

    private bool ready = false;

    private async Task EnsureServices(string profileName)
    {
    if (ready) return;

    if (NetworkManager.Singleton == null)
        throw new Exception("NetworkManager がシーンに見つかりません");

    statusText.text = "Initializing UGS...";

    var options = new InitializationOptions().SetEnvironmentName(UGS_ENV);
    await UnityServices.InitializeAsync(options);

    // ★Profile切替は「サインアウト状態」が前提
    if (AuthenticationService.Instance.IsSignedIn)
    {
        AuthenticationService.Instance.SignOut();
    }

    AuthenticationService.Instance.SwitchProfile(profileName);

    statusText.text = "Signing in...";
    await AuthenticationService.Instance.SignInAnonymouslyAsync();

    ready = true;
    Debug.Log($"UGS Ready. Env={UGS_ENV} ProjectId={Application.cloudProjectId} PlayerId={AuthenticationService.Instance.PlayerId} Profile={profileName}");
    statusText.text = $"UGS Ready ({UGS_ENV})";
    }


    public async void OnClickHost()
    {
        try
        {
            await EnsureServices("Host");

            statusText.text = "Creating Relay allocation...";
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 事故防止：表示はコードだけにする（Join Code: を付けない）
            joinCodeText.text = joinCode;

            Debug.Log($"[HOST] Env={UGS_ENV} ProjectId={Application.cloudProjectId} JoinCode={joinCode}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.ConnectionData,
                true
            );

            statusText.text = "Starting Host...";
            bool ok = NetworkManager.Singleton.StartHost();
            Debug.Log($"[HOST] StartHost ok={ok} IsListening={NetworkManager.Singleton.IsListening}");
            statusText.text = ok ? "Host started" : "Host start failed";
        }
        catch (Exception e)
        {
            statusText.text = $"Host error: {e.Message}";
            Debug.LogException(e);
        }
    }

    public async void OnClickJoin()
    {
        try
        {
            await EnsureServices("Client");

            string raw = joinCodeInput.text;
            string code = raw.Trim().ToUpperInvariant();

            Debug.Log($"[JOIN] Env={UGS_ENV} ProjectId={Application.cloudProjectId} raw='{raw}' trimmed='{code}' len={code.Length}");

            if (string.IsNullOrEmpty(code))
            {
                statusText.text = "Enter Join Code";
                return;
            }

            if (code.Length != 6)
            {
                statusText.text = "Join Code must be 6 chars";
                return;
            }

            statusText.text = "Joining Relay...";
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            Debug.Log($"[JOIN] JoinAllocation OK. AllocationIdBytesLen={joinAllocation.AllocationIdBytes?.Length}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                true
            );

            statusText.text = "Starting Client...";
            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log($"[JOIN] StartClient ok={ok} IsListening={NetworkManager.Singleton.IsListening}");
            statusText.text = ok ? "Client started" : "Client start failed";
        }
        catch (Exception e)
        {
            statusText.text = $"Join error: {e.Message}";
            Debug.LogException(e);
        }
    }
}
