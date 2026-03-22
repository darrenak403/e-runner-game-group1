using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Cài đặt Prefabs")]
    public GameObject groundPrefab;
    public GameObject[] obstaclePrefabs;

    // --- MỚI THÊM: Mảng chứa 20 prefab hàng rào/tường ---
    public GameObject[] wallPrefabs;
    public Transform playerTransform;

    [Header("Thông số đường")]
    public float tileLength = 100f;
    public int numberOfTiles = 5;
    public float laneWidth = 3.5f;

    [Header("Cài đặt Random Chung")]
    public int obstaclesPerTile = 5;
    public Vector3 globalRotationEuler = new Vector3(0, 90, 0);
    public float globalYOffset = 0f;

    // --- MỚI THÊM: Cài đặt sinh Tường 2 bên ---
    [Header("Cài đặt Tường 2 bên")]
    public float wallSpacing = 10f; // Khoảng cách giữa các mảnh tường (trục Z)
    public float leftWallX = -6f;   // Tọa độ X của lề trái (chỉnh cho sát mép đường)
    public float rightWallX = 6f;   // Tọa độ X của lề phải
    public float wallYOffset = 0f;  // Độ cao của tường
    public Vector3 leftWallRotation = new Vector3(0, 0, 0);   // Góc xoay tường trái
    public Vector3 rightWallRotation = new Vector3(0, 180, 0); // Góc xoay tường phải

    [Header("Fix Lệch Trục X Cho Obstacle_1")]
    public string specialObstacleName = "Obstacle_1";
    public float ob1_LeftX = -4.6f;
    public float ob1_MiddleX = -1.3f;
    public float ob1_RightX = 2f;

    [Header("Difficulty Reference")]
    public SwipeController playerController;

    private float spawnZ = 0f;
    private List<GameObject> activeTiles = new List<GameObject>();

    void Start()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<SwipeController>();

        spawnZ = -(tileLength / 2f);
        for (int i = 0; i < numberOfTiles; i++)
        {
            SpawnTile(true);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (playerTransform.position.z > (spawnZ - (numberOfTiles * tileLength) + tileLength))
        {
            SpawnTile(true);
            DeleteTile();
        }
    }

    public void SpawnTile(bool spawnObstacles)
    {
        Vector3 spawnPos = new Vector3(0, 0, spawnZ + (tileLength / 2f));
        GameObject go = Instantiate(groundPrefab, spawnPos, Quaternion.identity);
        go.transform.SetParent(transform);
        activeTiles.Add(go);

        if (spawnObstacles)
        {
            GenerateObstaclesOnTile(go.transform, spawnZ);

            // --- MỚI THÊM: Gọi hàm sinh tường ngay sau khi sinh chướng ngại vật ---
            GenerateWallsOnTile(go.transform, spawnZ);
        }
        spawnZ += tileLength;
    }

    private int GetObstacleCount()
    {
        if (playerController == null) return obstaclesPerTile;
        return playerController.currentDifficulty switch
        {
            SwipeController.GameDifficulty.Normal   => obstaclesPerTile + 1,
            SwipeController.GameDifficulty.Hard     => obstaclesPerTile + 2,
            SwipeController.GameDifficulty.VeryHard => obstaclesPerTile + 4,
            SwipeController.GameDifficulty.Extreme   => obstaclesPerTile + 6,
            SwipeController.GameDifficulty.Nightmare => obstaclesPerTile + 9,
            _                                        => obstaclesPerTile
        };
    }

    void GenerateObstaclesOnTile(Transform parentTile, float zStart)
    {
        int count = GetObstacleCount();
        float segmentLength = tileLength / count;

        for (int i = 0; i < count; i++)
        {
            int randomLane = Random.Range(-1, 2);
            float randomX = randomLane * laneWidth;

            float randomZ = zStart + (i * segmentLength) + Random.Range(2f, segmentLength - 2f);

            // Vùng an toàn đầu game
            if (zStart < 0 && randomZ > -5f && randomZ < 12f) continue;

            GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

            Vector3 finalPos = new Vector3(randomX, globalYOffset, randomZ);
            Quaternion finalRot = Quaternion.Euler(globalRotationEuler);

            if (obstaclePrefab.name.Contains(specialObstacleName))
            {
                if (randomLane == -1) finalPos.x = ob1_LeftX;
                else if (randomLane == 0) finalPos.x = ob1_MiddleX;
                else if (randomLane == 1) finalPos.x = ob1_RightX;
            }

            GameObject obstacle = Instantiate(obstaclePrefab, finalPos, finalRot);
            obstacle.transform.SetParent(parentTile);
        }
    }

    // --- MỚI THÊM: Hàm xử lý sinh tường/hàng rào ---
    void GenerateWallsOnTile(Transform parentTile, float zStart)
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0) return;

        // ==========================================
        // 1. SINH TƯỜNG LỀ TRÁI
        // ==========================================
        float currentLeftZ = zStart;
        while (currentLeftZ < zStart + tileLength)
        {
            GameObject prefab = wallPrefabs[Random.Range(0, wallPrefabs.Length)];

            // Bước 1: Sinh vật thể ở ngay gốc tọa độ (0,0,0) kèm góc xoay để quét hình dáng chuẩn nhất
            GameObject leftWall = Instantiate(prefab, Vector3.zero, Quaternion.Euler(leftWallRotation));

            float trueLength = wallSpacing; // Kích thước dự phòng nếu model bị lỗi
            float offsetToBackEdge = 0f;

            // Bước 2: Quét toàn bộ hình dáng hiển thị của vật thể (Bỏ qua Collider)
            Renderer[] renderers = leftWall.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                {
                    bounds.Encapsulate(r.bounds);
                }

                // Chiều dài thực tế của vật thể
                trueLength = bounds.size.z;

                // Công thức bù trừ Tâm (Pivot): Tính khoảng cách từ Tâm đến cái "lưng" của vật thể
                offsetToBackEdge = -bounds.min.z;
            }

            // Bước 3: Đẩy vật thể vào đúng vị trí lề đường, với đuôi của nó chạm đúng mép currentLeftZ
            Vector3 finalPos = new Vector3(leftWallX, wallYOffset, currentLeftZ + offsetToBackEdge);
            leftWall.transform.position = finalPos;
            leftWall.transform.SetParent(parentTile);

            // Bước 4: Nhích tọa độ Z lên đúng bằng chiều dài vật thể vừa sinh để lấy chỗ cho nhà tiếp theo
            currentLeftZ += trueLength;
        }

        // ==========================================
        // 2. SINH TƯỜNG LỀ PHẢI (Áp dụng công thức y hệt)
        // ==========================================
        float currentRightZ = zStart;
        while (currentRightZ < zStart + tileLength)
        {
            GameObject prefab = wallPrefabs[Random.Range(0, wallPrefabs.Length)];

            // --- FIX Ở ĐÂY: Xoay lề phải ngược 180 độ so với lề trái ---
            Vector3 correctRightRotation = new Vector3(leftWallRotation.x, leftWallRotation.y + 180f, leftWallRotation.z);
            GameObject rightWall = Instantiate(prefab, Vector3.zero, Quaternion.Euler(correctRightRotation));
            // ------------------------------------------------------------

            float trueLength = wallSpacing;
            float offsetToBackEdge = 0f;

            Renderer[] renderers = rightWall.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                {
                    bounds.Encapsulate(r.bounds);
                }

                trueLength = bounds.size.z;
                offsetToBackEdge = -bounds.min.z;
            }

            Vector3 finalPos = new Vector3(rightWallX, wallYOffset, currentRightZ + offsetToBackEdge);
            rightWall.transform.position = finalPos;
            rightWall.transform.SetParent(parentTile);

            currentRightZ += trueLength;
        }
    }

    private void DeleteTile()
    {
        if (activeTiles.Count > 0)
        {
            Destroy(activeTiles[0]);
            activeTiles.RemoveAt(0);
        }
    }

    float GetObjectLength(GameObject obj)
    {
        // Ưu tiên đo bằng lớp va chạm (Collider)
        Collider col = obj.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.size.z;

        // Nếu không có Collider, đo bằng lưới hiển thị hình ảnh (Mesh/Renderer)
        Renderer ren = obj.GetComponentInChildren<Renderer>();
        if (ren != null) return ren.bounds.size.z;

        // Nếu model bị lỗi không thể đo được, dùng tạm biến wallSpacing làm kích thước xài tạm
        return wallSpacing;
    }
}