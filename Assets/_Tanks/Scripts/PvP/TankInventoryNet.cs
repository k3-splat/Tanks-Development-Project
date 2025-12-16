// File: TankInventoryNet.cs
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    /// 各プレイヤーの弾/地雷在庫をネット同期する。
    /// - Owner(自機)が真値を持つ
    /// - Masterは「加算」をOwnerへRPCで依頼する（取得の確定はMaster側）
    /// </summary>
    [DisallowMultipleComponent]
    public class TankInventoryNet : MonoBehaviourPun, IPunObservable
    {
        // actorNr -> inventory（Masterが拾った処理を当てるため）
        private static readonly Dictionary<int, TankInventoryNet> ByActor = new();

        [Header("Stock")]
        [SerializeField] private int shells = 10;  // 初期弾数（必要に応じて）
        [SerializeField] private int mines  = 1;   // 初期地雷数（必要に応じて）

        [Header("Clamp")]
        [SerializeField] private int maxShells = 99;
        [SerializeField] private int maxMines  = 10;

        public int Shells => shells;
        public int Mines  => mines;

        private void Awake()
        {
            TryRegister();
        }

        private void OnEnable()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            TryUnregister();
        }

        private void OnDestroy()
        {
            TryUnregister();
        }

        private void TryRegister()
        {
            if (photonView == null) return;
            int actor = photonView.OwnerActorNr;
            if (actor <= 0) return;

            // 既に登録があれば上書きしない（念のため）
            if (!ByActor.ContainsKey(actor))
                ByActor.Add(actor, this);
        }

        private void TryUnregister()
        {
            if (photonView == null) return;
            int actor = photonView.OwnerActorNr;
            if (actor <= 0) return;

            if (ByActor.TryGetValue(actor, out var inv) && inv == this)
                ByActor.Remove(actor);
        }

        public static bool TryGetByActor(int actorNr, out TankInventoryNet inv)
        {
            return ByActor.TryGetValue(actorNr, out inv) && inv != null;
        }

        //========================
        // 消費（Ownerのみ）
        //========================
        public bool TryConsumeShell(int amount = 1)
        {
            if (!photonView.IsMine) return false;
            if (amount <= 0) return true;
            if (shells < amount) return false;

            shells -= amount;
            shells = Mathf.Clamp(shells, 0, maxShells);
            return true;
        }

        public bool TryConsumeMine(int amount = 1)
        {
            if (!photonView.IsMine) return false;
            if (amount <= 0) return true;
            if (mines < amount) return false;

            mines -= amount;
            mines = Mathf.Clamp(mines, 0, maxMines);
            return true;
        }

        //========================
        // 取得で増える（MasterがOwnerへ依頼する）
        //========================
        [PunRPC]
        public void RpcAddStock(int addShells, int addMines)
        {
            // 在庫はOwnerが更新する（他人が勝手に更新しない）
            if (!photonView.IsMine) return;

            if (addShells != 0)
                shells = Mathf.Clamp(shells + addShells, 0, maxShells);

            if (addMines != 0)
                mines = Mathf.Clamp(mines + addMines, 0, maxMines);
        }

        //========================
        // 同期
        //========================
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Ownerが送る
                stream.SendNext(shells);
                stream.SendNext(mines);
            }
            else
            {
                // 他人は受け取る
                shells = (int)stream.ReceiveNext();
                mines  = (int)stream.ReceiveNext();
            }
        }

        public void AddStockMaster(int addShells, int addMines)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // Masterが確定 → Ownerにだけ加算を反映（UI/操作に必要なのはOwner側）
            if (photonView.Owner != null)
                photonView.RPC(nameof(RpcAddStock), photonView.Owner, addShells, addMines);
        }

    }
}
