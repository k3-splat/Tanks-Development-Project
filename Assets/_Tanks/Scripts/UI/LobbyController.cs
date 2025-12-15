using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class LobbyController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string homeScene = "Home";
    [SerializeField] private string gameScene = "Game";

    [Header("UI Hooks (好きなUIに差し替えOK)")]
    public StampView myStampView;
    public StampView opponentStampView;
    public ReadyView myReadyView;
    public ReadyView opponentReadyView;

    private Lobby _lobby;
    private string _playerId;
    private Coroutine _pollCoroutine;

    // Lobby/Player Data keys
    private const string KEY_READY = "ready";      // "0" or "1"
    private const string KEY_STAMP = "stamp";      // e.g. "3|1700000000"
    private const string KEY_STATE = "state";      // "lobby" / "starting"
    private const string KEY_RELAY = "relayCode";  // join code

    private async void Start()
    {
        await UgsBootstrapper.InitAsync();
        _playerId = AuthenticationService.Instance.PlayerId;

        // ロビー入室（なければ作成）
        await QuickJoinOrCreateAsync();

        _pollCoroutine = StartCoroutine(PollLobbyLoop());
    }

    public async void OnClickLeaveLobby()
    {
        await LeaveLobbyAsync();
        SceneManager.LoadScene(homeScene);
    }

    public async void OnClickReady()
    {
        if (_lobby == null) return;
        bool newReady = !GetMyReady();
        await SetMyReadyAsync(newReady);
    }

    // stampId: 0..5 を想定
    public async void OnClickStamp(int stampId)
    {
        if (_lobby == null) return;

        // 自分に表示（5秒で消える、上書きはStampView側）
        myStampView.ShowStamp(stampId);

        // 相手へ通知：PlayerData更新
        string payload = $"{stampId}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        await UpdateMyPlayerDataAsync(KEY_STAMP, payload);
    }

    // -------------------------
    // Matchmaking (2 players)
    // -------------------------
    private async Task QuickJoinOrCreateAsync()
    {
        try
        {
            _lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch
        {
            // 無ければ作る（2人ロビー）
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = BuildPlayer()
            };
            _lobby = await LobbyService.Instance.CreateLobbyAsync("pvp-lobby", 2, options);
        }

        Debug.Log($"Joined Lobby: {_lobby.Id}, HostId={_lobby.HostId}");
        await EnsureInitialStateAsync();
        RefreshUIFromLobby();
    }

    private Player BuildPlayer()
    {
        return new Player(
            id: _playerId,
            data: new Dictionary<string, PlayerDataObject>
            {
                { KEY_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                { KEY_STAMP, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "") },
            }
        );
    }

    private async Task EnsureInitialStateAsync()
    {
        // ロビーDataに state を入れておく
        if (_lobby.Data == null || !_lobby.Data.ContainsKey(KEY_STATE))
        {
            if (IsHost())
            {
                var data = new Dictionary<string, DataObject>
                {
                    { KEY_STATE, new DataObject(DataObject.VisibilityOptions.Member, "lobby") }
                };
                _lobby = await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, new UpdateLobbyOptions { Data = data });
            }
        }
    }

    // -------------------------
    // Polling (簡単で堅牢)
    // -------------------------
    private IEnumerator PollLobbyLoop()
    {
        var wait = new WaitForSeconds(1.0f);
        while (true)
        {
            yield return wait;
            _ = PollOnceAsync(); // fire and forget（例外はログ）
        }
    }

    private async Task PollOnceAsync()
    {
        if (_lobby == null) return;

        try
        {
            _lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);
            RefreshUIFromLobby();

            // 両者READYならHostがRelay用意 → state=starting → Gameへ
            if (BothReady())
            {
                if (IsHost())
                {
                    await HostPrepareRelayAndStartAsync();
                }
            }

            // state=starting & relayCodeあり を検知したら、全員Gameへ
            if (_lobby.Data != null && _lobby.Data.TryGetValue(KEY_STATE, out var stateObj))
            {
                if (stateObj.Value == "starting" && _lobby.Data.TryGetValue(KEY_RELAY, out var relayObj))
                {
                    string joinCode = relayObj.Value;
                    await RelayNgoflow.StartClientAsync(joinCode);
                    SceneManager.LoadScene(gameScene);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning(e);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    // -------------------------
    // UI Sync
    // -------------------------
    private void RefreshUIFromLobby()
    {
        var (me, opp) = GetMeAndOpponent();

        // READY表示
        myReadyView.SetReady(ReadReady(me));
        opponentReadyView.SetReady(ReadReady(opp));

        // 相手スタンプ検知（変化したら表示）
        int? stamp = ReadStampIfAny(opp);
        if (stamp.HasValue)
        {
            opponentStampView.ShowStamp(stamp.Value);
        }
    }

    private bool ReadReady(Player p)
    {
        if (p == null || p.Data == null) return false;
        return p.Data.TryGetValue(KEY_READY, out var o) && o.Value == "1";
    }

    private int? ReadStampIfAny(Player p)
    {
        if (p == null || p.Data == null) return null;
        if (!p.Data.TryGetValue(KEY_STAMP, out var o)) return null;
        if (string.IsNullOrWhiteSpace(o.Value)) return null;

        // "stampId|timestamp"
        var parts = o.Value.Split('|');
        if (parts.Length < 1) return null;
        if (int.TryParse(parts[0], out int id)) return id;
        return null;
    }

    // -------------------------
    // READY / PlayerData update
    // -------------------------
    private bool GetMyReady()
    {
        var (me, _) = GetMeAndOpponent();
        return ReadReady(me);
    }

    private async Task SetMyReadyAsync(bool ready)
    {
        await UpdateMyPlayerDataAsync(KEY_READY, ready ? "1" : "0");
    }

    private async Task UpdateMyPlayerDataAsync(string key, string value)
    {
        var data = new Dictionary<string, PlayerDataObject>
        {
            { key, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value) }
        };
        _lobby = await LobbyService.Instance.UpdatePlayerAsync(_lobby.Id, _playerId, new UpdatePlayerOptions { Data = data });
    }

    // -------------------------
    // Relay + Start Host
    // -------------------------
    private async Task HostPrepareRelayAndStartAsync()
    {
        // 既にrelayCodeが入ってたら二重実行しない
        if (_lobby.Data != null && _lobby.Data.ContainsKey(KEY_RELAY)) return;

        // Relay割り当て（2人）
        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(1 /* joiners */);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        // ロビーにrelayCode & state を書き込む（相手が検知して参加）
        var lobbyData = new Dictionary<string, DataObject>
        {
            { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
            { KEY_STATE, new DataObject(DataObject.VisibilityOptions.Member, "starting") },
        };
        _lobby = await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, new UpdateLobbyOptions { Data = lobbyData });

        // Host側もNGO開始してGameへ
        await RelayNgoflow.StartHostAsync(alloc);
        SceneManager.LoadScene(gameScene);
    }

    // -------------------------
    // Helpers
    // -------------------------
    private (Player me, Player opp) GetMeAndOpponent()
    {
        Player me = null, opp = null;
        foreach (var p in _lobby.Players)
        {
            if (p.Id == _playerId) me = p;
            else opp = p;
        }
        return (me, opp);
    }

    private bool BothReady()
    {
        var (me, opp) = GetMeAndOpponent();
        return ReadReady(me) && ReadReady(opp);
    }

    private bool IsHost() => _lobby != null && _lobby.HostId == _playerId;

    private async Task LeaveLobbyAsync()
    {
        if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
        if (_lobby == null) return;
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, _playerId);
        }
        catch { /* ignore */ }
        _lobby = null;
    }
}
