using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    // ─── Cấu trúc mỗi item ───────────────────────────────────────────────────
    [System.Serializable]
    public class SpawnableItem
    {
        public string  itemName;
        public GameObject prefab;

        [Range(0, 100)]
        [Tooltip("Tỉ lệ xuất hiện (%). Tổng tất cả item phải <= 100.\nPhần còn lại (100 - tổng) = không spawn gì.")]
        public float spawnChance;
    }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Danh sách vật phẩm  (tổng % <= 100)")]
    public SpawnableItem[] items;
    // Coin: 55% | Heart: 8% | Shield: 8% | Potion: 10% | Thunder: 4%  → 15% không spawn

    [Header("Cài đặt spawn")]
    public Transform player;
    public float spawnInterval  = 4f;   // giây / lần thử spawn
    public float spawnDistanceZ = 40f;  // spawn xa hơn để player có thời gian nhặt
    public float laneDistance   = 3.5f;
    public float spawnYOffset   = 0.8f;

    [Header("Coin")]
    public int   coinCount   = 5;
    public float coinSpacing = 3.0f;
    public float minBatchGap = 25f;     // khoảng cách tối thiểu giữa các batch

    [Header("Difficulty Reference")]
    public SwipeController playerController;

    // ─── Private ──────────────────────────────────────────────────────────────
    private float timer;
    private float lastCoinBatchEndZ = float.MinValue;

    // ─── Update ───────────────────────────────────────────────────────────────
    void Start()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<SwipeController>();
    }

    private float GetCurrentSpawnInterval()
    {
        if (playerController == null) return spawnInterval;
        return playerController.currentDifficulty switch
        {
            SwipeController.GameDifficulty.Normal   => spawnInterval * 0.85f,
            SwipeController.GameDifficulty.Hard     => spawnInterval * 0.70f,
            SwipeController.GameDifficulty.VeryHard => spawnInterval * 0.55f,
            SwipeController.GameDifficulty.Extreme   => spawnInterval * 0.40f,
            SwipeController.GameDifficulty.Nightmare => spawnInterval * 0.25f,
            _                                        => spawnInterval
        };
    }

    void Update()
    {
        if (player == null) return;
        timer += Time.deltaTime;
        if (timer >= GetCurrentSpawnInterval())
        {
            TrySpawn();
            timer = 0f;
        }
    }

    // ─── Spawn logic ──────────────────────────────────────────────────────────

    void TrySpawn()
    {
        GameObject prefab = PickItem(); // null = không spawn (phần còn lại của 100%)
        if (prefab == null) return;

        int   lane   = Random.Range(-1, 2);
        float spawnX = lane * laneDistance;

        if (prefab.CompareTag("Coin"))
        {
            SpawnCoinBatch(prefab, spawnX);
        }
        else
        {
            Vector3 pos = new(spawnX, spawnYOffset, player.position.z + spawnDistanceZ);
            Destroy(Instantiate(prefab, pos, Quaternion.identity), 20f);
        }
    }

    void SpawnCoinBatch(GameObject coinPrefab, float spawnX)
    {
        float startZ = Mathf.Max(
            player.position.z + spawnDistanceZ,
            lastCoinBatchEndZ + minBatchGap
        );

        for (int i = 0; i < coinCount; i++)
        {
            Vector3 pos = new(spawnX, spawnYOffset, startZ + i * coinSpacing);
            Destroy(Instantiate(coinPrefab, pos, Quaternion.identity), 60f);
        }

        lastCoinBatchEndZ = startZ + (coinCount - 1) * coinSpacing;
    }

    // ─── Chọn item theo % ─────────────────────────────────────────────────────
    // Roll 0-100. Nếu roll nằm ngoài tổng % của tất cả item → trả về null (không spawn).
    GameObject PickItem()
    {
        float roll   = Random.Range(0f, 100f);
        float cursor = 0f;

        foreach (var item in items)
        {
            cursor += item.spawnChance;
            if (roll < cursor) return item.prefab;
        }

        return null; // phần còn lại = không spawn
    }

    // ─── Validate trong Editor ────────────────────────────────────────────────
    void OnValidate()
    {
        float total = 0f;
        foreach (var item in items) total += item.spawnChance;

        if (total > 100f)
            Debug.LogWarning($"[ItemSpawner] Tổng spawnChance = {total:F1}% > 100%! Hãy giảm xuống.");
        else
            Debug.Log($"[ItemSpawner] Tổng spawnChance = {total:F1}% | Không spawn = {100f - total:F1}%");
    }
}
