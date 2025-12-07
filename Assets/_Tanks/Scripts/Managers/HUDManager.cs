using UnityEngine;
// GameLoopStateを使用するために必要
using Tanks.Complete; 

namespace Tanks.Complete
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Stock UI")]
        [SerializeField] private GameObject Player1Stock;
        [SerializeField] private GameObject Player2Stock;

        [Header("Minimap UI")]
        [Tooltip("ミニマップを表示するRawImage")]
        [SerializeField] private GameObject MinimapImage; 

        [Header("Managers")]
        [SerializeField] private GameManager GameManager;

        // プレイヤー1（自機）のミニマップ用カメラへの参照
        private Camera player1Camera;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // 初期状態ではUIを非表示
            Player1Stock.SetActive(false);
            Player2Stock.SetActive(false);
            
            if (MinimapImage != null)
                MinimapImage.SetActive(false);

            // ゲームステート変更イベントの購読
            GameManager.OnGameStateChanged += HandleGameStateChanged;

            // 各戦車のイベント購読
            foreach (TankManager tankManager in GameManager.m_SpawnPoints)
            {
                if (tankManager != null)
                {
                    tankManager.OnWeaponStockChanged += HandleWeaponStockChanged;
                }
            }

            // 解答例 手順5: プレハブのCameraコンポーネントを取得して全て非アクティブにする
            // これにより、生成される全ての戦車でデフォルトでカメラが無効化された状態になります
            DisablePrefabCameras();
        }

        // プレハブ上のカメラを無効化する処理
        private void DisablePrefabCameras()
        {
            // GameManagerに登録されている4つのプレハブを配列にまとめる
            GameObject[] tankPrefabs = { 
                GameManager.m_Tank1Prefab, 
                GameManager.m_Tank2Prefab, 
                GameManager.m_Tank3Prefab, 
                GameManager.m_Tank4Prefab 
            };

            foreach (var prefab in tankPrefabs)
            {
                if (prefab != null)
                {
                    // 子オブジェクトに含まれるCameraコンポーネントを取得 (非アクティブなものも含む)
                    Camera cam = prefab.GetComponentInChildren<Camera>(true);
                    if (cam != null)
                    {
                        // カメラのGameObjectごと非アクティブにする
                        cam.gameObject.SetActive(false);
                    }
                }
            }
        }

        // ゲームステートが変更された時の処理
        private void HandleGameStateChanged(GameManager.GameLoopState newState)
        {
            // ラウンドプレイ中のみ有効にする
            bool isPlaying = (newState == GameManager.GameLoopState.RoundPlaying);

            // ストック表示の更新
            Player1Stock.SetActive(isPlaying);
            Player2Stock.SetActive(isPlaying);

            // ミニマップUI (RawImage) の表示更新
            if (MinimapImage != null)
                MinimapImage.SetActive(isPlaying);

            // ミニマップ用カメラ (player1Camera) の有効化制御
            if (isPlaying)
            {
                // player1Cameraの参照がまだない場合、Player1のインスタンスから取得を試みる
                if (player1Camera == null)
                {
                    // GameManagerのm_SpawnPoints[0]がPlayer1に相当
                    if (GameManager.m_SpawnPoints.Length > 0 && GameManager.m_SpawnPoints[0].m_Instance != null)
                    {
                        // 生成されたインスタンスからカメラを取得
                        player1Camera = GameManager.m_SpawnPoints[0].m_Instance.GetComponentInChildren<Camera>(true);
                    }
                }

                // カメラが見つかれば有効化
                if (player1Camera != null)
                {
                    player1Camera.gameObject.SetActive(true);
                }
            }
            else
            {
                // プレイ中以外はカメラを無効化
                if (player1Camera != null)
                {
                    player1Camera.gameObject.SetActive(false);
                }
            }
        }

        // 砲弾ストック数が変更された時の処理
        private void HandleWeaponStockChanged(int controlIndex, int currentShells)
        {
            if (controlIndex == 1)
                Player1Stock.GetComponent<PlayerStock>().UpdatePlayerStock(currentShells);
            else if (controlIndex == 2)
                Player2Stock.GetComponent<PlayerStock>().UpdatePlayerStock(currentShells);
        }
    }
}