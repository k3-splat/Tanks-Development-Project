using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class HPController : MonoBehaviour
    {
        [Header("HP Bars (Filled Images)")]
        public Slider playerHpBar;
        public Slider enemyHpBar;

        private TankHealth _playerHealth;
        private TankHealth _enemyHealth;

        public void SetPlayerTank(TankHealth health)
        {
            _playerHealth = health;
        }

        public void SetEnemyTank(TankHealth health)
        {
            _enemyHealth = health;
        }

        private void Update()
        {
            if (_playerHealth != null && playerHpBar != null)
            {
                float ratio = _playerHealth.CurrentHealth / _playerHealth.StartingHealth;
                playerHpBar.value = Mathf.Clamp01(ratio);
            }

            if (_enemyHealth != null && enemyHpBar != null)
            {
                float ratio = _enemyHealth.CurrentHealth / _enemyHealth.StartingHealth;
                enemyHpBar.value = Mathf.Clamp01(ratio);
            }
        }
    }
}
