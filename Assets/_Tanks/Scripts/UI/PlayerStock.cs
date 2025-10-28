using UnityEngine;


    public class PlayerStock : MonoBehaviour
    {
        //private TankShooting m_Shooting;

        [SerializeField] private GameObject[] Small_Shells;
        [SerializeField] private GameObject[] Big_Shells;

        public void UpdatePlayerStock(int CurrentShells)
        {
            for(int i=0;i<Small_Shells.Length;i++)
            {
                Small_Shells[i].SetActive(i<CurrentShells%10);
            }

            for(int i=0;i<Big_Shells.Length;i++)
            {
                Big_Shells[i].SetActive(i<CurrentShells/10);
            }
        }
    }
