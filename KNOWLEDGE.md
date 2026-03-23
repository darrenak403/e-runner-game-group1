# 📚 Kiến Thức Unity Đã Sử Dụng Trong Project Runner

Tài liệu này tổng hợp tất cả các kiến thức lập trình Unity (C#) đã được áp dụng trong project game **Endless Runner**.

---

## 📑 Mục Lục

1. [Singleton Pattern](#1-singleton-pattern)
2. [Coroutine (IEnumerator)](#2-coroutine-ienumerator)
3. [Raycast](#3-raycast)
4. [Physics – Collision & Trigger](#4-physics--collision--trigger)
5. [Physics.OverlapBox](#5-physicsoverlapbox)
6. [Scene Management](#6-scene-management)
7. [DontDestroyOnLoad](#7-dontdestroyonload)
8. [Animator & Animation Control](#8-animator--animation-control)
9. [AnimationCurve](#9-animationcurve)
10. [Cinemachine](#10-cinemachine)
11. [AudioSource & AudioClip](#11-audiosource--audioclip)
12. [UI – TextMeshPro & Button](#12-ui--textmeshpro--button)
13. [Data Persistence – JSON File I/O](#13-data-persistence--json-file-io)
14. [Instantiate & Destroy](#14-instantiate--destroy)
15. [Transform & Rigidbody](#15-transform--rigidbody)
16. [Time.timeScale](#16-timetimescale)
17. [Renderer & Bounds](#17-renderer--bounds)
18. [Skybox & RenderSettings](#18-skybox--rendersettings)
19. [Shader & Material](#19-shader--material)
20. [Enum & Switch Expression](#20-enum--switch-expression)
21. [RequireComponent Attribute](#21-requirecomponent-attribute)
22. [RuntimeInitializeOnLoadMethod](#22-runtimeinitializeonloadmethod)
23. [Event Registration (sceneLoaded)](#23-event-registration-sceneloaded)
24. [Swipe / Touch Input](#24-swipe--touch-input)
25. [Object Pooling Pattern (Tự động hủy)](#25-object-pooling-pattern-tự-động-hủy)
26. [Serializable Class & Inspector](#26-serializable-class--inspector)
27. [FindFirstObjectByType](#27-findfirstobjectbytype)
28. [Physics.IgnoreCollision](#28-physicsignorecollision)
29. [Mathf Utilities](#29-mathf-utilities)
30. [CompareTag](#30-comparetag)

---

## 1. Singleton Pattern

**Khái niệm:** Đảm bảo chỉ có **duy nhất một instance** của class tồn tại trong game. Các script khác truy cập qua `ClassName.Instance`.

**Áp dụng trong:** `AudioManager`, `BGMManager`, `GameOverController`

```csharp
// AudioManager.cs
public static AudioManager Instance;

private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else
    {
        Destroy(gameObject);
    }
}
```

**Tại sao dùng?** Để AudioManager tồn tại xuyên suốt các Scene mà không bị tạo lại, và các script khác gọi được `AudioManager.Instance.StopMusic()`.

---

## 2. Coroutine (IEnumerator)

**Khái niệm:** Coroutine cho phép thực thi code **trải dài qua nhiều frame** mà không block Main Thread. Sử dụng `yield return` để tạm dừng.

**Áp dụng trong:** `BGMManager`, `SwipeController`, `MainStatsController`

### 2.1. WaitForSeconds – Đợi thời gian

```csharp
// BGMManager.cs – Đợi hết bài nhạc rồi chuyển bài
private IEnumerator WaitAndPlayNext(float delay)
{
    yield return new WaitForSeconds(delay);
    if (!isGameOver)
    {
        PlayRandomBGM();
    }
}
```

### 2.2. yield return null – Đợi 1 frame

```csharp
// SwipeController.cs – Nhảy mượt mà qua nhiều frame
IEnumerator JumpRoutine()
{
    isJumping = true;
    float timeElapsed = 0f;
    float startY = transform.position.y;

    while (timeElapsed < effectiveJumpDuration)
    {
        timeElapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(timeElapsed / effectiveJumpDuration);
        float curveValue = jumpCurve.Evaluate(normalizedTime);
        float currentY = startY + (jumpHeight * curveValue);
        transform.position = new Vector3(transform.position.x, currentY, transform.position.z);
        yield return null; // Đợi frame tiếp theo
    }
}
```

### 2.3. Quản lý Coroutine (Start/Stop)

```csharp
// BGMManager.cs – Hủy coroutine cũ trước khi tạo mới
if (musicCoroutine != null)
{
    StopCoroutine(musicCoroutine);
    musicCoroutine = null;
}
musicCoroutine = StartCoroutine(WaitAndPlayNext(bgmSource.clip.length));
```

### 2.4. Hiệu ứng nhấp nháy (Immortal flashing)

```csharp
// SwipeController.cs – Nhấp nháy nhân vật khi bất tử
IEnumerator ImmortalRoutine()
{
    isImmortal = true;
    float timer = 0;
    float duration = 3f;
    float flashInterval = 0.1f;

    while (timer < duration)
    {
        bool isVisible = (Mathf.FloorToInt(timer / flashInterval) % 2 == 0);
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = isVisible;
        }
        yield return null;
        timer += Time.deltaTime;
    }
    isImmortal = false;
}
```

---

## 3. Raycast

**Khái niệm:** Bắn một tia vô hình từ điểm A theo hướng B, kiểm tra xem tia có chạm vào Collider nào không. Thường dùng để kiểm tra **mặt đất** (ground check).

**Áp dụng trong:** `SwipeController`

```csharp
// SwipeController.cs – Kiểm tra nhân vật có đang đứng trên mặt đất không
void CheckGrounded()
{
    if (Physics.Raycast(
        transform.position + Vector3.up * 0.1f,  // Điểm bắt đầu (hơi trên chân)
        Vector3.down,                              // Hướng bắn (xuống dưới)
        out RaycastHit hit,                        // Thông tin va chạm
        2f,                                        // Khoảng cách tối đa
        groundLayer))                              // Chỉ kiểm tra layer "Ground"
    {
        isGrounded = hit.distance <= 0.3f;
        if (isGrounded && !isJumping && !isSliding)
            groundY = hit.point.y; // Lưu lại Y của mặt đất
    }
}
```

**Cũng dùng trong Start() để xác định vị trí mặt đất ban đầu:**

```csharp
if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out hit, 5f, groundLayer))
    groundY = hit.point.y;
```

**LayerMask:** Dùng `groundLayer` để chỉ raycast vào các object thuộc layer "Ground", tránh va phải Obstacle/Item.

---

## 4. Physics – Collision & Trigger

**Khái niệm:** Unity cung cấp 2 cách phát hiện va chạm:
- **OnCollisionEnter**: Va chạm vật lý thực (cả 2 đều có Rigidbody + Collider, `isTrigger = false`)
- **OnTriggerEnter**: Va chạm kiểu "vùng phát hiện" (một trong 2 có `isTrigger = true`)

**Áp dụng trong:** `SwipeController`

```csharp
// OnTriggerEnter – Phát hiện nhặt item (Coin, Potion, Shield, Heart, Thunder)
private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Coin"))
    {
        CollectItem(other.gameObject);
        GameOverController.Instance.AddCoin();
    }
    else if (other.CompareTag("Obstacle"))
    {
        CheckObstacleCollision(other.gameObject);
    }
}

// OnCollisionEnter – Va chạm vật lý với Obstacle
private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Obstacle"))
    {
        if (isImmortal)
        {
            Physics.IgnoreCollision(collision.collider, bodyCollider, true);
            return;
        }
        CheckObstacleCollision(collision.gameObject);
    }
}
```

**Khi hồi sinh:** Đổi Collider sang `isTrigger = true` để nhân vật đi xuyên vật cản nhưng vẫn nhặt được item.

---

## 5. Physics.OverlapBox

**Khái niệm:** Quét một **hình hộp ảo** trong không gian 3D, trả về danh sách tất cả Collider nằm trong vùng đó.

**Áp dụng trong:** `SwipeController` – Item Thunder (sấm sét)

```csharp
// Quét vùng 50 unit phía trước, xóa tất cả Obstacle
Collider[] hitColliders = Physics.OverlapBox(
    transform.position + transform.forward * 25f,    // Tâm hộp (25m phía trước)
    new Vector3(laneDistance * 1.5f, 5f, 25f)         // Kích thước nửa hộp
);

foreach (var hit in hitColliders)
{
    if (hit.CompareTag("Obstacle"))
    {
        Instantiate(thunderVFX, hit.transform.position, Quaternion.identity);
        Destroy(hit.gameObject);
    }
}
```

---

## 6. Scene Management

**Khái niệm:** Chuyển đổi giữa các Scene (màn hình) trong game. Cần `using UnityEngine.SceneManagement`.

**Áp dụng trong:** `MainMenuController`, `MainInstructionsController`, `GameOverController`, `MainStatsController`

```csharp
// Chuyển scene
SceneManager.LoadScene("MainScreen");
SceneManager.LoadScene("MainMenu");

// Lấy tên scene hiện tại
string currentScene = SceneManager.GetActiveScene().name;

// Reload scene hiện tại (Restart game)
SceneManager.LoadScene(SceneManager.GetActiveScene().name);
```

---

## 7. DontDestroyOnLoad

**Khái niệm:** Giữ GameObject **không bị hủy** khi chuyển Scene. Thường kết hợp với Singleton.

**Áp dụng trong:** `AudioManager`

```csharp
DontDestroyOnLoad(gameObject);
```

**Tại sao?** Nhạc nền menu cần phát liên tục khi chuyển giữa MainMenu ↔ MainStats, không bị reset.

---

## 8. Animator & Animation Control

**Khái niệm:** Điều khiển Animation State Machine của nhân vật.

**Áp dụng trong:** `SwipeController`

```csharp
// Trigger animation
animator.SetTrigger("jump");
animator.SetTrigger("slide");

// Reset trigger để tránh xung đột
animator.ResetTrigger("slide");

// Điều chỉnh tốc độ animation (tăng dần theo độ khó)
animator.speed = currentAnimSpeed;

// Cấu hình animator
animator.updateMode = AnimatorUpdateMode.Normal;
animator.applyRootMotion = false; // Tắt Root Motion để tự quản lý vị trí
```

---

## 9. AnimationCurve

**Khái niệm:** Đường cong tùy chỉnh trong Inspector, dùng để điều khiển giá trị theo thời gian một cách mượt mà.

**Áp dụng trong:** `SwipeController` – Arc nhảy (Jump)

```csharp
// Khai báo curve mặc định: lên → đỉnh → xuống
public AnimationCurve jumpCurve = new AnimationCurve(
    new Keyframe(0, 0),     // 0% thời gian → Y = 0
    new Keyframe(0.5f, 1),  // 50% thời gian → Y = đỉnh
    new Keyframe(1, 0)      // 100% thời gian → Y = 0
);

// Sử dụng trong JumpRoutine
float normalizedTime = Mathf.Clamp01(timeElapsed / effectiveJumpDuration);
float curveValue = jumpCurve.Evaluate(normalizedTime); // 0.0 → 1.0 → 0.0
float currentY = startY + (jumpHeight * curveValue);
```

**Lợi ích:** Có thể chỉnh dạng đường cong nhảy trực tiếp trong Inspector mà không cần code lại.

---

## 10. Cinemachine

**Khái niệm:** Package camera chuyên nghiệp của Unity, quản lý nhiều góc nhìn camera.

**Áp dụng trong:** `CameraSwitcher`, `SwipeController`

### 10.1. Chuyển đổi góc nhìn bằng Priority

```csharp
// CameraSwitcher.cs – Nhấn V để đổi góc nhìn
public CinemachineCamera tppCamera; // Third-person
public CinemachineCamera fppCamera; // First-person

if (isFirstPerson)
{
    fppCamera.Priority = 15; // Camera nào Priority cao hơn sẽ được dùng
    tppCamera.Priority = 5;
}
```

### 10.2. Camera Shake (Impulse)

```csharp
// SwipeController.cs – Rung camera khi sét đánh
public CinemachineImpulseSource thunderImpulseSource;
thunderImpulseSource.GenerateImpulse(); // Rung!
```

---

## 11. AudioSource & AudioClip

**Khái niệm:** `AudioSource` là component phát âm thanh. `AudioClip` là file âm thanh.

**Áp dụng trong:** `AudioManager`, `BGMManager`, `SwipeController`

```csharp
// Phát nhạc nền (chỉ 1 bài tại một thời điểm)
musicSource.clip = menuMusics[randomIndex];
musicSource.Play();

// Phát hiệu ứng âm thanh (chồng lên nhạc nền)
audioSource.PlayOneShot(coinSound, coinVolume);

// Kiểm tra trạng thái
if (!musicSource.isPlaying) PlayRandomMusic();

// Cấu hình
musicSource.loop = false;
musicSource.volume = 0.5f;
```

**`RequireComponent(typeof(AudioSource))`:** Tự động thêm AudioSource nếu chưa có.

---

## 12. UI – TextMeshPro & Button

**Khái niệm:** TextMeshPro (TMP) là hệ thống text cao cấp. Button cho phép gắn sự kiện click.

**Áp dụng trong:** `GameOverController`, `MainStatsController`

```csharp
// Cập nhật text
scoreText.text = ": " + coinCount;
timerText.text = minutes + ":" + seconds;
distanceText.text = Mathf.FloorToInt(distance) + "m";

// Gắn sự kiện Button bằng code
respawnButton.onClick.AddListener(ReviveGame);
restartButton.onClick.AddListener(RestartGame);

// Ẩn/hiện UI
gameOverPanel.SetActive(false);
respawnButton.gameObject.SetActive(heartCount > 0);
```

---

## 13. Data Persistence – JSON File I/O

**Khái niệm:** Lưu/đọc dữ liệu game vào file JSON bằng `JsonUtility` + `System.IO`.

**Áp dụng trong:** `GameOverController`, `MainStatsController`

```csharp
[System.Serializable]
public class GameData
{
    public int totalCoins;
    public int highScore;
    public int storedHearts;
    public int storedShields;
}

// Đường dẫn lưu file (hoạt động trên mọi nền tảng)
savePath = Path.Combine(Application.persistentDataPath, "save.txt");

// Lưu
string json = JsonUtility.ToJson(data);
File.WriteAllText(savePath, json);

// Đọc
string json = File.ReadAllText(savePath);
GameData data = JsonUtility.FromJson<GameData>(json);
```

**`Application.persistentDataPath`:** Trả về thư mục an toàn cho mỗi nền tảng (Windows: `AppData/LocalLow/...`).

---

## 14. Instantiate & Destroy

**Khái niệm:** `Instantiate` tạo bản sao của Prefab. `Destroy` hủy GameObject.

**Áp dụng trong:** `ItemSpawner`, `LevelGenerator`, `SwipeController`

```csharp
// Tạo object tại vị trí
Instantiate(prefab, position, Quaternion.identity);

// Tạo và tự hủy sau N giây (tránh rò rỉ bộ nhớ)
Destroy(Instantiate(prefab, pos, Quaternion.identity), 20f);

// Tạo và gắn vào parent
GameObject obstacle = Instantiate(obstaclePrefab, finalPos, finalRot);
obstacle.transform.SetParent(parentTile);

// Tạo VFX rồi tự hủy
GameObject vfx = Instantiate(thunderVFX, vfxPos, Quaternion.identity);
Destroy(vfx, 2f);
```

---

## 15. Transform & Rigidbody

**Khái niệm:** `Transform` quản lý vị trí, xoay, scale. `Rigidbody` thêm vật lý cho object.

**Áp dụng trong:** `SwipeController`, `CoinRotator`

```csharp
// Di chuyển bằng Transform (không dùng vật lý)
transform.position = new Vector3(newX, newY, newZ);

// Xoay liên tục (đồng xu xoay)
transform.Rotate(0, speed * Time.deltaTime, 0, Space.World);

// Rigidbody control
rb.useGravity = false;
rb.isKinematic = true;    // Tắt vật lý, dùng code di chuyển
rb.linearVelocity = Vector3.zero; // Reset vận tốc
```

---

## 16. Time.timeScale

**Khái niệm:** Điều chỉnh tốc độ thời gian game. `0` = dừng game (pause), `1` = bình thường.

**Áp dụng trong:** `GameOverController`

```csharp
// Game Over → Dừng game
Time.timeScale = 0f;

// Hồi sinh / Restart → Chạy lại
Time.timeScale = 1f;
```

**Lưu ý:** Khi `Time.timeScale = 0`, `Time.deltaTime = 0` → mọi thứ đứng yên.

---

## 17. Renderer & Bounds

**Khái niệm:** `Renderer` là component hiển thị hình ảnh. `Bounds` cho biết kích thước thực tế của object.

**Áp dụng trong:** `LevelGenerator` – Đo chiều dài tường để xếp sát nhau

```csharp
Renderer[] renderers = leftWall.GetComponentsInChildren<Renderer>();
Bounds bounds = renderers[0].bounds;
foreach (Renderer r in renderers)
{
    bounds.Encapsulate(r.bounds); // Gộp toàn bộ Renderer con
}
float trueLength = bounds.size.z;             // Chiều dài thực
float offsetToBackEdge = -bounds.min.z;       // Bù trừ pivot
```

**Cũng dùng trong `SwipeController`:** Bật/tắt Renderer để tạo hiệu ứng nhấp nháy.

---

## 18. Skybox & RenderSettings

**Khái niệm:** Skybox là nền trời 360°. `RenderSettings.skybox` thay đổi skybox runtime.

**Áp dụng trong:** `SkyboxChanger`

```csharp
RenderSettings.skybox = skyboxMaterials[0];
DynamicGI.UpdateEnvironment(); // Cập nhật ánh sáng môi trường
```

---

## 19. Shader & Material

**Khái niệm:** Shader quyết định cách render. Material chứa shader + textures.

**Áp dụng trong:** `SkyboxChanger` – Blend mượt giữa 2 skybox

```csharp
// Tạo Material blend từ Custom Shader
var mat = new Material(blend6SidedShader);
mat.SetTexture("_FrontTexA", fromMaterial.GetTexture("_FrontTex"));
mat.SetFloat("_Blend", 0f);

// Crossfade mỗi frame
float smooth = Mathf.SmoothStep(0f, 1f, t);
blendMaterial.SetFloat("_Blend", smooth);

// Fallback khi không có custom shader
blendMaterial.Lerp(fromMaterial, toMaterial, smooth);
```

**Hỗ trợ 3 loại skybox:** 6-Sided, Panoramic, Cubemap – mỗi loại có shader blend riêng.

---

## 20. Enum & Switch Expression

**Khái niệm:** `enum` định nghĩa tập hợp hằng số. C# 8+ hỗ trợ **switch expression** ngắn gọn.

**Áp dụng trong:** `SwipeController`, `ItemSpawner`, `LevelGenerator`

```csharp
// Định nghĩa enum
public enum GameDifficulty { Easy, Normal, Hard, VeryHard, Extreme, Nightmare }

// Switch expression (C# 8+)
GameDifficulty next = currentDifficulty switch
{
    GameDifficulty.Easy     => GameDifficulty.Normal,
    GameDifficulty.Normal   => GameDifficulty.Hard,
    GameDifficulty.Hard     => GameDifficulty.VeryHard,
    _                       => GameDifficulty.Nightmare
};

// Dùng cho spawn interval theo độ khó
return playerController.currentDifficulty switch
{
    SwipeController.GameDifficulty.Normal => spawnInterval * 0.85f,
    SwipeController.GameDifficulty.Hard   => spawnInterval * 0.70f,
    _                                     => spawnInterval
};
```

---

## 21. RequireComponent Attribute

**Khái niệm:** Attribute bắt buộc GameObject phải có component cụ thể.

**Áp dụng trong:** `AudioManager`, `BGMManager`

```csharp
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour { ... }
```

**Lợi ích:** Khi gắn script vào GameObject, Unity tự thêm AudioSource nếu chưa có.

---

## 22. RuntimeInitializeOnLoadMethod

**Khái niệm:** Gọi hàm **tự động** trước khi bất kỳ Scene nào load, không cần gắn vào GameObject.

**Áp dụng trong:** `MobileResolutionInit`

```csharp
public static class MobileResolutionInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetMobileResolution()
    {
        Screen.SetResolution(630, 1120, FullScreenMode.Windowed);
    }
}
```

**Lợi ích:** Class static, không cần MonoBehaviour, chạy tự động.

---

## 23. Event Registration (sceneLoaded)

**Khái niệm:** Đăng ký/hủy sự kiện để phản ứng khi Scene được load.

**Áp dụng trong:** `AudioManager`

```csharp
// Đăng ký
SceneManager.sceneLoaded += OnSceneLoaded;

// Hủy đăng ký (tránh memory leak)
SceneManager.sceneLoaded -= OnSceneLoaded;

// Callback
private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    if (scene.name == "MainScreen") StopMusic();
    else if (scene.name == "MainMenu") PlayRandomMusic();
}
```

---

## 24. Swipe / Touch Input

**Khái niệm:** Phát hiện hướng vuốt (swipe) dựa trên vị trí chuột/ngón tay.

**Áp dụng trong:** `SwipeController`

```csharp
// Lưu điểm bắt đầu chạm
if (Input.GetMouseButtonDown(0)) startMousePos = Input.mousePosition;

// Khi nhả, tính hướng vuốt
if (Input.GetMouseButtonUp(0))
{
    Vector2 swipeVector = (Vector2)Input.mousePosition - startMousePos;
    if (swipeVector.magnitude > minSwipeDistance)
    {
        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
            ChangeLane(swipeVector.x > 0 ? 1 : -1);  // Trái/Phải
        else
            if (swipeVector.y > 0) Jump(); else Slide(); // Lên/Xuống
    }
}
```

**Cũng hỗ trợ bàn phím:** W (nhảy), S (trượt), A/D (đổi làn).

---

## 25. Object Pooling Pattern (Tự động hủy)

**Khái niệm:** Tự động hủy object sau một thời gian để tránh tràn bộ nhớ.

**Áp dụng trong:** `ItemSpawner`, `LevelGenerator`

```csharp
// Destroy sau 20 giây (item biến mất nếu player không nhặt)
Destroy(Instantiate(prefab, pos, Quaternion.identity), 20f);

// Destroy sau 60 giây (coin batch cần tồn tại lâu hơn)
Destroy(Instantiate(coinPrefab, pos, Quaternion.identity), 60f);

// LevelGenerator: Xóa tile cũ khi player chạy qua
private void DeleteTile()
{
    Destroy(activeTiles[0]);
    activeTiles.RemoveAt(0);
}
```

---

## 26. Serializable Class & Inspector

**Khái niệm:** Đánh dấu class `[System.Serializable]` để hiển thị trong Inspector và dùng với `JsonUtility`.

**Áp dụng trong:** `ItemSpawner`, `GameOverController`, `MainStatsController`

```csharp
[System.Serializable]
public class SpawnableItem
{
    public string itemName;
    public GameObject prefab;

    [Range(0, 100)]
    [Tooltip("Tỉ lệ xuất hiện")]
    public float spawnChance;
}

// Khai báo mảng trong Inspector
public SpawnableItem[] items;
```

**Attributes hữu ích:** `[Header]`, `[Tooltip]`, `[Range]`, `[SerializeField]`

---

## 27. FindFirstObjectByType

**Khái niệm:** Tìm object đầu tiên theo type trong Scene (thay thế `FindObjectOfType` đã deprecated).

**Áp dụng trong:** `GameOverController`, `ItemSpawner`, `LevelGenerator`

```csharp
SwipeController player = FindFirstObjectByType<SwipeController>();
```

---

## 28. Physics.IgnoreCollision

**Khái niệm:** Bỏ qua va chạm giữa 2 Collider cụ thể.

**Áp dụng trong:** `SwipeController` – Khi hồi sinh, đi xuyên vật cản

```csharp
Physics.IgnoreCollision(collision.collider, bodyCollider, true);
```

---

## 29. Mathf Utilities

**Khái niệm:** Thư viện toán học của Unity.

**Áp dụng trong:** Hầu hết các script

```csharp
Mathf.Lerp(a, b, t)           // Nội suy tuyến tính (chuyển động mượt)
Mathf.SmoothStep(0, 1, t)     // Ease in-out (skybox transition)
Mathf.Clamp(value, min, max)  // Giới hạn giá trị
Mathf.Clamp01(t)              // Giới hạn 0-1
Mathf.MoveTowards(a, b, step) // Di chuyển đều
Mathf.FloorToInt(f)           // Làm tròn xuống
Mathf.CeilToInt(f)            // Làm tròn lên
Mathf.Abs(x)                  // Giá trị tuyệt đối
Mathf.Min(a, b)               // Lấy số nhỏ hơn
Mathf.Max(a, b)               // Lấy số lớn hơn
Random.Range(min, max)         // Số ngẫu nhiên
```

---

## 30. CompareTag

**Khái niệm:** So sánh tag của GameObject. Nhanh hơn và an toàn hơn `gameObject.tag == "..."`.

**Áp dụng trong:** `SwipeController`, `ItemSpawner`

```csharp
if (other.CompareTag("Coin")) { ... }
if (other.CompareTag("Obstacle")) { ... }
if (prefab.CompareTag("Coin")) { ... }
```

---

## 📊 Tổng Hợp Script và Kiến Thức

| Script | Kiến thức chính |
|--------|----------------|
| `AudioManager` | Singleton, DontDestroyOnLoad, Event (sceneLoaded), AudioSource |
| `BGMManager` | Singleton, Coroutine, AudioSource |
| `CameraSwitcher` | Cinemachine Priority |
| `CoinRotator` | Transform.Rotate, Time.deltaTime |
| `GameOverController` | Singleton, UI (TMP/Button), JSON I/O, Time.timeScale, FindFirstObjectByType |
| `ItemSpawner` | Serializable Class, Random spawn, Instantiate/Destroy, Enum Switch Expression |
| `LevelGenerator` | Procedural Generation, Renderer Bounds, Instantiate/SetParent |
| `MainInstructionsController` | Scene Management |
| `MainMenuController` | Scene Management, Application.Quit |
| `MainStatsController` | JSON I/O, Coroutine, UI, Shop Logic |
| `MobileResolutionInit` | RuntimeInitializeOnLoadMethod, Static Class |
| `SkyboxChanger` | Shader, Material, RenderSettings, Skybox Crossfade, Enum |
| `SwipeController` | Raycast, Coroutine, Animator, AnimationCurve, Physics, Swipe Input, Cinemachine Impulse, OverlapBox, Collider manipulation |

---

> 📝 **Ghi chú:** Tài liệu này phục vụ cho môn học **PRU213** – tổng hợp toàn bộ kiến thức Unity C# đã áp dụng trong project game Endless Runner.
