using System.Collections;
using UnityEngine;
using Photon.Pun;

namespace Tanks.Complete
{
    public class CartridgeSpawnerNet : MonoBehaviour
    {
        [Header("Cartridge Data")]
        [SerializeField] private CartridgeData shellCartridgeData;
        [SerializeField] private CartridgeData mineCartridgeData;

        [SerializeField] private Vector2 spawnArea = new Vector2(40f, 40f);

        [Header("Reference")]
        [SerializeField] private GameManagerNet gameManagerNet;

        private Coroutine _shellRoutine;
        private Coroutine _mineRoutine;

        private void Awake()
        {
            if (gameManagerNet == null)
                gameManagerNet = FindAnyObjectByType<GameManagerNet>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            if (gameManagerNet != null)
                gameManagerNet.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            if (gameManagerNet != null)
                gameManagerNet.OnGameStateChanged -= HandleGameStateChanged;

            StopAllSpawn();
        }

        private void HandleGameStateChanged(GameManagerNet.GameLoopState st)
        {
            // Masterだけが生成する（重複スポーン防止）
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (st == GameManagerNet.GameLoopState.RoundPlaying)
            {
                if (_shellRoutine == null && shellCartridgeData != null)
                    _shellRoutine = StartCoroutine(SpawnRoutine(shellCartridgeData));

                if (_mineRoutine == null && mineCartridgeData != null)
                    _mineRoutine = StartCoroutine(SpawnRoutine(mineCartridgeData));
            }
            else
            {
                StopAllSpawn();
            }
        }

        private void StopAllSpawn()
        {
            if (_shellRoutine != null) { StopCoroutine(_shellRoutine); _shellRoutine = null; }
            if (_mineRoutine != null)  { StopCoroutine(_mineRoutine);  _mineRoutine  = null; }
        }

        private IEnumerator SpawnRoutine(CartridgeData data)
        {
            while (true)
            {
                SpawnCartridge(data);
                yield return new WaitForSeconds(data.spawnInterval);
            }
        }

        private void SpawnCartridge(CartridgeData data)
        {
            if (data == null || data.cartridgePrefab == null) return;

            Vector3 center = transform.position;
            float randomX = Random.Range(-spawnArea.x, spawnArea.x);
            float randomZ = Random.Range(-spawnArea.y, spawnArea.y);

            Vector3 spawnPos = new Vector3(center.x + randomX, center.y, center.z + randomZ);

            // Photonで同期生成する前提：
            // - data.cartridgePrefab は Resources に置いておく
            // - prefab名は data.cartridgePrefab.name を使う
            PhotonNetwork.Instantiate(data.cartridgePrefab.name, spawnPos, Quaternion.identity);
        }
    }
}
