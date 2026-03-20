using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Danh sách nhạc nền")]
    public AudioClip[] backgroundMusics;

    private AudioSource bgmSource;
    private bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        bgmSource = GetComponent<AudioSource>();
        bgmSource.loop = false;

        // Đảm bảo nhạc Menu đã tắt khi vào màn chơi
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        PlayRandomBGM();
    }

    void Update()
    {
        if (isGameOver) return;

        if (!bgmSource.isPlaying && backgroundMusics.Length > 0)
        {
            PlayRandomBGM();
        }
    }

    void PlayRandomBGM()
    {
        if (backgroundMusics.Length == 0) return;

        int randomIndex = Random.Range(0, backgroundMusics.Length);
        bgmSource.clip = backgroundMusics[randomIndex];
        bgmSource.volume = 0.4f;
        bgmSource.Play();
    }

    // --- HÀM TẮT NHẠC (Khi Game Over) ---
    public void StopMusic()
    {
        isGameOver = true;
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // --- HÀM BẬT LẠI NHẠC (Khi Hồi sinh) ---
    public void PlayMusic()
    {
        isGameOver = false; // Mở lại cờ để Update() có thể chạy tiếp
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }
}