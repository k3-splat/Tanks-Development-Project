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
        public enum GameLoopState { RoundStarting, RoundPlaying, RoundEnding }

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

        private TextMeshProUGUI titleText;

        // actorNr -> tank info
        private readonly Dictionary<int, TankNetController> tanks = new();
        private readonly Dictionary<int, int> wins = new();

        private bool localSpawned = false;
        private bool roundRunning = false;
        private int roundNumber = 0;

        void Start()
        {
            // メッセージテキスト（既存Menus prefabを流用）
            var textRef = FindAnyObjectByType<MessageTextReference>(FindObjectsInactive.Include);
            if (textRef != null) titleText = textRef.Text;

            StartCoroutine(Boot());
        }

        private IEnumerator Boot()
        {
            // ルームに入ってから
            while (!PhotonNetwork.InRoom) yield return null;

            // 2人揃うまで待つ
            while (PhotonNetwork.PlayerList.Length < 2) yield return null;

            // 自分の戦車だけ生成（全員の画面に複製される）
            if (!localSpawned)
            {
                int index = GetPlayerIndex(PhotonNetwork.LocalPlayer);
                Transform sp = spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)];

                PhotonNetwork.Instantiate(tankNetPrefabName, sp.position, sp.rotation);
                localSpawned = true;
            }

            // 両方のTankがRegisterされるまで待つ
            while (tanks.Count < 2) yield return null;

            // Masterだけがラウンド進行を担当
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

        // Tankから呼ばれる
        public void RegisterTank(TankNetController ctrl)
        {
            int actorNr = ctrl.photonView.OwnerActorNr;
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

        private IEnumerator MasterRoundStarting()
        {
            roundRunning = false;
            roundNumber++;

            photonView.RPC(nameof(RpcSetTitle), RpcTarget.All, $"ROUND {roundNumber}");
            SetControlForAllOwners(false);

            // リスポーン
            RespawnAllOwners();

            yield return new WaitForSeconds(startDelay);
        }

        private IEnumerator MasterRoundPlaying()
        {
            roundRunning = true;
            photonView.RPC(nameof(RpcSetTitle), RpcTarget.All, "");
            SetControlForAllOwners(true);

            // 「誰かが死んだ」報告（MasterReportTankDead）待ち
            while (roundRunning) yield return null;
        }

        private IEnumerator MasterRoundEnding()
        {
            SetControlForAllOwners(false);

            // ちょい待ってから次へ
            yield return new WaitForSeconds(endDelay);

            // 勝利条件チェック
            int winner = wins.OrderByDescending(kv => kv.Value).First().Key;
            if (wins[winner] >= winsToWin)
            {
                photonView.RPC(nameof(RpcSetTitle), RpcTarget.All, $"PLAYER {GetPlayerIndexByActor(winner) + 1} WINS THE GAME!");
                yield return new WaitForSeconds(2f);

                // Masterがロビーに戻す（AutomaticallySyncScene=trueなら相手も戻る）
                PhotonNetwork.LoadLevel(lobbySceneName);
            }
        }

        private int GetPlayerIndexByActor(int actorNr)
        {
            var sorted = PhotonNetwork.PlayerList.OrderBy(x => x.ActorNumber).ToArray();
            for (int i = 0; i < sorted.Length; i++)
                if (sorted[i].ActorNumber == actorNr) return i;
            return 0;
        }

        private void SetControlForAllOwners(bool enabled)
        {
            foreach (var kv in tanks)
            {
                var ctrl = kv.Value;
                if (ctrl != null)
                    ctrl.photonView.RPC(nameof(TankNetController.RpcSetLocalControl), ctrl.photonView.Owner, enabled);
            }
        }

        private void RespawnAllOwners()
        {
            // actorNr順に Spawn[0]/Spawn[1] を割り当て
            var sortedActors = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).Select(p => p.ActorNumber).ToArray();

            for (int i = 0; i < sortedActors.Length && i < spawnPoints.Length; i++)
            {
                int actorNr = sortedActors[i];
                if (!tanks.TryGetValue(actorNr, out var ctrl) || ctrl == null) continue;

                var sp = spawnPoints[i];
                ctrl.photonView.RPC(nameof(TankNetController.RpcRespawnLocal), ctrl.photonView.Owner, sp.position, sp.rotation);
            }
        }

        // TankHealthNetから「Masterだけ」呼ばれる
        public void MasterReportTankDead(int deadActorNr)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!roundRunning) return;

            // 生存者（=勝者）を決める
            int winnerActorNr = tanks.Keys.FirstOrDefault(a => a != deadActorNr);
            if (winnerActorNr == 0) return;

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
        }

        [PunRPC]
        private void RpcSetTitle(string msg)
        {
            if (titleText != null) titleText.text = msg;
        }
    }
}
