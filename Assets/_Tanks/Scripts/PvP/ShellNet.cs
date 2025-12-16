using Photon.Pun;
using UnityEngine;

public class ShellNet : MonoBehaviourPun
{
    [SerializeField] float lifeTime = 5f;

    void Start()
    {
        // 寿命で消す場合も、OwnerだけがPhotonNetwork.Destroyする
        Invoke(nameof(Kill), lifeTime);
    }

    void Kill()
    {
        if (!photonView.IsMine) return;
        PhotonNetwork.Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        if (!photonView.IsMine) return;

        // ここで爆発演出やダメージ通知を出す（必要ならRPC）
        PhotonNetwork.Destroy(gameObject);
    }
}
