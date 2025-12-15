using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class HPControllerNet : MonoBehaviour
    {
        [Header("HP Bars")]
        public Slider playerHpBar;
        public Slider enemyHpBar;

        private TankHealthNet _player;
        private TankHealthNet _enemy;

        private void Update()
        {
            // まだ取れてなければ探す（軽くするなら数秒おきにしてもOK）
            if (_player == null || _enemy == null)
                ResolveTargets();

            if (_player != null && playerHpBar != null)
                playerHpBar.value = Mathf.Clamp01(_player.CurrentHealth / _player.StartingHealth);

            if (_enemy != null && enemyHpBar != null)
                enemyHpBar.value = Mathf.Clamp01(_enemy.CurrentHealth / _enemy.StartingHealth);
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
