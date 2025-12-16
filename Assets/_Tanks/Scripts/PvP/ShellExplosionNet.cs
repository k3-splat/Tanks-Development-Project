using Photon.Pun;
using UnityEngine;

namespace Tanks.Complete
{
    public class ShellExplosionNet : MonoBehaviourPun, IPunInstantiateMagicCallback
    {
        [Header("Optional VFX")]
        [SerializeField] private GameObject explosionPrefab;          // 生成するVFX（任意）
        [SerializeField] private ParticleSystem explosionParticles;   // 直置き参照（任意：これがあるなら優先）
        [SerializeField] private AudioSource explosionAudio;          // 任意

        [Header("Tank Mask")]
        [SerializeField] private LayerMask tankMask = ~0;

        private Rigidbody _rb;

        private float _launchForce;
        private float _explosionForce;
        private float _explosionRadius;
        private float _maxDamage;
        private float _lifeTime;

        private bool _exploded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            var data = info.photonView.InstantiationData;
            if (data != null && data.Length >= 5)
            {
                _launchForce     = (float)data[0];
                _explosionForce  = (float)data[1];
                _explosionRadius = (float)data[2];
                _maxDamage       = (float)data[3];
                _lifeTime        = (float)data[4];
            }

            // 全員で同じ初速をセット（必要なら PhotonRigidbodyView で安定化）
            if (_rb != null)
                _rb.linearVelocity = transform.forward * _launchForce;

            // ★寿命で爆発の判定をするのは「オーナーだけ」
            if (photonView.IsMine)
                Invoke(nameof(ExplodeOwner), _lifeTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 当たり判定の決定は「弾の所有者だけ」
            if (!photonView.IsMine) return;
            ExplodeOwner();
        }

        private void OnTriggerEnter(Collider other)
        {
            // 使うならこっちでもOK（Collider運用の場合）
            if (_exploded) return;
            if (!photonView.IsMine) return;

            ExplodeOwner();
        }

        private void ExplodeOwner()
        {
            // ★爆発処理の実行は必ずオーナーだけ
            if (!photonView.IsMine) return;
            if (_exploded) return;
            _exploded = true;

            Vector3 pos = transform.position;

            // 1) 全員に爆発演出
            photonView.RPC(nameof(RpcPlayExplosion), RpcTarget.All, pos);

            // 2) 影響を受ける戦車を探して「その戦車の所有者」にだけ爆風RPC
            var hits = Physics.OverlapSphere(pos, _explosionRadius, tankMask, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                var receiver = h.GetComponentInParent<TankExplosionReceiverNet>();
                if (receiver == null) continue;

                var tankPv = receiver.GetComponent<PhotonView>();
                if (tankPv == null) continue;

                tankPv.RPC(nameof(TankExplosionReceiverNet.RpcExplosionHit), tankPv.Owner,
                    _explosionForce, pos, _explosionRadius, _maxDamage);
            }

            // 3) 弾の破棄は PhotonNetwork.Destroy（オーナーのみ）
            PhotonNetwork.Destroy(gameObject);
        }

        [PunRPC]
        private void RpcPlayExplosion(Vector3 pos)
        {
            transform.position = pos;

            // (A) 直参照の ParticleSystem があるならそれを使う
            if (explosionParticles != null)
            {
                explosionParticles.transform.parent = null;
                explosionParticles.transform.position = pos;
                explosionParticles.Play();

                float dur = 2f;
                var main = explosionParticles.main;
                dur = main.duration + main.startLifetime.constantMax;

                Destroy(explosionParticles.gameObject, dur);
            }
            // (B) 無いなら Prefab を生成して鳴らす
            else if (explosionPrefab != null)
            {
                var go = Instantiate(explosionPrefab, pos, Quaternion.identity);
                Destroy(go, 5f);
            }

            if (explosionAudio != null)
            {
                explosionAudio.Play();
            }

            // ★ここで PhotonNetwork.Destroy は絶対しない（全員が呼ぶので）
        }
    }
}
