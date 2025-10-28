using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    /// 安定志向のTPSカメラ:
    /// ・Yawを角度でSmoothDamp(角速度制御)
    /// ・Pitchは一定角
    /// ・距離/高さ基準で位置追従（指数スムージング）
    /// ・遮蔽物補正をSmoothDampでなめらかに
    /// ・LateUpdateで実行（タンクの物理更新後）
    /// </summary>
    public class TPSCamera : MonoBehaviour
    {
        [Header("Target (砲塔の子の CameraTarget)")]
        public Transform target;

        [Header("Composition")]
        public float distance = 6.0f;      // 後方距離
        public float height   = 2.5f;      // 目線の高さ
        public float pitchDeg = 15.0f;     // 一定の仰角（見下ろし角）

        [Header("Smoothing")]
        public float yawSmoothTime   = 0.12f; // Yaw角のSmoothDamp時間
        public float posFollowSpeed  = 12f;   // 位置の指数スムージング
        public float lookFollowSpeed = 14f;   // LookAt点の指数スムージング
        public float maxYawSpeedDeg  = 360f;  // Yawの最大角速度（安全弁）

        [Header("Collision (遮蔽物対策)")]
        public LayerMask collideMask = ~0;  // 衝突対象（自機レイヤーは外すと良い）
        public float camRadius       = 0.25f;
        public float hitPullback     = 0.15f;  // 壁から少し手前に
        public float blockSmoothTime = 0.08f;  // 遮蔽物距離のスムーズ時間

        // 内部状態
        float _yaw;               // 現在Yaw（水平角）
        float _yawVel;            // SmoothDampの角速度
        float _blockedDistVel;    // 遮蔽距離用SmoothDamp
        float _currentBlockedDist;// 現在の遮蔽距離

        Vector3 _lookAt;          // スムーズ化した注視点

        void LateUpdate()
        {
            if (target == null) return;

            // --- Yaw（水平角）：砲塔のforwardから目標Yawを算出し、角度スムーズ ---
            Vector3 f = target.forward;
            f.y = 0f; // 水平投影
            if (f.sqrMagnitude < 1e-4f) f = target.parent ? target.parent.forward : Vector3.forward;
            float desiredYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg; // Z前提のYaw

            // 角度SmoothDamp（最大角速度も制限）
            float newYaw = Mathf.SmoothDampAngle(_yaw, desiredYaw, ref _yawVel, yawSmoothTime, maxYawSpeedDeg, Time.deltaTime);
            _yaw = newYaw;

            // --- 目標位置の算出（距離＆高さ） ---
            // 回転からオフセットを作る（一定Pitch）
            Quaternion yawRot   = Quaternion.Euler(0f, _yaw, 0f);
            Quaternion pitchRot = Quaternion.Euler(pitchDeg, 0f, 0f);
            Vector3 back = pitchRot * Vector3.back; // ピッチを反映した「後ろ」
            Vector3 desiredPos = target.position + yawRot * (back * distance) + Vector3.up * height;

            // --- 遮蔽物補正（スフィアキャスト）。距離をなめらかに補正 ---
            float clearDist = distance;
            Vector3 from = target.position + Vector3.up * height;
            Vector3 toDir = (desiredPos - from);
            float   toLen = toDir.magnitude;
            if (toLen > 0.0001f)
            {
                toDir /= toLen;
                if (Physics.SphereCast(from, camRadius, toDir, out var hit, toLen, collideMask, QueryTriggerInteraction.Ignore))
                {
                    clearDist = Mathf.Max(0.1f, hit.distance - hitPullback);
                }
            }
            // 遮蔽距離をスムーズに（ガクつき低減）
            _currentBlockedDist = Mathf.SmoothDamp(_currentBlockedDist <= 0f ? distance : _currentBlockedDist,
                                                   clearDist, ref _blockedDistVel, blockSmoothTime);

            // 遮蔽物結果で再計算
            Vector3 blockedPos = target.position + yawRot * (back * _currentBlockedDist) + Vector3.up * height;

            // --- 位置の指数スムージング ---
            transform.position = Vector3.Lerp(transform.position, blockedPos, 1f - Mathf.Exp(-posFollowSpeed * Time.deltaTime));

            // --- 注視点（ターゲット＋少し前）もスムーズ。砲塔先を見させると酔いにくい ---
            Vector3 desiredLook = target.position + target.forward * 1.5f;
            _lookAt = Vector3.Lerp(_lookAt == Vector3.zero ? desiredLook : _lookAt, desiredLook,
                                   1f - Mathf.Exp(-lookFollowSpeed * Time.deltaTime));

            transform.rotation = Quaternion.LookRotation((_lookAt - transform.position).normalized, Vector3.up);
        }
    }
}
