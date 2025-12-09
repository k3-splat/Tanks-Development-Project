using UnityEngine;

namespace Tanks.Complete{
    public class HUDManager : MonoBehaviour
    {

        [SerializeField] private GameObject Player1Stock;
        [SerializeField] private GameObject Player2Stock;
        [SerializeField] private GameManager GameManager;
        private PlayerStock player1StockComp;
        private PlayerStock player2StockComp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Player1Stock.SetActive(false);
            Player2Stock.SetActive(false);
            player1StockComp = Player1Stock.GetComponent<PlayerStock>();
            player2StockComp = Player2Stock.GetComponent<PlayerStock>();

            GameManager.OnGameStateChanged += HandleGameStateChanged;

            foreach (TankManager tankManager in GameManager.m_SpawnPoints)
            {
                if (tankManager != null)
                {
                    tankManager.OnWeaponStockChanged += HandleWeaponStockChanged;
                }
            }

        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void HandleGameStateChanged(GameManager.GameLoopState newState)
        {
            bool isPlaying = (newState == GameManager.GameLoopState.RoundPlaying);

            Player1Stock.SetActive(isPlaying);
            Player2Stock.SetActive(isPlaying);
        }

        public void HandleWeaponStockChanged(int playerNumber, WeaponStockData weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogWarning($"[HUDManager] weaponData が null です (playerNumber={playerNumber})");
                return;
            }

            switch (playerNumber)
            {
                case 1:
                    player1StockComp?.UpdatePlayerStock(weaponData);
                break;

                case 2:
                    player2StockComp?.UpdatePlayerStock(weaponData);
                    break;

                default:
                    Debug.LogWarning($"[HUDManager] 未対応の PlayerNumber: {playerNumber}");
                    break;
            }
        }
    }
}