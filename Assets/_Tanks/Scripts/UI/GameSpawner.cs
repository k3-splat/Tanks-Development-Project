using Photon.Pun;
using UnityEngine;

public class GameSpawner : MonoBehaviour
{
    [SerializeField] private string tankPrefabName = "Tank"; // Resources/Tank.prefab
    [SerializeField] private Transform[] spawnPoints; // 0=Master, 1=Client

    void Start()
    {
        int idx = PhotonNetwork.IsMasterClient ? 0 : 1;
        var sp = spawnPoints[idx];
        PhotonNetwork.Instantiate(tankPrefabName, sp.position, sp.rotation);
    }
}
