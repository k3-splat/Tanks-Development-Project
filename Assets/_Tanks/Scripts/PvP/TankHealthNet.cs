using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankHealthNet : MonoBehaviourPun
    {
        public float m_StartingHealth = 100f;
        public Slider m_Slider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        public GameObject m_ExplosionPrefab;

        public float StartingHealth => m_StartingHealth;
        public float CurrentHealth  => m_CurrentHealth;

        private float m_CurrentHealth;
        private bool  m_Dead;

        private AudioSource     m_ExplosionAudio;
        private ParticleSystem  m_ExplosionParticles;

        // 死亡時にOFFにする対象（見た目/当たり判定）
        private Collider[]  _colliders;
        private Renderer[]  _renderers;

        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);

            if (m_ExplosionPrefab != null)
            {
                m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
                m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
                m_ExplosionParticles.gameObject.SetActive(false);
            }

            if (m_Slider != null)
                m_Slider.maxValue = m_StartingHealth;
        }

        private void OnDestroy()
        {
            if (m_ExplosionParticles != null)
                Destroy(m_ExplosionParticles.gameObject);
        }

        private void OnEnable()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            SetAliveVisual(true);
            SetHealthUI();
        }

        // =========================
        // 既存PvPコードが要求してるメソッド
        // =========================

        /// <summary>
        /// TankNetController 側が呼ぶ想定：ローカルでHP全回復＆復活（位置は変えない）
        /// </summary>
        public void LocalRespawnFull()
        {
            LocalRespawnFull(transform.position, transform.rotation);
        }

        /// <summary>
        /// TankNetController 側が呼ぶ想定：ローカルでHP全回復＆復活＋座標更新
        /// </summary>
        public void LocalRespawnFull(Vector3 pos, Quaternion rot)
        {
            transform.SetPositionAndRotation(pos, rot);

            // ローカル見た目を復活
            m_Dead = false;
            m_CurrentHealth = m_StartingHealth;
            SetAliveVisual(true);
            SetHealthUI();

            // HPの“真値”はMasterで確定して全員に同期（ズレ防止）
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RpcSyncHealth), RpcTarget.All, m_StartingHealth, false);
            }
            else
            {
                photonView.RPC(nameof(RpcRequestSetFullHealth), RpcTarget.MasterClient);
            }
        }

        [PunRPC]
        private void RpcRequestSetFullHealth()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            photonView.RPC(nameof(RpcSyncHealth), RpcTarget.All, m_StartingHealth, false);
        }

        /// <summary>
        /// ShellExplpsionNet 側が呼ぶ想定：Masterだけがダメージ確定する入口
        /// </summary>
        public void ApplyDamageMasterOnly(float amount)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ApplyDamage_Master(amount);
            }
            else
            {
                // Master以外から呼ばれた場合でも、Masterに依頼して成立させる
                photonView.RPC(nameof(RpcRequestDamage), RpcTarget.MasterClient, amount);
            }
        }

        // =========================
        // ダメージ処理（Master権威）
        // =========================

        public void TakeDamage(float amount)
        {
            if (m_Dead) return;

            if (PhotonNetwork.IsMasterClient)
            {
                ApplyDamage_Master(amount);
            }
            else
            {
                photonView.RPC(nameof(RpcRequestDamage), RpcTarget.MasterClient, amount);
            }
        }

        [PunRPC]
        private void RpcRequestDamage(float amount)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            ApplyDamage_Master(amount);
        }

        private void ApplyDamage_Master(float amount)
        {
            if (m_Dead) return;

            m_CurrentHealth -= amount;

            if (m_CurrentHealth <= 0f)
            {
                m_CurrentHealth = 0f;
                m_Dead = true;

                photonView.RPC(nameof(RpcSyncHealth), RpcTarget.All, m_CurrentHealth, true);
                photonView.RPC(nameof(RpcDie), RpcTarget.All);
            }
            else
            {
                photonView.RPC(nameof(RpcSyncHealth), RpcTarget.All, m_CurrentHealth, false);
            }
        }

        [PunRPC]
        private void RpcSyncHealth(float hp, bool dead)
        {
            m_CurrentHealth = hp;
            m_Dead = dead;
            SetHealthUI();
            if (dead) SetAliveVisual(false);
        }

        [PunRPC]
        private void RpcDie()
        {
            // 爆発演出
            if (m_ExplosionParticles != null)
            {
                m_ExplosionParticles.transform.position = transform.position;
                m_ExplosionParticles.gameObject.SetActive(true);
                m_ExplosionParticles.Play();
                if (m_ExplosionAudio != null) m_ExplosionAudio.Play();
            }

            // GameObjectは殺さない（RPC/Respawnが届かなくなる事故防止）
            SetAliveVisual(false);
        }

        private void SetHealthUI()
        {
            if (m_Slider != null) m_Slider.value = m_CurrentHealth;

            if (m_FillImage != null)
                m_FillImage.color = Color.Lerp(
                    m_ZeroHealthColor,
                    m_FullHealthColor,
                    (m_StartingHealth <= 0f) ? 0f : (m_CurrentHealth / m_StartingHealth)
                );
        }

        private void SetAliveVisual(bool alive)
        {
            if (_renderers != null)
                foreach (var r in _renderers) if (r != null) r.enabled = alive;

            if (_colliders != null)
                foreach (var c in _colliders) if (c != null) c.enabled = alive;
        }
    }
}
