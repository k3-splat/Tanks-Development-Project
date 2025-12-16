using Photon.Pun;
using UnityEngine;

namespace Tanks.Complete
{
    public class HUDManagerNet : MonoBehaviour
    {
        [Header("Stock UI")]
        [SerializeField] private GameObject Player1Stock;
        [SerializeField] private GameObject Player2Stock;

        [Header("Minimap UI")]
        [SerializeField] private GameObject MinimapImage;

        [Header("Managers")]
        [SerializeField] private GameManagerNet gameManagerNet;

        private PlayerStock player1StockComp;
        private PlayerStock player2StockComp;

        private Camera localTankCamera;

        private void Start()
        {
            if (Player1Stock != null) Player1Stock.SetActive(false);
            if (Player2Stock != null) Player2Stock.SetActive(false);
            if (MinimapImage != null) MinimapImage.SetActive(false);

            if (Player1Stock != null) player1StockComp = Player1Stock.GetComponent<PlayerStock>();
            if (Player2Stock != null) player2StockComp = Player2Stock.GetComponent<PlayerStock>();

            if (gameManagerNet == null)
                gameManagerNet = FindAnyObjectByType<GameManagerNet>();

            if (gameManagerNet != null)
                gameManagerNet.OnGameStateChanged += HandleGameStateChanged;
            else
                Debug.LogWarning("[HUDManagerNet] GameManagerNet が見つかりません。");
        }

        private void OnDestroy()
        {
            if (gameManagerNet != null)
                gameManagerNet.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameManagerNet.GameLoopState newState)
        {
            bool isPlaying = (newState == GameManagerNet.GameLoopState.RoundPlaying);

            if (Player1Stock != null) Player1Stock.SetActive(isPlaying);
            if (Player2Stock != null) Player2Stock.SetActive(isPlaying);
            if (MinimapImage != null) MinimapImage.SetActive(isPlaying);

            UpdateLocalCamera(isPlaying);
        }

        private void UpdateLocalCamera(bool enable)
        {
            // まず全戦車のカメラを切る（PvPは各クライアントで “自機だけON” が基本）
            var tanks = FindObjectsByType<TankNetController>(FindObjectsSortMode.None);
            foreach (var t in tanks)
            {
                var pv = t.GetComponent<PhotonView>();
                var cam = t.GetComponentInChildren<Camera>(true);
                if (cam == null) continue;

                // 自機以外は常にOFF
                if (pv != null && !pv.IsMine)
                    cam.gameObject.SetActive(false);
            }

            // 自機のカメラを探してON/OFF
            if (localTankCamera == null)
            {
                foreach (var t in tanks)
                {
                    var pv = t.GetComponent<PhotonView>();
                    if (pv != null && pv.IsMine)
                    {
                        localTankCamera = t.GetComponentInChildren<Camera>(true);
                        break;
                    }
                }
            }

            if (localTankCamera != null)
                localTankCamera.gameObject.SetActive(enable);
        }

        // ストック更新（Net側の武器処理から呼ぶ用）
        public void HandleWeaponStockChanged(int playerNumber, WeaponStockData weaponData)
        {
            if (weaponData == null) return;

            if (playerNumber == 1) player1StockComp?.UpdatePlayerStock(weaponData);
            else if (playerNumber == 2) player2StockComp?.UpdatePlayerStock(weaponData);
        }
    }
}
