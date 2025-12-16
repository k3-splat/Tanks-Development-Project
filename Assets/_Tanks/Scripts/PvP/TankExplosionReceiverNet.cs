using Photon.Pun;
using UnityEngine;

namespace Tanks.Complete
{
    public class TankExplosionReceiverNet : MonoBehaviourPun
    {
        [PunRPC]
        public void RpcExplosionHit(float explosionForce, Vector3 explosionPosition, float explosionRadius, float maxDamage)
        {
            // まず計算
            float dist  = Vector3.Distance(transform.position, explosionPosition);
            float atten = 1f - Mathf.Clamp01(dist / explosionRadius);
            float damage = maxDamage * atten;

            // ログ（dist/damage計算後！）
            Debug.Log($"[HitRPC] mine={photonView.IsMine} viewID={photonView.ViewID} dist={dist:F2} dmg={damage:F1}", this);

            // ★所有者だけ処理（重複防止）
            if (!photonView.IsMine) return;

            // 1) 吹っ飛び
            var move = GetComponent<TankMovementNet>();
            if (move != null)
                move.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0f);

            // 2) HPへ（Master権威ルート）
            var health = GetComponent<TankHealthNet>();
            if (health != null)
                health.TakeDamage(damage);
        }
    }
}
