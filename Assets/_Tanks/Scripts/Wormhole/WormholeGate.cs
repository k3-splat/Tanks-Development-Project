using UnityEngine;

namespace Tanks.Complete // 名前空間はプロジェクトに合わせてください
{
    public class WormholeGate : MonoBehaviour
    {
        [Tooltip("このゲートの出口となる向かい側のゲート")]
        public WormholeGate oppositeGate;

        [Tooltip("ワームホール通過にかかる時間")]
        public float travelDuration = 1.5f;

        [Tooltip("ゲートから出てくる際の少し前方の位置オフセット")]
        public float exitOffset = 2.0f; // 戦車がゲート自体に埋まらないように

        // このゲートから出てくる位置と向きを計算するプロパティ
        public Transform ExitTransform
        {
            get
            {
                // 簡単な実装例: ゲートの向きの exitOffset 分だけ前に出す
                // より凝るなら、空のGameObjectを子に置いてそのTransformを返すなど
                transform.position = transform.position + transform.forward * exitOffset;
                return transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // --- 仕様: 砲弾や地雷は通過できない ---
            // Shellコンポーネントを持つオブジェクトなら、ここで破壊するなどして処理終了
            ShellExplosion shell = other.GetComponent<ShellExplosion>();
            if (shell != null)
            {
                 // 必要ならエフェクトなどを再生
                 Destroy(shell.gameObject);
                 return;
            }

            // Mineコンポーネントを持つオブジェクトも同様 (もしあれば)
            // if (other.GetComponent<Mine>() != null) { /* 処理 */ return; }


            // --- 戦車のワープ処理 ---
            // 触れたオブジェクトからTankControllerのような中心的なスクリプトを探す
            // (ここでは TankMovement, TankShooting, TankHealth を持つものを戦車と仮定)
            TankMovement tankMovement = other.GetComponent<TankMovement>();
            if (tankMovement != null && oppositeGate != null)
            {
                // 戦車が見つかったら、ワープ処理を開始させる
                // 戦車側に StartWormholeTravel のようなメソッドが必要
                // 例: other.GetComponent<TankController>().StartWormholeTravel(oppositeGate, travelDuration);

                // 各コンポーネントを直接制御する場合 (例)
                 TankWormholeTravel travelController = other.GetComponent<TankWormholeTravel>();
                 if (travelController != null && !travelController.IsTraveling) // まだ移動中でなければ
                 {
                      Debug.Log($"{other.name} entered wormhole, exiting at {oppositeGate.name}");
                      travelController.StartTravel(oppositeGate, travelDuration);
                 }
            }
        }
    }
}