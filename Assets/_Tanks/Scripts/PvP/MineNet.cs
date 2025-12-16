// File: MineNet.cs
using Photon.Pun;
using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    /// 対戦車地雷（PUN）
    /// - ドクロ(フィールド/ミニマップ)は「置いた本人だけ」表示
    /// - 起爆確定はMaster（多重爆発/同期崩れ防止）
    /// - 爆風/ダメージ適用は TankExplosionReceiverNet のRPCに寄せる
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class MineNet : MonoBehaviourPun
    {
        [Header("Explosion")]
        [SerializeField] private float explosionForce  = 800f;
        [SerializeField] private float explosionRadius = 6f;
        [SerializeField] private float maxDamage       = 100f;

        [Header("Tank Mask")]
        [SerializeField] private LayerMask tankMask = ~0;

        [Header("Skull Marks (Owner Only)")]
        [SerializeField] private GameObject skullWorld;   // フィールドのドクロ
        [SerializeField] private GameObject skullMinimap; // ミニマップのドクロ

        [Header("Optional VFX")]
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private AudioSource explosionAudio;

        private bool _detonated;

        private void Start()
        {
            // 置いた本人だけドクロ表示（敵機の設置場所は何も表示しない）
            bool owner = photonView.IsMine;
            if (skullWorld   != null) skullWorld.SetActive(owner);
            if (skullMinimap != null) skullMinimap.SetActive(owner);

            // （任意）地雷本体の見た目を隠したいならここでRenderer制御もできる
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_detonated) return;

            // タンク以外は無視したい場合はtag/layerで絞ってOK
            // ここでは tankMask で OverlapSphere 側に絞るので、TriggerEnterは軽い入口だけ

            // 起爆確定はMasterに依頼
            if (PhotonNetwork.IsMasterClient)
            {
                DetonateOnMaster();
            }
            else
            {
                photonView.RPC(nameof(RpcRequestDetonate), RpcTarget.MasterClient);
            }
        }

        [PunRPC]
        private void RpcRequestDetonate()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            DetonateOnMaster();
        }

        private void DetonateOnMaster()
        {
            if (_detonated) return;
            _detonated = true;

            Vector3 pos = transform.position;

            // 1) 全員に演出
            photonView.RPC(nameof(RpcPlayExplosion), RpcTarget.All, pos);

            // 2) 影響タンクを探し、各タンクOwnerにだけ爆風RPC（あなたの既存設計に合わせる）
            var hits = Physics.OverlapSphere(pos, explosionRadius, tankMask, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                var receiver = h.GetComponentInParent<TankExplosionReceiverNet>();
                if (receiver == null) continue;

                var tankPv = receiver.GetComponent<PhotonView>();
                if (tankPv == null || tankPv.Owner == null) continue;

                // TankExplosionReceiverNet.RpcExplosionHit は Owner側で吹っ飛び＆TakeDamage（→Master確定）をやってくれる
                tankPv.RPC(nameof(TankExplosionReceiverNet.RpcExplosionHit), tankPv.Owner,
                    explosionForce, pos, explosionRadius, maxDamage);
            }

            // 3) 地雷の破棄はMasterがPhotonNetwork.Destroy
            PhotonNetwork.Destroy(gameObject);
        }

        [PunRPC]
        private void RpcPlayExplosion(Vector3 pos)
        {
            transform.position = pos;

            if (explosionPrefab != null)
            {
                var go = Instantiate(explosionPrefab, pos, Quaternion.identity);
                Destroy(go, 4f);
            }

            if (explosionAudio != null)
            {
                explosionAudio.Play();
            }
        }
    }
}
