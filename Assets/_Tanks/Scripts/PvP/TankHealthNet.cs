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

        private AudioSource m_ExplosionAudio;
        private ParticleSystem m_ExplosionParticles;

        private float m_CurrentHealth;
        private bool m_Dead;

        void Awake()
        {
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
            m_ExplosionParticles.gameObject.SetActive(false);
            if (m_Slider != null) m_Slider.maxValue = m_StartingHealth;
        }

        void OnEnable()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            SetHealthUI();
        }

        public void LocalRespawnFull()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            SetHealthUI();
        }

        public void ApplyDamageMasterOnly(float amount)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (m_Dead) return;

            m_CurrentHealth -= amount;
            if (m_CurrentHealth <= 0f) m_Dead = true;

            photonView.RPC(nameof(RpcSync), RpcTarget.All, m_CurrentHealth, m_Dead);

            if (m_Dead)
                FindFirstObjectByType<GameManagerNet>()?.MasterReportTankDead(photonView.OwnerActorNr);
        }

        [PunRPC] void RpcSync(float hp, bool dead)
        {
            m_CurrentHealth = hp;
            SetHealthUI();

            if (!m_Dead && dead)
            {
                m_Dead = true;
                OnDeathFx();
                gameObject.SetActive(false);
            }
        }

        void SetHealthUI()
        {
            if (m_Slider != null) m_Slider.value = m_CurrentHealth;
            if (m_FillImage != null)
                m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }

        void OnDeathFx()
        {
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();
        }
    }
}
