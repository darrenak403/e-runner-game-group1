using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class MainStatsController : MonoBehaviour
{
    [Header("UI Text Elements")]
    public TextMeshProUGUI totalCoinsText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI statusText;

    [Header("Persistence")]
    private string savePath;

    private Coroutine statusCoroutine;
    
    [System.Serializable]
    public class GameData
    {
        public int totalCoins;
        public int highScore;
        public int storedHearts;
        public int storedShields;
    }

    private GameData currentData = new GameData();

    void Awake()
    {
        if (statusText != null) statusText.text = ""; // Xóa chữ lúc đầu
        // Sử dụng persistentDataPath để hoạt động trên mọi máy
        savePath = Path.Combine(Application.persistentDataPath, "save.txt");
    }

    void Start()
    {
        LoadAndDisplayStats();
    }

    private void LoadAndDisplayStats()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                currentData = JsonUtility.FromJson<GameData>(json);

                UpdateUI();
            }
            catch (System.Exception e)
            {
                Debug.LogError("MainStats: Không thể đọc file: " + e.Message);
            }
        }
        else
        {
            currentData = new GameData();
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (totalCoinsText != null) totalCoinsText.text = currentData.totalCoins.ToString();
        if (highScoreText != null) highScoreText.text = currentData.highScore.ToString() + "m";
    }

    private void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(currentData);
            File.WriteAllText(savePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("MainStats: Không thể lưu file: " + e.Message);
        }
    }

    // --- HÀM HIỂN THỊ THÔNG BÁO ---
    private void ShowStatus(string message, Color color)
    {
        if (statusText == null) return;

        if (statusCoroutine != null) StopCoroutine(statusCoroutine);
        
        statusText.text = message;
        statusText.color = color;
        statusCoroutine = StartCoroutine(ClearStatusAfterDelay(2f));
    }

    private System.Collections.IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        statusText.text = "";
    }

    // --- SHOP LOGIC ---
    public void BuyHeart()
    {
        int cost = 100;
        if (currentData.totalCoins >= cost)
        {
            currentData.totalCoins -= cost;
            currentData.storedHearts++;
            SaveData();
            UpdateUI();
            ShowStatus("Đã mua 1 Tim!", Color.green);
            Debug.Log("Đã mua 1 Tim! Tổng Tim: " + currentData.storedHearts);
        }
        else
        {
            ShowStatus("Không đủ tiền mua Tim!", Color.red);
            Debug.Log("Không đủ tiền mua Tim!");
        }
    }

    public void BuyShield()
    {
        int cost = 80;
        if (currentData.totalCoins >= cost)
        {
            currentData.totalCoins -= cost;
            currentData.storedShields++;
            SaveData();
            UpdateUI();
            ShowStatus("Đã mua 1 Khiên!", Color.green);
            Debug.Log("Đã mua 1 Khiên! Tổng Khiên: " + currentData.storedShields);
        }
        else
        {
            ShowStatus("Không đủ tiền mua Khiên!", Color.red);
            Debug.Log("Không đủ tiền mua Khiên!");
        }
    }

    // Hàm chuyển về Main Menu
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayGame()
    {
        Debug.Log("PlayGame button clicked!");
        // Nhìn vào ảnh của bạn, tên scene chơi game là "MainScreen"
        SceneManager.LoadScene("MainScreen");
    }
}
