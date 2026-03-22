using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Danh sách nhạc nền Menu")]
    public AudioClip[] menuMusics;

    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            musicSource = GetComponent<AudioSource>();

            // Đăng ký sự kiện khi đổi Scene
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi bộ nhớ khi Object bị hủy (ví dụ khi tắt game)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nếu vào màn chơi game thì tắt nhạc Menu (BGMManager sẽ lo nhạc game)
        if (scene.name == "MainScreen")
        {
            StopMusic();
        }
        // Nếu quay về Menu hoặc Stats thì bật lại nhạc Menu
        else if (scene.name == "MainMenu" || scene.name == "MainStats")
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                PlayRandomMusic();
            }
        }
    }

    private void Start()
    {
        if (menuMusics.Length > 0)
        {
            PlayRandomMusic();
        }
    }

    private void Update()
    {
        // Tự động chuyển bài nếu bài cũ kết thúc và đang ở scene Menu/Stats
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "MainScreen")
        {
            if (musicSource != null && !musicSource.isPlaying && menuMusics.Length > 0)
            {
                PlayRandomMusic();
            }
        }
    }

    public void PlayRandomMusic()
    {
        if (menuMusics == null || menuMusics.Length == 0) return;

        int randomIndex = Random.Range(0, menuMusics.Length);
        musicSource.clip = menuMusics[randomIndex];
        musicSource.volume = 0.5f;
        musicSource.Play();
        Debug.Log("AudioManager: Đang phát nhạc menu: " + musicSource.clip.name);
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
}
