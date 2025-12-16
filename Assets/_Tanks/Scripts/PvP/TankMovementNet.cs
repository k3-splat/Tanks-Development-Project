using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    //Ensure it run before the TankShooting component as TankShooting grabs the InputUser from this when there are no
    //GameManager set (used during learning experience to test tank in empty scenes)
    [DefaultExecutionOrder(-10)]
    public class TankMovementNet : MonoBehaviour
    {
        PhotonView _pv;

        [Tooltip("The player number. Without a tank selection menu, Player 1 is left keyboard control, Player 2 is right keyboard")]
        public int m_PlayerNumber = 1;
        [Tooltip("The speed in unity unit/second the tank move at")]
        public float m_Speed = 12f;
        [Tooltip("The speed in deg/s that tank will rotate at")]
        public float m_TurnSpeed = 180f;
        [Tooltip("If set to true, the tank auto orient and move toward the pressed direction instead of rotating on left/right and move forward on up")]
        public bool m_IsDirectControl;

        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;

        [Tooltip("Is set to true this will be controlled by the computer and not a player")]
        public bool m_IsComputerControlled = false;

        [HideInInspector] public TankInputUser m_InputUser;

        public Rigidbody Rigidbody => m_Rigidbody;
        public int ControlIndex { get; set; } = -1;

        private string m_MovementAxisName;
        private string m_TurnAxisName;
        private Rigidbody m_Rigidbody;
        private float m_MovementInputValue;
        private float m_TurnInputValue;
        private Vector3 m_ExplosionForceValue;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;

        private InputAction m_MoveAction;
        private InputAction m_TurnAction;

        private Vector3 m_RequestedDirection;

        private bool IsLocal => (_pv == null) || _pv.IsMine;

        private void Awake()
        {
            _pv = GetComponent<PhotonView>();

            m_Rigidbody = GetComponent<Rigidbody>();

            // ✅ Inputは「自分の戦車だけ」生成/設定する
            if (IsLocal)
            {
                m_InputUser = GetComponent<TankInputUser>();
                if (m_InputUser == null)
                    m_InputUser = gameObject.AddComponent<TankInputUser>();
            }
        }

        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;

            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            m_ExplosionForceValue = Vector3.zero;

            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
                m_particleSystems[i].Play();
        }

        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;

            for (int i = 0; i < m_particleSystems.Length; ++i)
                m_particleSystems[i].Stop();
        }

        private void Start()
        {
            // ✅ 他人の戦車は「入力セットアップ」を絶対しない
            if (!IsLocal) return;

            if (m_IsComputerControlled)
            {
                var ai = GetComponent<TankAI>();
                if (ai == null) gameObject.AddComponent<TankAI>();
            }

            if (ControlIndex == -1 && !m_IsComputerControlled)
                ControlIndex = m_PlayerNumber;

            var mobileControl = FindAnyObjectByType<MobileUIControl>();

            if (mobileControl != null && ControlIndex == 1)
            {
                m_InputUser.SetNewInputUser(InputUser.PerformPairingWithDevice(mobileControl.Device));
                m_InputUser.ActivateScheme("Gamepad");
            }
            else
            {
                m_InputUser.ActivateScheme(ControlIndex == 1 ? "KeyboardLeft" : "KeyboardRight");
            }

            m_MovementAxisName = "Vertical";
            m_TurnAxisName = "Horizontal";

            m_MoveAction = m_InputUser.ActionAsset.FindAction(m_MovementAxisName);
            m_TurnAction = m_InputUser.ActionAsset.FindAction(m_TurnAxisName);

            m_MoveAction.Enable();
            m_TurnAction.Enable();

            if (m_MovementAudio)
                m_OriginalPitch = m_MovementAudio.pitch;
        }

        private void Update()
        {
            // ✅ 他人の戦車は入力を読まない（Audioは鳴らしても良いけど、今は安全に全止め）
            if (!IsLocal) return;

            if (!m_IsComputerControlled)
            {
                m_MovementInputValue = m_MoveAction.ReadValue<float>();
                m_TurnInputValue = m_TurnAction.ReadValue<float>();
            }

            if (m_MovementAudio)
                EngineAudio();
        }

        private void EngineAudio()
        {
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        private void FixedUpdate()
        {
            // ✅ 他人の戦車は物理で動かさない（＝同期だけで動く）
            if (!IsLocal) return;

            if ((m_InputUser.InputUser.controlScheme.HasValue && m_InputUser.InputUser.controlScheme.Value.name == "Gamepad") || m_IsDirectControl)
            {
                var camForward = Camera.main.transform.forward;
                camForward.y = 0;

                if (camForward.sqrMagnitude < 0.0001f)
                {
                    camForward = Camera.main.transform.up;
                    camForward.y = 0;
                }

                camForward.Normalize();
                var camRight = Vector3.Cross(Vector3.up, camForward);

                m_RequestedDirection = (camForward * m_MovementInputValue + camRight * m_TurnInputValue);
                m_RequestedDirection.Normalize();
            }

            Move();
            Turn();
        }

        private void Move()
        {
            float speedInput = 0.0f;

            if ((m_InputUser.InputUser.controlScheme.HasValue && m_InputUser.InputUser.controlScheme.Value.name == "Gamepad") || m_IsDirectControl)
            {
                speedInput = m_RequestedDirection.magnitude;
                speedInput *= 1.0f - Mathf.Clamp01((Vector3.Angle(m_RequestedDirection, transform.forward) - 90) / 90.0f);
            }
            else
            {
                speedInput = m_MovementInputValue;
            }

            Vector3 movement = transform.forward * speedInput * m_Speed;

            m_Rigidbody.linearVelocity = movement + m_ExplosionForceValue;
            m_ExplosionForceValue = Vector3.Lerp(m_ExplosionForceValue, Vector3.zero, Time.deltaTime * 3f);
        }

        private void Turn()
        {
            Quaternion turnRotation;

            if ((m_InputUser.InputUser.controlScheme.HasValue && m_InputUser.InputUser.controlScheme.Value.name == "Gamepad") || m_IsDirectControl)
            {
                float angleTowardTarget = Vector3.SignedAngle(m_RequestedDirection, transform.forward, transform.up);
                var rotatingAngle = Mathf.Sign(angleTowardTarget) * Mathf.Min(Mathf.Abs(angleTowardTarget), m_TurnSpeed * Time.deltaTime);
                turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
            }
            else
            {
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
                turnRotation = Quaternion.Euler(0f, turn, 0f);
            }

            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier = 0f)
        {
            Vector3 explosionDir = transform.position - explosionPosition;
            float explosionDistance = explosionDir.magnitude;

            if (upwardsModifier != 0)
            {
                explosionDir.y += upwardsModifier;
                explosionDir.Normalize();
            }
            else
            {
                explosionDir = explosionDir.normalized;
            }

            float attenuation = 1f - Mathf.Clamp01(explosionDistance / explosionRadius);
            Vector3 velocityChange = explosionDir * (explosionForce * attenuation);

            m_ExplosionForceValue = velocityChange;
        }
    }
}
