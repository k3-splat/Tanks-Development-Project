using UnityEngine;

namespace Tanks.Complete
{
    public class HP_WinsNet : MonoBehaviour
    {
        [Header("対戦用HUDルート")]
        [SerializeField] private GameObject hpwins;

        [SerializeField] private GameManagerNet gameManagerNet;

        private void Awake()
        {
            if (hpwins != null) hpwins.SetActive(false);

            if (gameManagerNet == null)
                gameManagerNet = FindAnyObjectByType<GameManagerNet>(FindObjectsInactive.Include);

            if (gameManagerNet != null)
                gameManagerNet.OnGameStateChanged += HandleGameLoopStateChanged;
            else
                Debug.LogWarning("[HP_WinsNet] GameManagerNet が見つかりません。");
        }

        private void OnDestroy()
        {
            if (gameManagerNet != null)
                gameManagerNet.OnGameStateChanged -= HandleGameLoopStateChanged;
        }

        private void HandleGameLoopStateChanged(GameManagerNet.GameLoopState state)
        {
            bool shouldShow =
                state == GameManagerNet.GameLoopState.RoundStarting ||
                state == GameManagerNet.GameLoopState.RoundPlaying ||
                state == GameManagerNet.GameLoopState.RoundEnding;

            if (hpwins != null) hpwins.SetActive(shouldShow);
        }
    }
}
