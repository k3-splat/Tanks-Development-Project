using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Tanks.Complete
{
    public class GameManagerNet : MonoBehaviourPunCallbacks
    {
        public static GameManagerNet Instance { get; private set; }

        public enum GameLoopState { RoundStarting, RoundPlaying, RoundEnding }

        public event Action<GameLoopState> OnGameStateChanged;

        [Header("Scene Names")]
        [SerializeField] private string lobbySceneName = "Lobby";

        [Header("Net Prefab (Resources)")]
        [SerializeField] private string tankNetPrefabName = "Tank_Net";

        [Header("Spawns (2 players)")]
        [SerializeField] private Transform[] spawnPoints = new Transform[2];

        [Header("Rounds")]
        public int winsToWin = 5;
        public float startDelay = 3f;
        public float endDelay = 3f;

        [Header("UI (optional)")]
        [SerializeField] private WinsController winsUI;   // PvP HUDのWinsControllerを刺す（任意）

        private TextMeshProUGUI titleText;

        // actorNr -> tank
        private readonly Dictionary<int, TankNetController> tanks = new();
        private readonly Dictionary<int, int> wins = new();

        private bool localSpawned = false;
        private bool roundRunning = false;
        private int roundNumber = 0;

        private GameLoopState _currentState;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            var textRef = FindAnyObjectByType<MessageTextReference>(FindObjectsInactive.Include);
            if (textRef != null) titleText = textRef.Text;

            StartCoroutine(Boot());
        }


        private IEnumerator Boot()
        {
            // ルームに入るまで待つ
            while (!PhotonNetwork.InRoom) yield return null;

            // 2人揃うまで待つ
            while (PhotonNetwork.PlayerList == null || PhotonNetwork.PlayerList.Length < 2)
                yield return null;

            // Inspector必須チェック（ここで落ちるのを防ぐ）
            if (spawnPoints == null || spawnPoints.Length < 2 || spawnPoints[0] == null || spawnPoints[1] == null)
            {
                Debug.LogError("[GameManagerNet] spawnPoints が未設定です。GameManagerNetのInspectorで2つのTransformを必ず設定してください。");
                yield break;
            }

            // ResourcesにPrefabがあるかチェック（Instantiate失敗の早期発見）
            if (Resources.Load<GameObject>(tankNetPrefabName) == null)
            {
                Debug.LogError($"[GameManagerNet] Resourcesに '{tankNetPrefabName}' が見つかりません。Assets/Resources/{tankNetPrefabName}.prefab を確認してください。");
                yield break;
            }

            // 自分の戦車だけ生成（全員に複製される）
            if (!localSpawned)
            {
                int index = GetPlayerIndex(PhotonNetwork.LocalPlayer);
                Transform sp = spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)];
                PhotonNetwork.Instantiate(tankNetPrefabName, sp.position, sp.rotation);
                localSpawned = true;
            }

            // 全員分のTankがRegisterされるまで待つ（安全に）
            int expected = PhotonNetwork.PlayerList.Length;
            float timeout = 10f;
            float t = 0f;

            while (tanks.Count < expected)
            {
                t += Time.deltaTime;
                if (t > timeout)
                {
                    Debug.LogWarning($"[GameManagerNet] RegisterTank が揃っていません。tanks={tanks.Count} expected={expected}（TankNetControllerからRegisterTankが呼ばれてるか確認）");
                    break; // ここで止めずに進める
                }
                yield return null;
            }

            // Masterだけがラウンド進行
            if (PhotonNetwork.IsMasterClient)
            {
                foreach (var p in PhotonNetwork.PlayerList)
                    if (!wins.ContainsKey(p.ActorNumber)) wins[p.ActorNumber] = 0;

                StartCoroutine(MasterGameLoop());
            }
        }

        private int GetPlayerIndex(Player p)
        {
            var sorted = PhotonNetwork.PlayerList.OrderBy(x => x.ActorNumber).ToArray();
            for (int i = 0; i < sorted.Length; i++)
                if (sorted[i].ActorNumber == p.ActorNumber) return i;
            return 0;
        }

        private int GetPlayerIndexByActor(int actorNr)
        {
            var sorted = PhotonNetwork.PlayerList.OrderBy(x => x.ActorNumber).ToArray();
            for (int i = 0; i < sorted.Length; i++)
                if (sorted[i].ActorNumber == actorNr) return i;
            return 0;
        }

        // Tankから呼ばれる（重要）
        public void RegisterTank(TankNetController ctrl)
        {
            if (ctrl == null) return;
            var pv = ctrl.GetComponent<PhotonView>();
            if (pv == null) return;

            int actorNr = pv.OwnerActorNr;
            if (!tanks.ContainsKey(actorNr))
                tanks.Add(actorNr, ctrl);

            if (PhotonNetwork.IsMasterClient && !wins.ContainsKey(actorNr))
                wins[actorNr] = 0;
        }

        private IEnumerator MasterGameLoop()
        {
            while (true)
            {
                yield return StartCoroutine(MasterRoundStarting());
                yield return StartCoroutine(MasterRoundPlaying());
                yield return StartCoroutine(MasterRoundEnding());
            }
        }

        private void SetGameState(GameLoopState s)
        {
            if (_currentState == s) return;
            _currentState = s;
            OnGameStateChanged?.Invoke(s);
        }

        [PunRPC]
        private void RpcSetGameState(int s)
        {
            SetGameState((GameLoopState)s);
        }

        private IEnumerator MasterRoundStarting()
        {
            
            roundRunning = false;
            roundNumber++;

            photonView.RPC(nameof(RpcSetGameState), RpcTarget.All, (int)GameLoopState.RoundStarting);

            photonView.RPC(nameof(RpcSetTitle), RpcTarget.All, $"ROUND {roundNumber}");

            SafeSetControlForAllOwners(false);
            SafeRespawnAllOwners();

            yield return new WaitForSeconds(startDelay);
        }

        private IEnumerator MasterRoundPlaying()
        {
            roundRunning = true;

            photonView.RPC(nameof(RpcSetGameState), RpcTarget.All, (int)GameLoopState.RoundPlaying);

            photonView.RPC(nameof(RpcSetTitle), RpcTarget.All, "");
            SafeSetControlForAllOwners(true);

            while (roundRunning) yield return null;
        }

        private IEnumerator MasterRoundEnding()
        {
            photonView.RPC(nameof(RpcSetGameState), RpcTarget.All, (int)GameLoopState.RoundEnding);

            SafeSetControlForAllOwners(false);

            yield return new WaitForSeconds(endDelay);

            if (wins.Count == 0) yield break;

            int winner = wins.OrderByDescending(kv => kv.Value).First().Key;
            if (wins[winner] >= winsToWin)
            {
                photonView.RPC(nameof(RpcSetTitle), RpcTarget.All, $"PLAYER {GetPlayerIndexByActor(winner) + 1} WINS THE GAME!");
                yield return new WaitForSeconds(2f);
                PhotonNetwork.LoadLevel(lobbySceneName);
            }
        }

        private void SafeSetControlForAllOwners(bool enabled)
        {
            foreach (var kv in tanks.ToArray())
            {
                var ctrl = kv.Value;
                if (ctrl == null) continue;

                var pv = ctrl.GetComponent<PhotonView>();
                if (pv == null) continue;
                if (pv.Owner == null) continue;

                pv.RPC(nameof(TankNetController.RpcSetLocalControl), pv.Owner, enabled);
            }
        }

        private void SafeRespawnAllOwners()
        {
            var sortedActors = PhotonNetwork.PlayerList
                .OrderBy(p => p.ActorNumber)
                .Select(p => p.ActorNumber)
                .ToArray();

            for (int i = 0; i < sortedActors.Length && i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null)
                {
                    Debug.LogError($"[GameManagerNet] spawnPoints[{i}] が null です。Inspector設定してください。");
                    continue;
                }

                int actorNr = sortedActors[i];
                if (!tanks.TryGetValue(actorNr, out var ctrl) || ctrl == null) continue;

                var pv = ctrl.GetComponent<PhotonView>();
                if (pv == null || pv.Owner == null) continue;

                var sp = spawnPoints[i];
                pv.RPC(nameof(TankNetController.RpcRespawnLocal), pv.Owner, sp.position, sp.rotation);
            }
        }

        // TankHealthNetから「Masterだけ」呼ばれる
        public void MasterReportTankDead(int deadActorNr)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!roundRunning) return;

            int winnerActorNr = tanks.Keys.FirstOrDefault(a => a != deadActorNr);
            if (winnerActorNr == 0) return;

            if (!wins.ContainsKey(winnerActorNr)) wins[winnerActorNr] = 0;
            wins[winnerActorNr]++;

            int p1Actor = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).First().ActorNumber;
            int p2Actor = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).Skip(1).First().ActorNumber;

            int p1Wins = wins.ContainsKey(p1Actor) ? wins[p1Actor] : 0;
            int p2Wins = wins.ContainsKey(p2Actor) ? wins[p2Actor] : 0;

            photonView.RPC(nameof(RpcRoundResult), RpcTarget.All,
                GetPlayerIndexByActor(winnerActorNr) + 1, p1Wins, p2Wins, winsToWin);

            roundRunning = false;
        }

        [PunRPC]
        private void RpcRoundResult(int winnerPlayerNo, int p1Wins, int p2Wins, int toWin)
        {
            if (titleText != null)
                titleText.text = $"PLAYER {winnerPlayerNo} WINS THE ROUND!\n\nWins: {p1Wins}/{toWin}  vs  {p2Wins}/{toWin}";

            if (winsUI != null)
                winsUI.UpdateWins(p1Wins, p2Wins, toWin);
        }

        [PunRPC]
        private void RpcSetTitle(string msg)
        {
            if (titleText != null) titleText.text = msg;
        }
    }
}
