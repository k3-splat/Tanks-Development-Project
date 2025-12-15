using UnityEngine;
using Photon.Pun;

namespace Tanks.Complete
{
    public class ShellExplosionNet : MonoBehaviourPun
    {
        public LayerMask m_TankMask;
        public ParticleSystem m_ExplosionParticles;
        public AudioSource m_ExplosionAudio;
        public float m_MaxLifeTime = 20f;

        [HideInInspector] public float m_MaxDamage = 100f;
        [HideInInspector] public float m_ExplosionForce = 50f;
        [HideInInspector] public float m_ExplosionRadius = 5f;

        private bool exploded;

        void Start()
        {
            if (photonView.IsMine)
                Invoke(nameof(DestroyMine), m_MaxLifeTime);
        }

        void DestroyMine()
        {
            if (this != null) PhotonNetwork.Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (exploded) return;
            if (!photonView.IsMine) return; // 砲弾オーナーだけ発火

            exploded = true;

            Vector3 pos = transform.position;
            photonView.RPC(nameof(RpcPlayFx), RpcTarget.All, pos);
            photonView.RPC(nameof(RpcDamageMaster), RpcTarget.MasterClient, pos);

            PhotonNetwork.Destroy(gameObject);
        }

        [PunRPC] void RpcPlayFx(Vector3 pos)
        {
            if (m_ExplosionParticles != null)
            {
                m_ExplosionParticles.transform.parent = null;
                m_ExplosionParticles.transform.position = pos;
                m_ExplosionParticles.Play();
                Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.main.duration);
            }
            if (m_ExplosionAudio != null)
            {
                m_ExplosionAudio.transform.position = pos;
                m_ExplosionAudio.Play();
            }

            // 押す力は “各戦車のオーナーだけ”
            Collider[] cols = Physics.OverlapSphere(pos, m_ExplosionRadius, m_TankMask);
            foreach (var c in cols)
            {
                var rb = c.GetComponent<Rigidbody>();
                if (!rb) continue;

                var tankPv = rb.GetComponentInParent<PhotonView>();
                if (tankPv != null && !tankPv.IsMine) continue;

                var mov = rb.GetComponentInParent<TankMovement>();
                if (mov != null) mov.AddExplosionForce(m_ExplosionForce, pos, m_ExplosionRadius);
            }
        }

        [PunRPC] void RpcDamageMaster(Vector3 pos)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Collider[] cols = Physics.OverlapSphere(pos, m_ExplosionRadius, m_TankMask);
            foreach (var c in cols)
            {
                var rb = c.GetComponent<Rigidbody>();
                if (!rb) continue;

                var hp = rb.GetComponentInParent<TankHealthNet>();
                if (!hp) continue;

                float damage = CalcDamage(pos, rb.position);
                hp.ApplyDamageMasterOnly(damage);
            }
        }

        float CalcDamage(Vector3 explosionPos, Vector3 targetPos)
        {
            float dist = (targetPos - explosionPos).magnitude;
            float rel = (m_ExplosionRadius - dist) / m_ExplosionRadius;
            return Mathf.Max(0f, rel * m_MaxDamage);
        }
    }
}
