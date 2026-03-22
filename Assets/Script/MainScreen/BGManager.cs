using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Danh sách nhạc nền")]
    public AudioClip[] backgroundMusics;

    private AudioSource bgmSource;
    private bool isGameOver = false;
    private Coroutine musicCoroutine;

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

    void PlayRandomBGM()
    {
        if (backgroundMusics.Length == 0) return;

        // Hủy coroutine cũ để tránh phát đè
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
            musicCoroutine = null;
        }

        int randomIndex = Random.Range(0, backgroundMusics.Length);
        bgmSource.clip = backgroundMusics[randomIndex];
        bgmSource.volume = 0.4f;
        bgmSource.Play();

        // Đợi đúng thời lượng bài rồi mới chuyển bài tiếp
        musicCoroutine = StartCoroutine(WaitAndPlayNext(bgmSource.clip.length));
    }

    private IEnumerator WaitAndPlayNext(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isGameOver)
        {
            PlayRandomBGM();
        }
    }

    // --- HÀM TẮT NHẠC (Khi Game Over) ---
    public void StopMusic()
    {
        isGameOver = true;
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
            musicCoroutine = null;
        }
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // --- HÀM BẬT LẠI NHẠC (Khi Hồi sinh) ---
    public void PlayMusic()
    {
        isGameOver = false;
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
            // Tiếp tục đợi phần còn lại của bài hiện tại
            float remaining = bgmSource.clip.length - bgmSource.time;
            musicCoroutine = StartCoroutine(WaitAndPlayNext(remaining));
        }
    }
}