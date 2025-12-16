// ファイル名: TankAimingNet.cs
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Tanks.Complete
{
    // 砲塔(Turret)にアタッチ
    // PhotonView は親（戦車ルート）に付いている想定
    public class TankAimingNet : MonoBehaviour, IPunObservable
    {
        [Tooltip("砲塔の回転速度（度/秒）")]
        public float m_TurnSpeed = 180f;

        [Tooltip("戦車のタイプ")]
        public string m_TankType = "Medium"; // "Medium" or "Heavy"

        [Tooltip("Aimアクションの名前")]
        public string m_AimActionName = "Aim";

        [Tooltip("砲塔と同期して回転するSliderのTransform（UIなど）")]
        public Transform m_TurretSliderTransform;

        [Header("Remote Smooth")]
        public float m_RemoteFollowSpeed = 16f;

        // ★親のPhotonViewを明示的に使う
        private PhotonView _pv;

        private TankInputUserNet m_InputUserNet;
        private InputAction m_AimAction;

        private float m_AimInputValue;
        private Quaternion m_NetTargetLocalRot;

        private void Awake()
        {
            // ★親のPhotonView
            _pv = GetComponentInParent<PhotonView>();
            if (_pv == null)
            {
                Debug.LogError("親オブジェクトに PhotonView が見つかりません。", this);
                enabled = false;
                return;
            }

            // Turretは子なので親から取得
            m_InputUserNet = GetComponentInParent<TankInputUserNet>();

            m_NetTargetLocalRot = transform.localRotation;

            // ★入力が必要なのはローカルだけ
            if (_pv.IsMine && m_InputUserNet == null)
            {
                Debug.LogError("親オブジェクトに TankInputUserNet が見つかりません。", this);
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            if (!_pv.IsMine) return;

            m_AimAction = m_InputUserNet.ActionAsset.FindAction(m_AimActionName);
            if (m_AimAction == null)
            {
                Debug.LogError($"Input Action '{m_AimActionName}' が見つかりません。Tank_Actions.inputactions を確認してください。", this);
                enabled = false;
                return;
            }

            m_AimAction.Enable();
        }

        private void Update()
        {
            if (_pv.IsMine)
            {
                if (m_AimAction == null) return;

                m_AimInputValue = m_AimAction.ReadValue<float>();
                TurnLocal(m_AimInputValue);

                // 送信する正解角
                m_NetTargetLocalRot = transform.localRotation;
            }
            else
            {
                // 受信角へ補間
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    m_NetTargetLocalRot,
                    1f - Mathf.Exp(-m_RemoteFollowSpeed * Time.deltaTime)
                );
            }

            UpdateSliderFromTurret();
        }

        private void TurnLocal(float aimInput)
        {
            float turn = aimInput * m_TurnSpeed * Time.deltaTime;

            if (m_TankType == "Medium")
                transform.Rotate(0f, turn, 0f, Space.Self);
            else if (m_TankType == "Heavy")
                transform.Rotate(0f, 0f, turn, Space.Self);
        }

        private void UpdateSliderFromTurret()
        {
            if (m_TurretSliderTransform == null) return;

            float turretAngle =
                (m_TankType == "Heavy")
                    ? transform.localEulerAngles.z
                    : transform.localEulerAngles.y;

            m_TurretSliderTransform.localRotation = Quaternion.Euler(0f, 0f, turretAngle);
        }

        private void OnEnable()
        {
            if (_pv != null && _pv.IsMine)
            {
                m_AimAction?.Enable();
                m_AimInputValue = 0f;
            }
        }

        private void OnDisable()
        {
            if (_pv != null && _pv.IsMine)
            {
                m_AimAction?.Disable();
            }
        }

        // ★親PhotonViewのObservedに入っていれば呼ばれる
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.localRotation);
            }
            else
            {
                m_NetTargetLocalRot = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}
