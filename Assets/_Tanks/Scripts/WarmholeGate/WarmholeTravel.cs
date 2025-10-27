using System.Collections;
using UnityEngine;

namespace Tanks.Complete // 名前空間はプロジェクトに合わせてください
{
    public class TankWormholeTravel : MonoBehaviour
    {
        [Tooltip("点滅の間隔（秒）")]
        public float blinkInterval = 0.1f;

        [Tooltip("ワープ直後に再度ワープできない時間（秒）")]
        public float exitCooldown = 1.0f;

        // 必要なコンポーネントへの参照
        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        private TankHealth m_Health;
        private Rigidbody m_Rigidbody;
        private Collider[] m_Colliders; // 物理的な衝突も無効化する場合
        private Renderer[] m_Renderers; // 点滅させるためのレンダラー

        private bool m_IsTraveling = false;
        private float m_ExitCooldownTimer = 0f;
        private Coroutine m_BlinkCoroutine = null;

        public bool IsTraveling => m_IsTraveling; // 外部から状態を確認できるように

        void Awake()
        {
            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();
            m_Health = GetComponent<TankHealth>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Colliders = GetComponentsInChildren<Collider>();
            m_Renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            // ワープ後の再ワープ禁止クールダウン
            if (m_ExitCooldownTimer > 0)
            {
                m_ExitCooldownTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// ワームホール移動を開始する（WormholeGateから呼び出される）
        /// </summary>
        public void StartTravel(WormholeGate exitGate, float duration)
        {
            if (m_IsTraveling || m_ExitCooldownTimer > 0) return; // 移動中かクールダウン中は無視

            m_IsTraveling = true;

            // --- 制約を適用 ---
            // 動きを止める (RigidbodyをKinematicにするのが安全)
            if (m_Rigidbody != null) m_Rigidbody.isKinematic = true;
            // スクリプトを無効化 (TankAIなども考慮に入れる)
            if (m_Movement != null) m_Movement.enabled = false;
            if (m_Shooting != null) m_Shooting.enabled = false;
            // 物理的な当たり判定も消す場合 (オプション)
            // foreach (var col in m_Colliders) col.enabled = false;

            // 点滅開始
            m_BlinkCoroutine = StartCoroutine(BlinkEffect());

            // 一定時間後にテレポートして終了するコルーチンを開始
            StartCoroutine(TravelSequence(exitGate, duration));

            // TankHealth側の修正も必要: TakeDamageメソッドの最初に if (GetComponent<TankWormholeTravel>().IsTraveling) return; を追加
            // TankShooting側の修正も必要: Update/Fireメソッドの最初に if (GetComponent<TankWormholeTravel>().IsTraveling) return; を追加
        }

        /// <summary>
        /// ワームホール移動のシーケンス（待機 -> テレポート -> 終了処理）
        /// </summary>
        private IEnumerator TravelSequence(WormholeGate exitGate, float duration)
        {
            // 指定時間待機
            yield return new WaitForSeconds(duration);

            // --- テレポート実行 ---
            Transform exitTransform = exitGate.ExitTransform;
            transform.position = exitTransform.position;
            transform.rotation = exitTransform.rotation;

            // --- 終了処理 ---
            m_IsTraveling = false;
            m_ExitCooldownTimer = exitCooldown; // 再ワープ禁止タイマー開始

            // 点滅終了と表示の確定
            if (m_BlinkCoroutine != null) StopCoroutine(m_BlinkCoroutine);
            SetRenderersEnabled(true); // 確実に表示状態に戻す

            // 制約を解除
            if (m_Movement != null) m_Movement.enabled = true;
            if (m_Shooting != null) m_Shooting.enabled = true;
            // 物理的な当たり判定を戻す場合
            // foreach (var col in m_Colliders) col.enabled = true;
             // RigidbodyのKinematicを解除して物理演算を再開
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = false;
                // オプション: 出口から少し前進する力を加える
                m_Rigidbody.linearVelocity = exitTransform.forward * m_Movement.m_Speed * 0.5f;
            }
        }

        /// <summary>
        /// 点滅エフェクトのコルーチン
        /// </summary>
        private IEnumerator BlinkEffect()
        {
            while (m_IsTraveling)
            {
                SetRenderersEnabled(false); // 消す
                yield return new WaitForSeconds(blinkInterval);
                SetRenderersEnabled(true);  // 表示する
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        /// <summary>
        /// 全てのレンダラーの有効/無効を一括で設定するヘルパーメソッド
        /// </summary>
        private void SetRenderersEnabled(bool enabled)
        {
            foreach (Renderer renderer in m_Renderers)
            {
                 // UI要素など、点滅させたくないRendererは除外する (例: CanvasRenderer)
                 if (!(renderer is CanvasRenderer))
                 {
                      renderer.enabled = enabled;
                 }
            }
        }

         // TankHealth側での修正例
         /*
         public class TankHealth : MonoBehaviour
         {
             private TankWormholeTravel m_WormholeTravel;

             private void Awake() {
                 m_WormholeTravel = GetComponent<TankWormholeTravel>();
             }

             public void TakeDamage(float amount)
             {
                 if (m_WormholeTravel != null && m_WormholeTravel.IsTraveling) return; // ワープ中は無敵
                 // ...本来のダメージ処理...
             }
         }
         */

         // TankShooting側での修正例
         /*
         public class TankShooting : MonoBehaviour
         {
             private TankWormholeTravel m_WormholeTravel;

             private void Awake() {
                 m_WormholeTravel = GetComponent<TankWormholeTravel>();
             }

             private void Update() {
                 if (m_WormholeTravel != null && m_WormholeTravel.IsTraveling) return; // ワープ中は発射不可
                 // ...本来の入力チェックやAIの処理...
             }

             private void Fire() {
                 if (m_WormholeTravel != null && m_WormholeTravel.IsTraveling) return; // ワープ中は発射不可
                  // ...本来の発射処理...
             }
         }
          */
    }
}