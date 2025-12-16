using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankShootingNet : MonoBehaviourPun
    {
        [Header("Photon Prefab Name (PhotonNetwork.Instantiate)")]
        [SerializeField] private string shellPrefabName = "Shell_Net"; // ★PhotonのPrefab登録名

        [Header("Refs")]
        [SerializeField] private Transform fireTransform;
        [SerializeField] private Slider aimSlider;
        [SerializeField] private AudioSource shootingAudio;
        [SerializeField] private AudioClip chargingClip;
        [SerializeField] private AudioClip fireClip;

        [Header("Shot")]
        [SerializeField] private float minLaunchForce = 5f;
        [SerializeField] private float maxLaunchForce = 20f;
        [SerializeField] private float maxChargeTime  = 0.75f;
        [SerializeField] private float shotCooldown   = 1.0f;

        [Header("Explosion Params (sent to shell)")]
        [SerializeField] private float maxDamage      = 100f;
        [SerializeField] private float explosionForce = 50f;
        [SerializeField] private float explosionRadius= 5f;
        [SerializeField] private float shellLifeTime  = 2.0f;

        // Input
        private TankInputUserNet _inputUser;
        private InputAction _fireAction;

        private float _chargeSpeed;
        private float _currentLaunchForce;
        private bool _charging;
        private float _cooldownTimer;

        private bool IsLocal => photonView.IsMine;

        private void Awake()
        {
            // ローカルだけInputを持つ
            if (!IsLocal) return;

            _inputUser = GetComponent<TankInputUserNet>();
            if (_inputUser == null) _inputUser = gameObject.AddComponent<TankInputUserNet>();
        }

        private void Start()
        {
            if (!IsLocal) return;

            _chargeSpeed = (maxLaunchForce - minLaunchForce) / maxChargeTime;
            _currentLaunchForce = minLaunchForce;

            // Action名は君のプロジェクトと同じ "Fire" 前提
            _fireAction = _inputUser.ActionAsset.FindAction("Fire");
            _fireAction.Enable();

            if (aimSlider != null)
            {
                aimSlider.minValue = minLaunchForce;
                aimSlider.maxValue = maxLaunchForce;
                aimSlider.value = minLaunchForce;
            }
        }

        private void Update()
        {
            if (!IsLocal) return;

            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            // UI更新
            if (aimSlider != null) aimSlider.value = _currentLaunchForce;

            // 1) 押し始め：チャージ開始
            if (_cooldownTimer <= 0f && _fireAction.WasPressedThisFrame())
            {
                _charging = true;
                _currentLaunchForce = minLaunchForce;

                if (shootingAudio != null && chargingClip != null)
                {
                    shootingAudio.clip = chargingClip;
                    shootingAudio.Play();
                }
            }

            // 2) 押しっぱ：チャージ増加
            if (_charging && _fireAction.IsPressed())
            {
                _currentLaunchForce += _chargeSpeed * Time.deltaTime;
                if (_currentLaunchForce >= maxLaunchForce)
                    _currentLaunchForce = maxLaunchForce;
            }

            // 3) 離した：発射
            if (_charging && _fireAction.WasReleasedThisFrame())
            {
                FireNet(_currentLaunchForce);
                _charging = false;
                _currentLaunchForce = minLaunchForce;
            }
        }

        private void FireNet(float launchForce)
        {
            if (fireTransform == null) return;

            // 弾の初期条件を InstantiateData で渡す（全員同じ値を受け取れる）
            object[] data = new object[]
            {
                launchForce,
                explosionForce,
                explosionRadius,
                maxDamage,
                shellLifeTime
            };

            PhotonNetwork.Instantiate(
                shellPrefabName,
                fireTransform.position,
                fireTransform.rotation,
                0,
                data
            );

            if (shootingAudio != null && fireClip != null)
            {
                shootingAudio.Stop();
                shootingAudio.clip = fireClip;
                shootingAudio.Play();
            }

            _cooldownTimer = shotCooldown;
        }
    }
}
