using UnityEngine;

namespace Tanks.Complete
{
    public class PlayerStock : MonoBehaviour
    {
        [SerializeField] private GameObject[] Small_Shells;
        [SerializeField] private GameObject[] Big_Shells;
        [SerializeField] private GameObject[] mineImages;

        // 既存：オフライン用（そのまま残す）
        public void UpdatePlayerStock(WeaponStockData data)
        {
            UpdatePlayerStock(data.GetWeaponTag(), data.GetCurrentQuantity());
        }

        // 追加：ネット用（tag, qty で更新）
        public void UpdatePlayerStock(int weaponTag, int currentQuantity)
        {
            if (weaponTag == 0)
            {
                for (int i = 0; i < Small_Shells.Length; i++)
                    Small_Shells[i].SetActive(i < (currentQuantity % 10));

                for (int i = 0; i < Big_Shells.Length; i++)
                    Big_Shells[i].SetActive(i < (currentQuantity / 10));
            }
            else if (weaponTag == 1)
            {
                for (int i = 0; i < mineImages.Length; i++)
                    mineImages[i].SetActive(i < currentQuantity);
            }
        }
    }
}
