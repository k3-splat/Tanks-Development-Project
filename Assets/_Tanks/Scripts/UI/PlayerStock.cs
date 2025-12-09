using UnityEngine;

namespace Tanks.Complete
{
    public class PlayerStock : MonoBehaviour
    {

        [SerializeField] private GameObject[] Small_Shells;
        [SerializeField] private GameObject[] Big_Shells;
        [SerializeField] private GameObject[] mineImages;

        public void UpdatePlayerStock(WeaponStockData data)
        {
            if(data.GetWeaponTag()==0){
                for(int i=0;i<Small_Shells.Length;i++)
                {
                    Small_Shells[i].SetActive(i<data.GetCurrentQuantity()%10);
                }

                for(int i=0;i<Big_Shells.Length;i++)
                {
                    Big_Shells[i].SetActive(i<data.GetCurrentQuantity()/10);
                }
            }else if(data.GetWeaponTag()==1){
                for (int i = 0; i < mineImages.Length; i++)
                {
                    mineImages[i].SetActive(i<data.GetCurrentQuantity());
                }
            }
        }
    }
}