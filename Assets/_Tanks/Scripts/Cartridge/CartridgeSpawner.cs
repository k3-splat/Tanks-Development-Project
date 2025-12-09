using System.Collections;
using UnityEngine;

namespace Tanks.Complete
{
    public class CartridgeSpawner : MonoBehaviour
    {
        // 2. フィールド定義 -----------------------------

        [Header("Cartridge Data")]
        [SerializeField] private CartridgeData shellCartridgeData; // 砲弾
        [SerializeField] private CartridgeData mineCartridgeData;  // 地雷


        // x,z の広さを表す。y はこのオブジェクトの高さを使う。
        [SerializeField] private Vector2 spawnArea = new Vector2(40f, 40f);
        // spawnArea.x : x方向の半径
        // spawnArea.y : z方向の半径

        [SerializeField] private GameManager gameManager;
        // 5. Startでコルーチン開始 -----------------------
        private void Start()
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        // 3. Cartridge を1つ生成するメソッド -------------
        private void SpawnCartridge(CartridgeData data)
        {
            // このオブジェクトを中心として、x,z 方向にランダムにずらす
            Vector3 center = transform.position;

            float randomX = Random.Range(-spawnArea.x, spawnArea.x);
            float randomZ = Random.Range(-spawnArea.y, spawnArea.y);

            // y は固定（Spawner の高さを使う）
            Vector3 spawnPos = new Vector3(
                center.x + randomX,
                center.y,
                center.z + randomZ
            );

            // 向きはとりあえずそのまま（必要ならランダム回転でもOK）
            Instantiate(data.cartridgePrefab, spawnPos, Quaternion.identity);
        }

        // 4. 一定間隔で生成するコルーチン ---------------
        private IEnumerator SpawnRoutine(CartridgeData data)
        {
            while (true)
            {
                SpawnCartridge(data);
                yield return new WaitForSeconds(data.spawnInterval);
            }
        }

        private void HandleGameStateChanged(GameManager.GameLoopState newState)
        {

            if (newState == GameManager.GameLoopState.RoundPlaying)
            {
                StartCoroutine(SpawnRoutine(shellCartridgeData));
                StartCoroutine(SpawnRoutine(mineCartridgeData));
            }
            else
            {
                StopCoroutine(SpawnRoutine(shellCartridgeData));
                StopCoroutine(SpawnRoutine(mineCartridgeData));
            }
        }
    }
}
