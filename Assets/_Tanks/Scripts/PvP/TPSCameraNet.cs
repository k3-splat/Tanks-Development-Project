using UnityEngine;
using Photon.Pun;

namespace Tanks.Complete
{
    public class TPSCameraNet : MonoBehaviour
    {
        public enum Axis { XPlus, XMinus, YPlus, YMinus, ZPlus, ZMinus }

        [Header("Target (local player)")]
        public Transform target;                 // CameraTarget 推奨
        public string targetName = "CameraTarget";

        [Header("Axes (モデル/CameraTargetの向きが怪しい時にここで合わせる)")]
        public Axis forwardAxis = Axis.ZPlus;    // “前”にしたいローカル軸
        public Axis upAxis      = Axis.YPlus;    // “上”にしたいローカル軸（通常Y+）

        [Header("Orbit (角度で位置を作る = 上から30度を保証しやすい)")]
        public float distance = 6.0f;            // 半径（離れ具合）
        [Range(0f, 75f)]
        public float pitchDeg = 30.0f;           // ★上から30度 → 30
        public float extraHeight = 0.0f;         // ★角度を崩したくないなら 0 推奨
        public float lookAhead = 1.8f;           // 砲塔前方を見る量

        [Header("Smoothing")]
        public float yawSmoothTime   = 0.12f;    // 左右追従の滑らかさ
        public float posFollowSpeed  = 12f;      // 位置指数スムージング
        public float rotFollowSpeed  = 14f;      // 回転指数スムージング
        public float maxYawSpeedDeg  = 360f;

        [Header("Collision (遮蔽物対策)")]
        public LayerMask collideMask = ~0;
        public float camRadius       = 0.25f;
        public float hitPullback     = 0.15f;
        public float blockSmoothTime = 0.08f;
        public float minDistance     = 0.6f;     // 近づきすぎ防止

        [Header("Auto bind (local player)")]
        public bool autoBindOnStart = true;

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

        static Vector3 Dir(Transform t, Axis a) => t.TransformDirection(AxisToVector(a));

        void Start()
        {
            if (autoBindOnStart && target == null)
                TryBindLocalPlayerTarget();
        }

        void LateUpdate()
        {
            if (target == null)
            {
                if (autoBindOnStart) TryBindLocalPlayerTarget();
                return;
            }

            // --- forward / up を軸指定で取得 ---
            Vector3 fwd = Dir(target, forwardAxis);
            Vector3 up  = Dir(target, upAxis);

            // yaw（左右）を “ワールドY” 基準で安定させる（fwdをXZに投影して角度化）
            Vector3 f = fwd; f.y = 0f;
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
            f.Normalize();

            float desiredYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            _yaw = Mathf.SmoothDampAngle(_yaw, desiredYaw, ref _yawVel, yawSmoothTime, maxYawSpeedDeg, Time.deltaTime);

            Quaternion yawRot   = Quaternion.Euler(0f, _yaw, 0f);
            Quaternion pitchRot = Quaternion.Euler(pitchDeg, 0f, 0f);

            // ★pitchで「後ろ上」の単位ベクトルを作る（位置が角度になる）
            Vector3 backUp = pitchRot * Vector3.back; // pitchDeg=30 → 上に30度持ち上がる

            // 望ましいカメラ位置（遮蔽物前）
            Vector3 desiredPos =
                target.position
                + yawRot * (backUp * distance)
                + Vector3.up * extraHeight;

            // 遮蔽物チェック：target付近(eyeFrom)→desiredPos へ SphereCast
            Vector3 eyeFrom = target.position + Vector3.up * extraHeight;
            float clearDist = distance;

            Vector3 to = desiredPos - eyeFrom;
            float toLen = to.magnitude;

            if (toLen > 1e-4f)
            {
                Vector3 toDir = to / toLen; // ほぼ backUp 回転後と同じ向き

                if (Physics.SphereCast(eyeFrom, camRadius, toDir, out RaycastHit hit, toLen,
                        collideMask, QueryTriggerInteraction.Ignore))
                {
                    clearDist = Mathf.Max(minDistance, hit.distance - hitPullback);
                }
            }

            // 遮蔽物距離スムーズ化（距離だけ縮める＝角度は維持される）
            if (_currentBlockedDist <= 0f) _currentBlockedDist = distance;
            _currentBlockedDist = Mathf.SmoothDamp(_currentBlockedDist, clearDist, ref _blockedDistVel, blockSmoothTime);

            Vector3 blockedPos =
                target.position
                + yawRot * (backUp * _currentBlockedDist)
                + Vector3.up * extraHeight;

            // 位置スムージング（指数）
            float posT = 1f - Mathf.Exp(-posFollowSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, blockedPos, posT);

            // 注視点（砲塔前方）もスムーズに
            Vector3 desiredLook = target.position + fwd.normalized * lookAhead;
            float lookT = 1f - Mathf.Exp(-rotFollowSpeed * Time.deltaTime);
            _lookAt = Vector3.Lerp(_lookAt == Vector3.zero ? desiredLook : _lookAt, desiredLook, lookT);

            // 回転
            Quaternion lookRot = Quaternion.LookRotation((_lookAt - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, lookT);
        }

        void TryBindLocalPlayerTarget()
        {
            // ローカル所有の PhotonView を探して、その配下の CameraTarget を拾う
            var views = FindObjectsOfType<PhotonView>();
            foreach (var v in views)
            {
                if (!v.IsMine) continue;

                var found = FindChildRecursive(v.transform, targetName);
                if (found != null)
                {
                    target = found;
                    // 初期化：いきなりガクッとしないため
                    _currentBlockedDist = distance;
                    _lookAt = target.position;
                    return;
                }
            }
        }

        Transform FindChildRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindChildRecursive(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
