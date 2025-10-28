// ファイル名: TankAiming.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// TankMovement.csと同じ名前空間にしておくと管理が楽です
namespace Tanks.Complete
{
    // このスクリプトは「砲塔(Turret)」のオブジェクトにアタッチしてください
    public class TankAiming : MonoBehaviour
    {
        [Tooltip("砲塔の回転速度（度/秒）")]
        public float m_TurnSpeed = 180f;

        [Tooltip("戦車のタイプ")]
        public string m_TankType = "Medium";

        [Tooltip("（デバッグ用）Aimアクションの名前")]
        public string m_AimActionName = "Aim";

        [Tooltip("砲塔と同期して回転するSliderのTransform")]
        public Transform m_TurretSliderTransform;

        // 親オブジェクトが持つTankInputUserへの参照
        private TankInputUser m_InputUser;
        // "Aim"アクションへの参照
        private InputAction m_AimAction;
        
        // 現在のAim入力値 (-1.0f ～ +1.0f)
        private float m_AimInputValue;

        // TankMovement.cs (Awake) を参考に
        private void Awake()
        {
            // このスクリプトは子オブジェクト（砲塔）にあるため、
            // 親オブジェクトにあるTankInputUserを取得します
            m_InputUser = GetComponentInParent<TankInputUser>();

            if (m_InputUser == null)
            {
                Debug.LogError("親オブジェクトに TankInputUser が見つかりません。", this);
                enabled = false; // スクリプトを無効化
            }
        }

        // TankMovement.cs (Start) を参考に
        private void Start()
        {
            if (m_InputUser == null) return;

            // Input Action Assetから "Aim" アクションを探します
            m_AimAction = m_InputUser.ActionAsset.FindAction(m_AimActionName);

            if (m_AimAction == null)
            {
                Debug.LogError($"Input Action '{m_AimActionName}' が見つかりません。Tank_Actions.inputactionsを確認してください。", this);
                enabled = false;
                return;
            }

            // アクションを有効化します
            m_AimAction.Enable();
        }

        // TankMovement.cs (Update) を参考に
        private void Update()
        {
            if (m_AimAction == null) return;

            // "Aim" アクションから入力値を読み取ります
            m_AimInputValue = m_AimAction.ReadValue<float>();

            // 読み取った値で回転処理を呼び出します
            Turn();
        }

        // TankMovement.cs (Turn) を参考にしつつ、Rigidbodyを使わない実装
        private void Turn()
        {
            // 入力値と速度、フレーム時間から、このフレームでの回転角度を計算します
            // m_AimInputValue は -1 (左) から +1 (右) の値です
            float turn = m_AimInputValue * m_TurnSpeed * Time.deltaTime;

            // 砲塔自身のY軸（ローカル座標の上方向）を軸にして回転させます
            // (Rigidbody.MoveRotation ではなく transform.Rotate を使います)
            if (m_TankType == "Medium") transform.Rotate(0f, turn, 0f, Space.Self);
            else if (m_TankType == "Heavy") transform.Rotate(0f, 0f, turn, Space.Self);

            if (m_TurretSliderTransform != null)
            {
                m_TurretSliderTransform.Rotate(0f, 0f, turn, Space.Self);
            }
        }

        // TankMovement.cs (OnEnable/OnDisable) を参考に
        private void OnEnable()
        {
            // m_AimActionが初期化済みなら有効化する
            m_AimAction?.Enable();
            m_AimInputValue = 0f;
        }

        private void OnDisable()
        {
            // このスクリプトが無効になったら入力受付も停止する
            m_AimAction?.Disable();
        }
    }
}