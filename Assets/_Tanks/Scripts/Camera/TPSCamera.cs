using UnityEngine;

namespace Tanks.Complete
{
    public class TPSCamera : MonoBehaviour
    {
        public enum Axis { XPlus, XMinus, YPlus, YMinus, ZPlus, ZMinus }

        [Header("Target (砲塔 or 砲塔子の CameraTarget)")]
        public Transform target;

        [Header("Which local axes mean Forward / Up?")]
        public Axis forwardAxis = Axis.ZPlus; // 砲塔の“前”ローカル軸
        public Axis upAxis      = Axis.YPlus; // 砲塔の“上”ローカル軸（Heavy等でZ+が上ならZPlusに）

        [Header("Composition")]
        public float distance = 6.0f;
        public float height   = 2.5f;
        public float pitchDeg = 15.0f;

        [Header("Smoothing")]
        public float yawSmoothTime   = 0.12f;
        public float posFollowSpeed  = 12f;
        public float lookFollowSpeed = 14f;
        public float maxYawSpeedDeg  = 360f;

        [Header("Collision (遮蔽物対策)")]
        public LayerMask collideMask = ~0;
        public float camRadius       = 0.25f;
        public float hitPullback     = 0.15f;
        public float blockSmoothTime = 0.08f;

        [Header("Auto bind (spawn support)")]
        public bool   autoBindOnStart = true;     // 再生時に自動で target を探す
        public string targetTag       = "CameraTarget"; // これが付いたオブジェクトを最優先
        public string targetName      = "CameraTarget"; // 名前検索のフォールバック
        public string playerTankTag   = "Player";       // 自機タンクがあれば、その子からも探す

        float _yaw;
        float _yawVel;
        float _blockedDistVel;
        float _currentBlockedDist;
        Vector3 _lookAt;

        static Vector3 AxisToVector(Axis a)
        {
            switch (a)
            {
                case Axis.XPlus:  return Vector3.right;
                case Axis.XMinus: return Vector3.left;
                case Axis.YPlus:  return Vector3.up;
                case Axis.YMinus: return Vector3.down;
                case Axis.ZPlus:  return Vector3.forward;
                case Axis.ZMinus: return Vector3.back;
            }
            return Vector3.forward;
        }

        // target のローカル軸をワールド方向に変換
        static Vector3 Dir(Transform t, Axis a) => t.TransformDirection(AxisToVector(a));

        void Start()
        {
            if (!autoBindOnStart || target) return;

            // ① Tag で CameraTarget を最優先で探す
            if (!string.IsNullOrEmpty(targetTag))
            {
                var tagged = GameObject.FindGameObjectWithTag(targetTag);
                if (tagged) { target = tagged.transform; return; }
            }

            // ② 名前で検索（最初に見つかったもの）
            if (!target && !string.IsNullOrEmpty(targetName))
            {
                var named = GameObject.Find(targetName);
                if (named) { target = named.transform; return; }
            }

            // ③ Player タグが付いた自機タンクがあれば、その子から探す
            if (!target && !string.IsNullOrEmpty(playerTankTag))
            {
                var player = GameObject.FindGameObjectWithTag(playerTankTag);
                if (player)
                {
                    var ct = player.transform.Find(targetName);
                    if (ct) { target = ct; return; }
                }
            }

            // ④ 砲塔コンポ（TankAiming）から辿るフォールバック
            if (!target)
            {
                var aiming = FindObjectOfType<Tanks.Complete.TankAiming>();
                if (aiming)
                {
                    var ct = aiming.transform.Find(targetName);
                    target = ct ? ct : aiming.transform; // 見つからなければ砲塔そのもの
                }
            }
        }

        void LateUpdate()
        {
            if (!target) return;

            // --- 砲塔の“前/上”をローカル軸指定で取得 ---
            Vector3 fwd = Dir(target, forwardAxis);
            Vector3 up  = Dir(target, upAxis);

            // 水平面へ投影して yaw を算出（ワールドUpはY固定）
            Vector3 f = fwd; f.y = 0f;
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward; // 万一の保険
            f.Normalize();
            float desiredYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

            // 角度 SmoothDamp（最大角速度制限あり）
            _yaw = Mathf.SmoothDampAngle(_yaw, desiredYaw, ref _yawVel, yawSmoothTime, maxYawSpeedDeg, Time.deltaTime);

            // 一定ピッチで“後ろ上”へオフセット
            Quaternion yawRot   = Quaternion.Euler(0f, _yaw, 0f);
            Quaternion pitchRot = Quaternion.Euler(pitchDeg, 0f, 0f);
            Vector3 back = pitchRot * Vector3.back;

            Vector3 eyeFrom = target.position + Vector3.up * height; // レイの始点
            Vector3 desiredPos = target.position + yawRot * (back * distance) + Vector3.up * height;

            // 遮蔽物：スフィアキャストで距離をクリップ
            float clearDist = distance;
            Vector3 toDir = desiredPos - eyeFrom;
            float toLen = toDir.magnitude;
            if (toLen > 1e-4f)
            {
                toDir /= toLen;
                if (Physics.SphereCast(eyeFrom, camRadius, toDir, out var hit, toLen, collideMask, QueryTriggerInteraction.Ignore))
                {
                    clearDist = Mathf.Max(0.1f, hit.distance - hitPullback);
                }
            }

            // 遮蔽物距離をスムーズ化
            if (_currentBlockedDist <= 0f) _currentBlockedDist = distance;
            _currentBlockedDist = Mathf.SmoothDamp(_currentBlockedDist, clearDist, ref _blockedDistVel, blockSmoothTime);

            // 反映したカメラ位置
            Vector3 blockedPos = target.position + yawRot * (back * _currentBlockedDist) + Vector3.up * height;

            // 位置の指数スムージング
            float posT = 1f - Mathf.Exp(-posFollowSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, blockedPos, posT);

            // 注視点（砲塔の前）もスムーズに
            Vector3 desiredLook = target.position + fwd * 1.5f;
            float lookT = 1f - Mathf.Exp(-lookFollowSpeed * Time.deltaTime);
            _lookAt = Vector3.Lerp(_lookAt == Vector3.zero ? desiredLook : _lookAt, desiredLook, lookT);

            // 回転
            transform.rotation = Quaternion.LookRotation((_lookAt - transform.position).normalized, Vector3.up);
        }
    }
}
