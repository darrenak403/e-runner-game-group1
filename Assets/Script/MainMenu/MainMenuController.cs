using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc phải có dòng này để chuyển Scene

public class MainMenuController : MonoBehaviour
{
    // Hàm này sẽ được gọi khi bấm nút Play
    public void PlayGame()
    {
        Debug.Log("PlayGame button clicked!");
        // Nhìn vào ảnh của bạn, tên scene chơi game là "MainScreen"
        SceneManager.LoadScene("MainScreen");
    }

    // Hàm này sẽ được gọi khi bấm nút Quit
    public void QuitGame()
    {
        Debug.Log("QuitGame button clicked!"); // Dòng này để test trong Editor
        Application.Quit(); // Lệnh này sẽ hoạt động khi bạn build game ra file chạy thật
    }

    public void GoToMainStats()
    {
        SceneManager.LoadScene("MainStats");
    }

    public void GoToMainInstructions()
    {
        SceneManager.LoadScene("MainInstructions");
    }
}