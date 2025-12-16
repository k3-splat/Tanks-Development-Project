using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Tanks.Complete
{
    [RequireComponent(typeof(PhotonView))]
    public class CartridgePickupNet : MonoBehaviourPun
    {
        [Header("Add Amount")]
        [SerializeField] private int addShells = 1;
        [SerializeField] private int addMines  = 0;

        [Header("Validation")]
        [SerializeField] private float validateRadius = 3.0f;

        private bool _requested;
        private bool _taken;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_requested) return;

            var inv = other.GetComponentInParent<TankInventoryNet>();
            if (inv == null) return;

            var tankPv = inv.GetComponent<PhotonView>();
            if (tankPv == null) return;

            // 自機だけが拾うリクエストを送る
            if (!tankPv.IsMine) return;

            _requested = true;

            // “誰の在庫か”をViewIDで渡す（Masterが確実に特定できる）
            photonView.RPC(nameof(RpcRequestPickup), RpcTarget.MasterClient, tankPv.ViewID);
        }

        [PunRPC]
        private void RpcRequestPickup(int inventoryViewId, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (_taken) return;

            var invPv = PhotonView.Find(inventoryViewId);
            if (invPv == null) return;

            // 送信者がそのTankのOwnerかチェック（チート/事故対策）
            Player sender = info.Sender;
            if (sender == null || invPv.OwnerActorNr != sender.ActorNumber) return;

            var inv = invPv.GetComponent<TankInventoryNet>();
            if (inv == null) return;

            // 距離チェック
            float dist = Vector3.Distance(inv.transform.position, transform.position);
            if (dist > validateRadius) return;

            _taken = true;

            // ★重要：Masterが在庫を増やし、全員へ同期
            inv.AddStockMaster(addShells, addMines);

            // ★重要：PickupはMasterがDestroy
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
