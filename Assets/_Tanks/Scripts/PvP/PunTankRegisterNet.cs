using UnityEngine;
using Photon.Pun;

namespace Tanks.Complete
{
    public class PunTankRegisterNet : MonoBehaviourPun
    {
        void Start()
        {
            var gm = FindFirstObjectByType<GameManagerNet>();
            var ctrl = GetComponent<TankNetController>();
            if (gm != null && ctrl != null)
                gm.RegisterTank(ctrl);
        }
    }
}
