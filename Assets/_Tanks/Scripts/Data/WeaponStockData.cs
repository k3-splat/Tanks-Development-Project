using UnityEngine;
namespace Tanks.Complete
{
    // 2. Inspector で表示できるようにする
    [System.Serializable]
    public class WeaponStockData
    {
        // 3. 公開フィールド
        [SerializeField]private int m_InitialQuantity;
        [SerializeField]private int m_MaxCapacity;
        [SerializeField]private int m_ReplenishQuantity;

        private int m_CurrentQuantity;

        public int GetCurrentQuantity()
        {
            return m_CurrentQuantity;
        }
        public void InitializeQuantity()
        {
            m_CurrentQuantity=m_InitialQuantity;
        }
        public void Replenish()
        {
            if(m_CurrentQuantity+m_ReplenishQuantity<m_MaxCapacity)
            {
                m_CurrentQuantity=m_CurrentQuantity+m_ReplenishQuantity;
            }else{
                m_CurrentQuantity=m_MaxCapacity;
            }
        }
        public void Use()
        {
            m_CurrentQuantity--;
        }
    }

}
