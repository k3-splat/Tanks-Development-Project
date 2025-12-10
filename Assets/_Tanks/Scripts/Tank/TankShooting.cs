using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankShooting : MonoBehaviour
    {
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        [Tooltip("The speed in unit/second the shell have when fired at minimum charge")]
        public float m_MinLaunchForce = 5f;        // The force given to the shell if the fire button is not held.
        [Tooltip("The speed in unit/second the shell have when fired at max charge")]
        public float m_MaxLaunchForce = 20f;        // The force given to the shell if the fire button is held for the max charge time.
        [Tooltip("The maximum time spent charging. When charging reach that time, the shell is fired at MaxLaunchForce")]
        public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.
        [Tooltip("The time that must pass before being able to shoot again after a shot")]
        public float m_ShotCooldown = 1.0f;         // The time required between 2 shots
        [Header("Shell Properties")]
        [Tooltip("The amount of health removed to a tank if they are exactly on the landing spot of a shell")]
        public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
        [Tooltip("The force of the explosion at the shell position. Keep it 50 and below")]
        public float m_ExplosionForce = 50f;              // The amount of force added to a tank at the centre of the explosion.
        [Tooltip("The radius of the explosion in Unity unit. Force decrease with distance to the center, and an tank further than this from the shell explosion won't be impacted by the explosion")]
        public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.

        [HideInInspector]
        public TankInputUser m_InputUser;           // The Input User component for that tanks. Contains the Input Actions. 
        
        public float CurrentChargeRatio =>
            (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce); //The charging amount between 0-1
        public bool IsCharging => m_IsCharging;

        public bool m_IsComputerControlled { get; set; } = false;
        
        private TankWormholeTravel m_WormholeTravel; // ワープ処理スクリプトへの参照

        private string m_FireButton;                // The input axis that is used for launching shells.
        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.
        private bool m_HasSpecialShell;             // has the tank a shell that makes extra damage?
        private float m_SpecialShellMultiplier;     // The amount that the special shell will multiply the damage.
        private InputAction fireAction;             // The Input Action for shooting, retrieve from TankInputUser
        private bool m_IsCharging = false;          // Are we currently charging the shot
        private float m_BaseMinLaunchForce;         // The initial value of m_MinLaunchForce
        private float m_ShotCooldownTimer;          // The timer counting down before a shot is allowed again

        // added for bullet controlling

        private bool m_ChargingForward=true;

        // --- Mine（地雷）管理用 ---
        [SerializeField] public WeaponStockData m_ShellStockData;

        [SerializeField] public WeaponStockData m_MineStockData;  // 地雷の所持数を管理する ScriptableObject 等
        [SerializeField] private GameObject m_Mine;                // 地雷プレハブ

        private string m_SetMineButton; // 地雷設置用のキー名（Input Manager 使用時）

        public event Action<WeaponStockData> OnWeaponStockChanged;     // 地雷の所持数が変化した時のイベント
        public event Action<Vector3> OnMinePlaced;         // 地雷が設置されたことを通知（座標などを渡す）

        private InputAction setMineAction; // 新 Input System 用のアクション
        
        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force, the UI and the power ups
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_BaseMinLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_BaseMinLaunchForce;
            m_HasSpecialShell = false;
            m_SpecialShellMultiplier = 1.0f;

            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
        }

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();

            m_WormholeTravel = GetComponent<TankWormholeTravel>();
            if (m_WormholeTravel == null)
            {
                Debug.LogWarning("TankWormholeTravel component not found on this tank.", this);
            }
        }

        private void Start ()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire";
            fireAction = m_InputUser.ActionAsset.FindAction(m_FireButton);
            
            fireAction.Enable();

            m_SetMineButton = "SetMine";
            setMineAction = m_InputUser.ActionAsset.FindAction(m_SetMineButton);
            
            setMineAction.Enable();

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

            m_ShellStockData.InitializeQuantity();
            m_MineStockData.InitializeQuantity();

            OnWeaponStockChanged?.Invoke(m_ShellStockData);
            OnWeaponStockChanged?.Invoke(m_MineStockData);
        }


        private void Update ()
        {
            // ワームホール移動中なら、Update処理全体をスキップ
            if (m_WormholeTravel != null && m_WormholeTravel.IsTraveling)
            {
                // (オプション) もしチャージ中だったらキャンセルする
                if (m_IsCharging)
                {
                    m_IsCharging = false;
                    m_CurrentLaunchForce = m_MinLaunchForce;
                    m_AimSlider.value = m_BaseMinLaunchForce; // スライダーもリセット
                    m_ShootingAudio.Stop(); // チャージ音停止
                }
                return;
            }

            // Computer and Human control Tank use 2 different update functions 
            if (!m_IsComputerControlled)
            {
                HumanUpdate();
            }
            else
            {
                ComputerUpdate();
            }
        }

        /// <summary>
        /// Used by AI to start charging
        /// </summary>
        public void StartCharging()
        {
            if (m_WormholeTravel != null && m_WormholeTravel.IsTraveling) return;
            
            m_IsCharging = true;
            // ... reset the fired flag and reset the launch force.
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            // Change the clip to the charging clip and start it playing.
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play ();
        }

        public void StopCharging()
        {
            // Fire()の中でチェックされるので必須ではないが、念のため追加
            if (m_WormholeTravel != null && m_WormholeTravel.IsTraveling) return;

            if (m_IsCharging)
            {
                Fire();
                m_IsCharging = false;
            }
        }

        void ComputerUpdate()
        {
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_BaseMinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            //added
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired && m_ShellStockData.GetCurrentQuantity()>0)
            {
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire ();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (m_IsCharging && !m_Fired)
            {
                // Increment the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            //added
            else if (fireAction.WasReleasedThisFrame() && !m_Fired && m_ShellStockData.GetCurrentQuantity()>0)
            {
                // ... launch the shell.
                Fire ();
                m_IsCharging = false;
            }
        }
        
        void HumanUpdate()
        {
            // if there is a cooldown timer, decrement it
            if (m_ShotCooldownTimer > 0.0f)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
            }
            
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_BaseMinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce > m_MaxLaunchForce && !m_Fired)
            {
                m_ChargingForward=false;
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                //Fire ();
            }
            // Otherwise, if the fire button has just started being pressed...
            // added 
            else if (m_ShotCooldownTimer <= 0 && fireAction.WasPressedThisFrame() && m_ShellStockData.GetCurrentQuantity()>0)
            {

                //Debug.Log("pressed");
                // ... reset the fired flag and reset the launch force.
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;

                // Change the clip to the charging clip and start it playing.
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play ();
            }
            else if (m_CurrentLaunchForce < m_MinLaunchForce && !m_Fired)
            {
                m_ChargingForward=true;
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MinLaunchForce;
                //Fire ();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (fireAction.IsPressed() && !m_Fired)
            {
                if(m_ChargingForward){
                // Increment the launch force and update the slider.
                    m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                }else
                {
                    m_CurrentLaunchForce -= m_ChargeSpeed * Time.deltaTime;
                }
                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                // ... launch the shell.
                Fire ();
            }


            if (setMineAction.WasPressedThisFrame())
            {
                Debug.Log("pressed");
                PlaceMine();
            }
        }


        private void Fire ()
        {
            // ワームホール移動中なら発射処理をスキップ
            if (m_WormholeTravel != null && m_WormholeTravel.IsTraveling)
            {
                // (オプション) チャージ状態などをリセット
                m_Fired = true; // 発射されたことにする (再発射を防ぐため)
                m_CurrentLaunchForce = m_MinLaunchForce;
                return;
            }

            // Set the fired flag so only Fire is only called once.
            m_Fired = true;

            //decrease amount of bullet
            m_ShellStockData.Use();

            //Debug.Log(m_ShellStockData.GetCurrentQuantity());
            OnWeaponStockChanged?.Invoke(m_ShellStockData);

            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance =
                Instantiate (m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            ShellExplosion explosionData = shellInstance.GetComponent<ShellExplosion>();
            explosionData.m_ExplosionForce = m_ExplosionForce;
            explosionData.m_ExplosionRadius = m_ExplosionRadius;
            explosionData.m_MaxDamage = m_MaxDamage;
            
            // Increase the damage if extra damage PowerUp is active
            if (m_HasSpecialShell)
            {
                explosionData.m_MaxDamage *= m_SpecialShellMultiplier;
                // Reset the default values after increasing the damage of the fired shell
                m_HasSpecialShell = false;
                m_SpecialShellMultiplier = 1f;
                
                PowerUpDetector powerUpDetector = GetComponent<PowerUpDetector>();
                if (powerUpDetector != null)
                    powerUpDetector.m_HasActivePowerUp = false;

                PowerUpHUD powerUpHUD = GetComponentInChildren<PowerUpHUD>();
                if (powerUpHUD != null)
                    powerUpHUD.DisableActiveHUD();
            }

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play ();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;

            m_ShotCooldownTimer = m_ShotCooldown;
        }

        private void PlaceMine()
        {
            if (m_MineStockData.GetCurrentQuantity() > 0){

            // 実際に地雷を設置
            Instantiate(m_Mine, transform.position- transform.forward * 2, transform.rotation);

            // 所持地雷を減らす
            m_MineStockData.Use();

            // イベント通知（UI などが更新）
            OnWeaponStockChanged?.Invoke(m_MineStockData);

            // 地雷を置いたことを通知（位置を渡せる）
            OnMinePlaced?.Invoke(transform.position);
            }
        }


        public void EquipSpecialShell(float damageMultiplier)
        {
            m_HasSpecialShell = true;
            m_SpecialShellMultiplier = damageMultiplier;
        }

        void OnCollisionEnter(Collision collision)
        {
            // 衝突した相手が ShellCartridge というタグを持っている場合
            if (collision.gameObject.CompareTag("ShellCartridge"))
            {
                m_ShellStockData.Replenish();
                OnWeaponStockChanged?.Invoke(m_ShellStockData);
                Destroy(collision.gameObject);
            }
            if (collision.gameObject.CompareTag("MineCartridge"))
            {
                m_MineStockData.Replenish();
                OnWeaponStockChanged?.Invoke(m_MineStockData);
                Destroy(collision.gameObject);
            }
        }


        /// <summary>
        /// Return the estyimated position the projectile will have with the charging level (between 0 & 1)
        /// </summary>
        /// <param name="chargingLevel">The fire charging level between 0 - 1</param>
        /// <returns>The position at which the projectile will be (ignore obstacle)</returns>
        public Vector3 GetProjectilePosition(float chargingLevel)
        {
            float chargeLevel = Mathf.Lerp (m_MinLaunchForce, m_MaxLaunchForce, chargingLevel);
            Vector3 velocity = chargeLevel * m_FireTransform.forward; 
            
            float a = 0.5f * Physics.gravity.y;
            float b = velocity.y;
            float c = m_FireTransform.position.y;
            
            float sqrtContent = b * b - 4 * a * c;
            //no solution
            if (sqrtContent <= 0)
            {
                return m_FireTransform.position;
            }

            float answer1 = (-b + Mathf.Sqrt(sqrtContent)) / (2 * a);
            float answer2 = (-b - Mathf.Sqrt(sqrtContent)) / (2 * a);

            float answer = answer1 > 0 ? answer1 : answer2;
            
            Vector3 position = m_FireTransform.position +
                               new Vector3(velocity.x, 0, velocity.z) *
                               answer;
            position.y = 0;

            return position;
        }
    }
}