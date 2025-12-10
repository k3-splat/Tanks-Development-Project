using UnityEngine;

namespace Tanks.Complete
{
    public class Cartridge : MonoBehaviour
    {
        [Header("Cartridge Settings")]
        [SerializeField] private float m_LifeTime = 15f;       //消滅までの時間 管理用タイマー
        [SerializeField] private float m_BlinkInterval = 0.2f; // 点滅間隔
        [SerializeField] private float m_Blinklong = 3f;     //点滅時間
        private float m_BlinkTimer = 0f;                       // 点滅管理用タイマー

        private Renderer m_Renderer;                           // Renderer の参照

        void Start()
        {
            // Renderer を取得
            m_Renderer = GetComponent<Renderer>();
            if (m_Renderer == null)
            {
                Debug.LogWarning("[Cartridge] Renderer コンポーネントが見つかりません。");
            }
        }

        void Update()
        {
            // 生存時間をカウント
            m_LifeTime -= Time.deltaTime;

            // 一定時間経過で消滅
            if (m_LifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 点滅タイマー更新
            m_BlinkTimer += Time.deltaTime;

            // 点滅の間隔を超えたら表示状態を切り替える
            if (m_BlinkTimer >= m_BlinkInterval && m_LifeTime <= m_Blinklong)
            {
                m_BlinkTimer = 0f;

                if (m_Renderer != null)
                {
                    m_Renderer.enabled = !m_Renderer.enabled;
                }
            }
        }
    }
}
