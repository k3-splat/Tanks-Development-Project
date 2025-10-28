using UnityEngine;

namespace Tanks.Complete{
    public class HUDManager : MonoBehaviour
    {

        [SerializeField] private GameObject Player1Stock;
        [SerializeField] private GameObject Player2Stock;
        [SerializeField] private GameManager GameManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Player1Stock.SetActive(false);
            Player2Stock.SetActive(false);

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

        private void HandleWeaponStockChanged(int controlIndex, int currentShells)
        {
            if (controlIndex == 1)
                Player1Stock.GetComponent<PlayerStock>().UpdatePlayerStock(currentShells);
            else if (controlIndex == 2)
                Player2Stock.GetComponent<PlayerStock>().UpdatePlayerStock(currentShells);

        }
    }
}