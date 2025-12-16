using UnityEngine;
using Photon.Pun;

namespace Tanks.Complete
{
    public class TankNetController : MonoBehaviourPun
    {
        [Header("Local Control Components (only for owner)")]
        [SerializeField] private Behaviour[] localOnlyControls;

        private TankHealthNet healthNet;

        void Awake()
        {
            healthNet = GetComponent<TankHealthNet>();
        }

        [PunRPC]
        public void RpcSetLocalControl(bool enabled)
        {
            // 自分(Owner)だけが実行するRPCとして呼ばれる想定
            if (localOnlyControls == null) return;
            foreach (var b in localOnlyControls)
                if (b != null) b.enabled = enabled;
        }

        [PunRPC]
        public void RpcRespawnLocal(Vector3 pos, Quaternion rot)
        {
            // 自分(Owner)側だけでリスポーン処理
            transform.SetPositionAndRotation(pos, rot);

            if (healthNet != null)
                healthNet.LocalRespawnFull();

            gameObject.SetActive(true);
        }

        [PunRPC]
        public void RpcRespawnAll(Vector3 pos, Quaternion rot)
        {
            transform.SetPositionAndRotation(pos, rot);

            // 速度も消す（吹っ飛びの残り対策）
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // ★ここは「ローカル表示だけ」戻す（RPCは飛ばさない）
            var health = GetComponent<TankHealthNet>();
            if (health != null) health.LocalResetFull_NoRpc();
        }

    }
}

