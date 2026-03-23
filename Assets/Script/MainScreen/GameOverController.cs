using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance;

    [Header("UI Panels")]
    public GameObject gameOverPanel;

    // Hai nút riêng biệt của bạn
    public Button respawnButton;
    public Button restartButton;
    public Button exitButton;

    [Header("UI Text Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI heartText;
    public TextMeshProUGUI potionText;
    public TextMeshProUGUI shieldText;
    public TextMeshProUGUI distanceText;

    [Header("UI Rows")]
    public GameObject coinRow;
    public GameObject heartRow;
    public GameObject potionRow;
    public GameObject shieldRow;

    private int coinCount = 0; // Số xu NHẶT ĐƯỢC TRONG MÀN NÀY (Bắt đầu từ 0)
    private int totalCoinsStored = 0; // Tổng số xu lưu trong file
    private int heartCount = 0;
    private int shieldCount = 0;
    private int highScore = 0;

    [Header("Persistence")]
    private string savePath;

    private float startTime;
    private bool isGameOver = false;

    [System.Serializable]
    public class GameData
    {
        public int totalCoins;
        public int highScore;
        public int storedHearts;
        public int storedShields;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Sử dụng persistentDataPath để hoạt động trên mọi máy
        savePath = Path.Combine(Application.persistentDataPath, "save.txt");
        Debug.Log("Dữ liệu sẽ được lưu tại: " + savePath);
    }
    void Start()
    {
        LoadGameData();
        startTime = Time.time;
        isGameOver = false;
        Time.timeScale = 1f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (potionRow != null) potionRow.SetActive(false);

        // Tạo mới hoàn toàn event để xóa CẢ persistent listeners (Inspector) lẫn runtime listeners
        // RemoveAllListeners() chỉ xóa runtime listeners, không xóa persistent listeners từ Inspector
        if (respawnButton != null)
        {
            respawnButton.onClick = new Button.ButtonClickedEvent();
            respawnButton.onClick.AddListener(ReviveGame);
        }
        if (restartButton != null)
        {
            restartButton.onClick = new Button.ButtonClickedEvent();
            restartButton.onClick.AddListener(RestartGame);
        }
        if (exitButton != null)
        {
            exitButton.onClick = new Button.ButtonClickedEvent();
            exitButton.onClick.AddListener(SaveAndExit);
        }

        UpdateAllUI();
    }

    void Update()
    {
        if (!isGameOver) UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (timerText == null) return;
        float t = Time.time - startTime;
        string minutes = ((int)t / 60).ToString("00");
        string seconds = (t % 60).ToString("00");
        timerText.text = minutes + ":" + seconds;
    }

    // --- CỘNG ITEM ---
    public void AddCoin()
    {
        coinCount++;
        UpdateAllUI();
        // Không lưu ngay lập tức nữa, sẽ lưu khi Game Over hoặc Restart
    }
    public void AddHeart() { heartCount++; UpdateRowOrder(heartRow); UpdateAllUI(); }
    public void AddPotion() { /* Potion logic is now timer-based in SwipeController */ }
    public void AddShield() { shieldCount++; UpdateRowOrder(shieldRow); UpdateAllUI(); }

    private void UpdateRowOrder(GameObject row)
    {
        if (row != null) row.transform.SetAsLastSibling();
    }

    // --- LOGIC SỬ DỤNG ITEM (Dùng cho SwipeController) ---
    public int GetShieldCount() { return shieldCount; }
    public bool TryUseShield() { if (shieldCount > 0) { shieldCount--; UpdateAllUI(); return true; } return false; }

    private void UpdateAllUI()
    {
        if (scoreText != null) scoreText.text = ": " + coinCount;
        if (heartText != null) heartText.text = ": " + heartCount;
        if (shieldText != null) shieldText.text = ": " + shieldCount;

        if (heartRow != null) heartRow.SetActive(heartCount > 0);
        if (shieldRow != null) shieldRow.SetActive(shieldCount > 0);
    }

    public void UpdatePotionUI(float remainingTime)
    {
        if (potionRow != null)
        {
            bool isActive = remainingTime > 0;
            potionRow.SetActive(isActive);
            if (isActive && potionText != null)
            {
                potionText.text = ": " + Mathf.CeilToInt(remainingTime).ToString("00") + "s";
            }
        }
    }

    public void UpdateDistanceUI(float distance)
    {
        if (distanceText != null)
        {
            distanceText.text = Mathf.FloorToInt(distance) + "m";
        }
    }

    // --- GAME OVER & HỒI SINH ---
    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // Nút Respawn (màu xanh) chỉ hiện khi có Tim > 0
        if (respawnButton != null) respawnButton.gameObject.SetActive(heartCount > 0);
        // Nút Restart (màu xám) luôn hiện
        if (restartButton != null) restartButton.gameObject.SetActive(true);
        // Nút Exit luôn hiện khi game over
        if (exitButton != null) exitButton.gameObject.SetActive(true);
    }

    public void ReviveGame()
    {
        if (heartCount > 0)
        {
            heartCount--; // Trừ 1 Tim ngay lập tức

            // Cập nhật lại UI để người chơi thấy số Tim đã giảm
            UpdateAllUI();

            // Reset các item khác về 0 (Khiên), giữ nguyên xu
            shieldCount = 0;
            UpdateAllUI();

            // Mở lại game
            isGameOver = false;
            Time.timeScale = 1f;
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Bật lại nhạc game sau khi hồi sinh
            if (BGMManager.Instance != null)
            {
                BGMManager.Instance.PlayMusic();
            }

            // Gọi Player bắt đầu trạng thái "Bóng ma" đi xuyên vật cản
            SwipeController player = FindFirstObjectByType<SwipeController>();
            if (player != null)
            {
                player.enabled = true;
                player.StartReviveProcess();
            }
        }
    }

    public void RestartGame()
    {
        SaveGameData();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SaveAndExit()
    {
        SaveGameData();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // Hàm chuyển về Main Menu
    public void ReturnToMainMenu()
    {
        SaveGameData();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // --- LƯU VÀ TẢI DỮ LIỆU FILE ---
    private void LoadGameData()
    {
        coinCount = 0; // Luôn bắt đầu màn chơi với 0 xu
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                GameData data = JsonUtility.FromJson<GameData>(json);
                totalCoinsStored = data.totalCoins;
                highScore = data.highScore;

                // Lấy số Tim và Khiên đã mua từ shop
                heartCount = data.storedHearts;
                shieldCount = data.storedShields;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Không thể đọc file: " + e.Message);
            }
        }
    }

    private void SaveGameData()
    {
        try
        {
            // 1. Kiểm tra kỷ lục quãng đường trước khi lưu
            SwipeController player = FindFirstObjectByType<SwipeController>();
            if (player != null)
            {
                int currentDistance = Mathf.FloorToInt(player.transform.position.z);
                if (currentDistance > highScore)
                {
                    highScore = currentDistance;
                    Debug.Log("Kỷ lục mới! " + highScore + "m");
                }
            }

            // 2. Chuẩn bị dữ liệu
            GameData data = new GameData();
            // Cộng dồn số xu màn này vào tổng số
            data.totalCoins = totalCoinsStored + coinCount;
            data.highScore = highScore;

            // Lưu lại số Tim và Khiên hiện còn (sau khi lượm hoặc dùng)
            data.storedHearts = heartCount;
            data.storedShields = shieldCount;

            string json = JsonUtility.ToJson(data);
            File.WriteAllText(savePath, json);

            // 3. Cập nhật lại bộ nhớ đệm
            totalCoinsStored = data.totalCoins;
            coinCount = 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Không thể lưu file: " + e.Message);
        }
    }
}