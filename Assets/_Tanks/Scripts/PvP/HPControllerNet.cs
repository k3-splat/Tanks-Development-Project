using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class HPControllerNet : MonoBehaviour
    {
        [Header("HP Bars (UI上での位置)")]
        public Slider playerHpBar; // ← “自機” として表示したい方
        public Slider enemyHpBar;

        private TankHealthNet _player;
        private TankHealthNet _enemy;

        private bool _swapped;

        private void Start()
        {
            TrySwapBarsIfNeeded();
        }

        private void Update()
        {
            if (!_swapped) TrySwapBarsIfNeeded();

            if (_player == null || _enemy == null)
                ResolveTargets();

            if (_player != null && playerHpBar != null)
                playerHpBar.value = Mathf.Clamp01(_player.CurrentHealth / _player.StartingHealth);

            if (_enemy != null && enemyHpBar != null)
                enemyHpBar.value = Mathf.Clamp01(_enemy.CurrentHealth / _enemy.StartingHealth);
        }

        private void TrySwapBarsIfNeeded()
        {
            if (!PhotonNetwork.InRoom) return;
            var list = PhotonNetwork.PlayerList?.OrderBy(p => p.ActorNumber).ToArray();
            if (list == null || list.Length < 2) return;

            int p1Actor = list[0].ActorNumber;
            bool iAmP1 = PhotonNetwork.LocalPlayer.ActorNumber == p1Actor;

            // UIは常に「playerHpBarが自機」になってほしいので、
            // 自分がP2なら表示側だけ入れ替える
            if (!iAmP1)
            {
                (playerHpBar, enemyHpBar) = (enemyHpBar, playerHpBar);
            }
            _swapped = true;
        }

        private void ResolveTargets()
        {
            var all = FindObjectsByType<TankHealthNet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var h in all)
            {
                var pv = h.GetComponent<PhotonView>();
                if (pv == null) continue;

                if (pv.IsMine) _player = h;
                else _enemy = h;
            }
        }
    }
}
