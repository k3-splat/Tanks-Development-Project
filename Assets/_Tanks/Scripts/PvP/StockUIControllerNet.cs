using Photon.Pun;
using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    /// TankInventoryNet を見て PlayerStock(画像表示) を更新する。HUD側に付ける想定。
    /// </summary>
    public class StockUIControllerNet : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private PlayerStock playerStockUI; // 自機表示
        [SerializeField] private PlayerStock enemyStockUI;  // 敵機表示

        private TankInventoryNet _mine;
        private TankInventoryNet _enemy;

        private void Update()
        {
            if (_mine == null || _enemy == null) Resolve();

            if (_mine != null && playerStockUI != null)
            {
                playerStockUI.UpdatePlayerStock(0, _mine.Shells);
                playerStockUI.UpdatePlayerStock(1, _mine.Mines);
            }

            if (_enemy != null && enemyStockUI != null)
            {
                enemyStockUI.UpdatePlayerStock(0, _enemy.Shells);
                enemyStockUI.UpdatePlayerStock(1, _enemy.Mines);
            }
        }

        private void Resolve()
        {
            var all = FindObjectsByType<TankInventoryNet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var inv in all)
            {
                var pv = inv.GetComponent<PhotonView>();
                if (pv == null) continue;

                if (pv.IsMine) _mine = inv;
                else _enemy = inv;
            }
        }
    }
}
