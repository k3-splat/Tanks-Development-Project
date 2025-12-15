using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Tanks.Complete
{
    public class TankShootingNet : MonoBehaviourPun
    {
        public Transform m_FireTransform;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;

        public float m_MinLaunchForce = 5f;
        public float m_MaxLaunchForce = 20f;
        public float m_MaxChargeTime = 0.75f;
        public float m_ShotCooldown = 1.0f;

        [Header("Explosion Params")]
        public float m_MaxDamage = 100f;
        public float m_ExplosionForce = 50f;
        public float m_ExplosionRadius = 5f;

        [Header("Stock")]
        public WeaponStockData m_ShellStockData;
        public WeaponStockData m_MineStockData;

        [Header("Net Prefab Names (Resources)")]
        [SerializeField] private string shellNetPrefabName = "Shell_Net";
        [SerializeField] private string mineNetPrefabName  = "Mine_Net";

        public event Action<WeaponStockData> OnWeaponStockChanged;
        public event Action<Vector3> OnMinePlaced;

        private TankInputUser m_InputUser;
        private InputAction fireAction;
        private InputAction setMineAction;

        private float m_CurrentLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired;
        private bool m_IsCharging;
        private float m_ShotCooldownTimer;

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
        }

        private void Start()
        {
            if (m_ShellStockData != null)
            {
                m_ShellStockData.InitializeQuantity();
                OnWeaponStockChanged?.Invoke(m_ShellStockData);
            }
            else
            {
                Debug.LogError("[TankShootingNet] m_ShellStockData is NULL", this);
            }

            if (m_MineStockData != null)
            {
                m_MineStockData.InitializeQuantity();
                OnWeaponStockChanged?.Invoke(m_MineStockData);
            }
            else
            {
                Debug.LogError("[TankShootingNet] m_MineStockData is NULL", this);
            }


            m_ShellStockData.InitializeQuantity();
            m_MineStockData.InitializeQuantity();
            OnWeaponStockChanged?.Invoke(m_ShellStockData);
            OnWeaponStockChanged?.Invoke(m_MineStockData);

            fireAction = m_InputUser.ActionAsset.FindAction("Fire");
            setMineAction = m_InputUser.ActionAsset.FindAction("SetMine");
            fireAction.Enable();
            setMineAction.Enable();

            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
            m_CurrentLaunchForce = m_MinLaunchForce;
        }

        private void Update()
        {
            if (!photonView.IsMine) return; // 自機だけ操作

            if (m_ShotCooldownTimer > 0f) m_ShotCooldownTimer -= Time.deltaTime;

            if (m_ShotCooldownTimer <= 0f && fireAction.WasPressedThisFrame() && m_ShellStockData.GetCurrentQuantity() > 0)
            {
                m_Fired = false;
                m_IsCharging = true;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }

            if (m_IsCharging && !m_Fired && fireAction.IsPressed())
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                if (m_CurrentLaunchForce >= m_MaxLaunchForce)
                    m_CurrentLaunchForce = m_MaxLaunchForce;
            }

            if (m_IsCharging && !m_Fired && fireAction.WasReleasedThisFrame())
            {
                FireNet();
                m_IsCharging = false;
            }

            if (setMineAction.WasPressedThisFrame())
            {
                PlaceMineNet();
            }
        }

        private void FireNet()
        {
            m_Fired = true;

            m_ShellStockData.Use();
            OnWeaponStockChanged?.Invoke(m_ShellStockData);

            // ネット生成
            GameObject go = PhotonNetwork.Instantiate(shellNetPrefabName, m_FireTransform.position, m_FireTransform.rotation);

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            // 爆発パラメータをShell側へ渡す（簡易：直接セット）
            var exp = go.GetComponent<ShellExplosionNet>();
            if (exp != null)
            {
                exp.m_MaxDamage = m_MaxDamage;
                exp.m_ExplosionForce = m_ExplosionForce;
                exp.m_ExplosionRadius = m_ExplosionRadius;
            }

            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShotCooldownTimer = m_ShotCooldown;
        }

        private void PlaceMineNet()
        {
            if (m_MineStockData.GetCurrentQuantity() <= 0) return;

            PhotonNetwork.Instantiate(mineNetPrefabName, transform.position - transform.forward * 2f, transform.rotation);

            m_MineStockData.Use();
            OnWeaponStockChanged?.Invoke(m_MineStockData);
            OnMinePlaced?.Invoke(transform.position);
        }
    }
}
