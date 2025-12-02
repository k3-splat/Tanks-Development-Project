using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    /// ゲーム中（ラウンド開始～終了）だけ、HPバーや勝利数HUDを表示するための制御クラス。
    /// GameManager の OnGameStateChanged にぶら下がって表示/非表示を切り替える。
    /// </summary>
    public class HP_Wins : MonoBehaviour
    {
        [Header("対戦用HUDルート")]
        public GameObject hpwins;  // Player1HPbar / Player2HPbar / WinsText などの親

        private GameManager _gameManager;

        private void Awake()
        {
            // 最初は非表示（タンク選択などでは出さない）
            if (hpwins != null)
            {
                hpwins.SetActive(false);
            }

            // シーン内の GameManager を探す
            _gameManager = FindAnyObjectByType<GameManager>();

            if (_gameManager != null)
            {
                // ゲーム状態変更イベントにハンドラを登録
                _gameManager.OnGameStateChanged += HandleGameLoopStateChanged;
            }
            else
            {
                Debug.LogWarning("[HP_Wins] GameManager が見つかりませんでした。HUDの表示制御が行えません。");
            }
        }

        private void OnDestroy()
        {
            // シーン変更時などにイベント登録を解除
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= HandleGameLoopStateChanged;
            }
        }

        private void HandleGameLoopStateChanged(GameManager.GameLoopState state)
        {
            // RoundStarting / RoundPlaying / RoundEnding のどれかなら「ゲーム中」とみなして表示
            bool shouldShow =
                state == GameManager.GameLoopState.RoundStarting ||
                state == GameManager.GameLoopState.RoundPlaying ||
                state == GameManager.GameLoopState.RoundEnding;

            if (hpwins != null)
            {
                hpwins.SetActive(shouldShow);
            }
        }
    }
}
