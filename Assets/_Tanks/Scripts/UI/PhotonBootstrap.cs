using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonBootstrap : MonoBehaviourPunCallbacks
{
    public static PhotonBootstrap I;

    [SerializeField] private string gameVersion = "0.1";

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.AutomaticallySyncScene = true; // MasterのLoadLevelに全員追従
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PUN] ConnectedToMaster");
    }
}
