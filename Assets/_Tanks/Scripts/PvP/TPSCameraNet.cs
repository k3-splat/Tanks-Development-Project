using UnityEngine;
using Photon.Pun;

namespace Tanks.Complete
{
    /// <summary>
    /// PUN向けTPSカメラ。
    /// ローカルプレイヤー（PhotonView.IsMine）の CameraTarget に追従する。
    /// </summary>
    public class TPSCameraNet : MonoBehaviour
    {
        public enum Axis { XPlus, XMinus, YPlus, YMinus, ZPlus, ZMinus }

        [Header("Target (optional)")]
        [Tooltip("手動で指定する場合はここに入れる。空なら自動でローカルの CameraTarget を探す。")]
        public Transform target;

        [Header("Which local axes mean Forward / Up?")]
        public Axis forwardAxis = Axis.ZPlus;
        public Axis upAxis      = Axis.YPlus;

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

        [Header("Auto bind (PUN)")]
        public bool autoBind = true;

        [Tooltip("CameraTarget がこのTagを持つなら最優先で探す（複数ある前提で IsMine の親を選ぶ）")]
        public string cameraTargetTag = "CameraTarget";

        [Tooltip("Tag が使えない場合のフォールバック：子の名前（再帰検索）")]
        public string cameraTargetName = "CameraTarget";

        [Tooltip("さらにフォールバック：プレイヤーrootのTag（その親に IsMine が居るものを探す）")]
        public string playerTag = "Player";

        [Tooltip("targetが見つからない/破棄された時に再探索する間隔(秒)")]
        public float rebindInterval = 0.5f;

        [Header("Debug")]
        public bool verboseLog = false;

        float _yaw;
        float _yawVel;
        float _blockedDistVel;
        float _currentBlockedDist;
        Vector3 _lookAt;
        float _nextRebindTime;
        bool _initializedFromTarget;

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

        static Vector3 Dir(Transform t, Axis a) => t.TransformDirection(AxisToVector(a));

        void Start()
        {
            if (autoBind && !target)
                TryAutoBindLocalTarget(forceLog: true);
        }

        void LateUpdate()
        {
            if (!target)
            {
                if (autoBind && Time.time >= _nextRebindTime)
                {
                    _nextRebindTime = Time.time + rebindInterval;
                    TryAutoBindLocalTarget(forceLog: false);
                }
                return;
            }

            // --- 砲塔の“前/上”をローカル軸指定で取得 ---
            Vector3 fwd = Dir(target, forwardAxis);
            Vector3 up  = Dir(target, upAxis); // 現状は回転のUpに使ってないが、必要なら拡張可

            // 水平面へ投影して yaw を算出（ワールドUpはY固定）
            Vector3 f = fwd; f.y = 0f;
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
            f.Normalize();
            float desiredYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

            // 初回だけ yaw を即合わせ（カメラが変な方向で固まって見えるのを防ぐ）
            if (!_initializedFromTarget)
            {
                _yaw = desiredYaw;
                _yawVel = 0f;
                _initializedFromTarget = true;
                if (verboseLog) Debug.Log("[TPSCameraNet] Initialized yaw from target.");
            }
            else
            {
                _yaw = Mathf.SmoothDampAngle(_yaw, desiredYaw, ref _yawVel, yawSmoothTime, maxYawSpeedDeg, Time.deltaTime);
            }

            // 一定ピッチで“後ろ上”へオフセット
            Quaternion yawRot   = Quaternion.Euler(0f, _yaw, 0f);
            Quaternion pitchRot = Quaternion.Euler(pitchDeg, 0f, 0f);
            Vector3 back = pitchRot * Vector3.back;

            Vector3 eyeFrom    = target.position + Vector3.up * height;
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
            Vector3 lookDir = (_lookAt - transform.position);
            if (lookDir.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }

        /// <summary>
        /// 外から明示的にセットしたい時用（推奨）
        /// </summary>
        public void SetTarget(Transform t)
        {
            target = t;
            _initializedFromTarget = false; // 次フレームでyaw初期化
            _lookAt = Vector3.zero;
            if (verboseLog) Debug.Log("[TPSCameraNet] Target set explicitly.");
        }

        void TryAutoBindLocalTarget(bool forceLog)
        {
            // 1) CameraTargetTag を持つものを全部見て、IsMine の親を持つものを採用
            if (!string.IsNullOrEmpty(cameraTargetTag))
            {
                try
                {
                    var candidates = GameObject.FindGameObjectsWithTag(cameraTargetTag);
                    foreach (var go in candidates)
                    {
                        var pv = go.GetComponentInParent<PhotonView>();
                        if (pv && pv.IsMine)
                        {
                            Bind(go.transform, "tag(CameraTargetTag)");
                            return;
                        }
                    }
                }
                catch
                {
                    // Tag未定義だと例外になるので握りつぶす（Unity仕様）
                }
            }

            // 2) playerTag の root を見て IsMine のやつの子に CameraTargetName があればそれ
            if (!string.IsNullOrEmpty(playerTag))
            {
                try
                {
                    var players = GameObject.FindGameObjectsWithTag(playerTag);
                    foreach (var p in players)
                    {
                        var pv = p.GetComponentInParent<PhotonView>();
                        if (pv && pv.IsMine)
                        {
                            var ct = FindDeepChild(p.transform, cameraTargetName);
                            Bind(ct ? ct : p.transform, "playerTag");
                            return;
                        }
                    }
                }
                catch { }
            }

            // 3) 最後：全PhotonViewから IsMine を探し、その子に CameraTargetName があれば採用
            var views = FindObjectsOfType<PhotonView>();
            foreach (var pv in views)
            {
                if (!pv.IsMine) continue;
                var ct = FindDeepChild(pv.transform, cameraTargetName);
                Bind(ct ? ct : pv.transform, "PhotonView(IsMine)");
                return;
            }

            if (verboseLog || forceLog)
                Debug.LogWarning("[TPSCameraNet] Local target not found yet. Will retry.");
        }

        void Bind(Transform t, string via)
        {
            if (!t) return;
            if (target == t) return;

            target = t;
            _initializedFromTarget = false;
            _lookAt = Vector3.zero;

            if (verboseLog) Debug.Log($"[TPSCameraNet] Bound target via {via}: {t.name}");
        }

        static Transform FindDeepChild(Transform root, string childName)
        {
            if (!root || string.IsNullOrEmpty(childName)) return null;

            // 自分が一致
            if (root.name == childName) return root;

            // 子を再帰
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == childName) return c;

                var deep = FindDeepChild(c, childName);
                if (deep) return deep;
            }
            return null;
        }
    }
}
