using UnityEngine;

namespace Tanks.Complete
{
    // 2. Inspector で表示できるようにする
    [System.Serializable]
    public class CartridgeData
    {
        // 3. 公開フィールド
        public GameObject cartridgePrefab; // カートリッジのプレハブ
        public float spawnInterval;        // 生成頻度（秒）
    }
}