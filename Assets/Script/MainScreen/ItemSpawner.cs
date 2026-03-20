using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableItem
    {
        public GameObject prefab;
        [Range(0, 100)] public float spawnChance; // Tỉ lệ xuất hiện (0-100)
    }

    [Header("Danh sách vật phẩm")]
    public SpawnableItem[] items;
    public Transform player;

    [Header("Cài đặt")]
    public float spawnInterval = 1f;
    public float spawnDistanceZ = 30f;
    public float laneDistance = 3.5f;

    [Header("Coin Settings")]
    public int coinCount = 5;
    public float coinSpacing = 3.0f;
    public float minBatchGap = 6.0f;
    public float spawnYOffset = 0.8f;

    private float timer;
    private float lastCoinBatchEndZ = float.MinValue;
    private float totalSpawnWeight;

    void Start()
    {
        foreach (var item in items) totalSpawnWeight += item.spawnChance;
    }

    void Update()
    {
        if (player == null) return;
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnRandomItem();
            timer = 0f;
        }
    }

    void SpawnRandomItem()
    {
        // 1. Chọn Lane ngẫu nhiên (-1, 0, 1)
        int randomLane = Random.Range(-1, 2);
        float spawnX = randomLane * laneDistance;

        // 2. Chọn loại vật phẩm
        GameObject selectedPrefab = ChooseItem();

        if (selectedPrefab != null)
        {
            bool isCoin = selectedPrefab.CompareTag("Coin");

            if (isCoin)
            {
                float desiredStartZ = player.position.z + spawnDistanceZ;

                // Đảm bảo batch mới không đè lên batch trước
                float startZ = Mathf.Max(desiredStartZ, lastCoinBatchEndZ + minBatchGap);

                for (int i = 0; i < coinCount; i++)
                {
                    float spawnZ = startZ + i * coinSpacing;
                    Vector3 spawnPos = new Vector3(spawnX, spawnYOffset, spawnZ);
                    GameObject newItem = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                    Destroy(newItem, 30f);
                }

                lastCoinBatchEndZ = startZ + (coinCount - 1) * coinSpacing;
            }
            else
            {
                Vector3 spawnPos = new Vector3(spawnX, spawnYOffset, player.position.z + spawnDistanceZ);
                GameObject newItem = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                Destroy(newItem, 15f);
            }
        }
    }
    GameObject ChooseItem()
    {
        float randomPoint = Random.value * totalSpawnWeight;

        for (int i = 0; i < items.Length; i++)
        {
            if (randomPoint < items[i].spawnChance) return items[i].prefab;
            randomPoint -= items[i].spawnChance;
        }
        return null;
    }
}