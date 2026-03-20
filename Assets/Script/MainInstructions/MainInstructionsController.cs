using UnityEngine;
using UnityEngine.SceneManagement;
public class MainInstructionsController : MonoBehaviour
{
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
