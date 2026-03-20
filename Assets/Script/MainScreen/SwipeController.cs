using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class SwipeController : MonoBehaviour
{
    public Animator animator;
    private Rigidbody rb;
    private Vector2 startMousePos;
    private float minSwipeDistance = 20f;

    [Header("Lane Movement")]
    private int targetLane = 1;
    public float laneDistance = 3.5f;
    public float laneSpeed = 10f;

    [Header("Collider Settings")]
    public CapsuleCollider bodyCollider;
    public BoxCollider footCollider;

    public enum GameDifficulty { Easy, Normal, Hard }

    [Header("Difficulty & Speed Settings")]
    public GameDifficulty currentDifficulty = GameDifficulty.Easy;

    private float currentForwardSpeed;
    private float targetForwardSpeed;

    public float maxAnimSpeed = 3.0f;
    public float speedIncreaseRate = 0.01f;
    private float currentAnimSpeed = 1.0f;
    private float targetAnimSpeed = 1.0f;

    [Header("Difficulty Progression Logic")]
    public float timeToUpgradeDifficulty = 60f;
    private float difficultyTimer = 0f;

    [Header("Audio Settings")]
    public AudioClip jumpSound;
    [Range(0f, 1f)] public float jumpVolume = 1.0f;
    public AudioClip slideSound;
    [Range(0f, 1f)] public float slideVolume = 1.0f;
    public AudioClip coinSound;
    [Range(0f, 1f)] public float coinVolume = 1.0f;
    public AudioClip thunderClip;
    [Range(0f, 1f)] public float thunderVolume = 1.0f;
    public AudioClip gameOverSound;
    [Range(0f, 1f)] public float gameOverVolume = 1.0f;

    private AudioSource audioSource;
    private float originalHeight;
    private Vector3 originalCenter;
    private float originalRadius;

    [Header("Jump Settings")]
    public float jumpDuration = 0.9f;
    public float jumpHeight = 1.5f;
    private float defaultJumpHeight = 1.5f;

    [Header("Slide Settings")]
    public float slideDuration = 1.533f;
    public float slideHeightRatio = 0.3f;

    [Header("Mixamo Sync (Curves)")]
    public AnimationCurve jumpCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    private Coroutine activeRoutine;

    [Header("Physics Settings")]
    public bool isGrounded;
    public LayerMask groundLayer;
    private float groundY;

    [Header("Camera Shake Effect")]
    public CinemachineImpulseSource thunderImpulseSource;

    private bool isJumping = false;
    private bool isSliding = false;

    // --- BIẾN MỚI CHO ITEM ---
    private bool isImmortal = false;
    public GameObject shieldEffect;
    public float shieldYOffset = 1.0f;
    public GameObject thunderVFX;
    private GameObject shieldInstance;

    // --- Biến để quản lý thời gian Potion ---
    private float potionTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider>();

        if (bodyCollider != null)
        {
            originalHeight = bodyCollider.height;
            originalCenter = bodyCollider.center;
            originalRadius = bodyCollider.radius;
        }

        defaultJumpHeight = jumpHeight;

        // Disable Rigidbody gravity to fix jittering
        if (rb != null)
        {
            rb.useGravity = false;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out hit, 5f, groundLayer))
            groundY = hit.point.y;
        else
            groundY = transform.position.y;

        currentForwardSpeed = 6f;
        targetForwardSpeed = 6f;

        ApplyDifficultySettings();

        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.applyRootMotion = false;
            animator.speed = currentAnimSpeed;
        }
    }

    void Update()
    {
        SmoothSpeedTransition();
        HandleDifficultyProgression();
        HandleMovement();
        HandleInput();
        HandleAnimationSpeed();
        HandlePotionEffect();
        UpdateDistance();
        HandleShieldVisual();
    }

    private void HandleShieldVisual()
    {
        if (shieldEffect == null || GameOverController.Instance == null) return;

        bool hasShield = GameOverController.Instance.GetShieldCount() > 0;

        // Tạo instance một lần duy nhất khi có shield
        if (hasShield && shieldInstance == null)
        {
            shieldInstance = Instantiate(shieldEffect, transform);
            shieldInstance.transform.localPosition = new Vector3(0, shieldYOffset, 0);
            foreach (var c in shieldInstance.GetComponentsInChildren<Collider>())
                Destroy(c);
        }

        if (shieldInstance != null)
            shieldInstance.SetActive(hasShield);
    }

    private void UpdateDistance()
    {
        if (GameOverController.Instance != null)
        {
            // Assuming the player starts at Z = 0
            float distance = transform.position.z;
            GameOverController.Instance.UpdateDistanceUI(distance);
        }
    }

    private void HandlePotionEffect()
    {
        if (potionTimer > 0)
        {
            potionTimer -= Time.deltaTime;
            jumpHeight = 3f;
            if (potionTimer <= 0)
            {
                potionTimer = 0;
                jumpHeight = defaultJumpHeight;
            }

            if (GameOverController.Instance != null)
            {
                GameOverController.Instance.UpdatePotionUI(potionTimer);
            }
        }
    }

    private void SmoothSpeedTransition()
    {
        currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetForwardSpeed, Time.deltaTime * 5f);
        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 5f);
        if (animator != null) animator.speed = currentAnimSpeed;
    }

    private void HandleDifficultyProgression()
    {
        if (currentDifficulty == GameDifficulty.Hard) return;
        difficultyTimer += Time.deltaTime;
        if (difficultyTimer >= timeToUpgradeDifficulty)
        {
            difficultyTimer = 0f;
            SetDifficulty(currentDifficulty == GameDifficulty.Easy ? GameDifficulty.Normal : GameDifficulty.Hard);
        }
    }

    public void SetDifficulty(GameDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        ApplyDifficultySettings();
    }

    private void ApplyDifficultySettings()
    {
        switch (currentDifficulty)
        {
            case GameDifficulty.Easy:
                targetForwardSpeed = 6f;
                maxAnimSpeed = 1.5f;
                speedIncreaseRate = 0.005f;
                break;
            case GameDifficulty.Normal:
                targetForwardSpeed = 7.5f;
                maxAnimSpeed = 2.0f;
                speedIncreaseRate = 0.01f;
                break;
            case GameDifficulty.Hard:
                targetForwardSpeed = 9f;
                maxAnimSpeed = 2.5f;
                speedIncreaseRate = 0.015f;
                break;
        }
    }

    private void HandleAnimationSpeed()
    {
        if (animator != null && targetAnimSpeed < maxAnimSpeed)
        {
            targetAnimSpeed += speedIncreaseRate * Time.deltaTime;
            targetAnimSpeed = Mathf.Min(targetAnimSpeed, maxAnimSpeed);
        }
    }

    private void HandleMovement()
    {
        CheckGrounded();
        float targetX = (targetLane - 1) * laneDistance;
        Vector3 currentPos = transform.position;

        // Tỉ lệ tốc độ chuyển làn theo tốc độ chạy hiện tại để không bị "lì" khi game nhanh
        float effectiveLaneSpeed = laneSpeed * (1f + (currentAnimSpeed - 1f) * 0.5f);
        float newX = Mathf.Lerp(currentPos.x, targetX, Time.deltaTime * effectiveLaneSpeed);
        float newZ = currentPos.z + (currentForwardSpeed * Time.deltaTime);

        float newY = currentPos.y;

        if (!isJumping && !isSliding)
        {
            newY = Mathf.MoveTowards(newY, groundY, 20f * Time.deltaTime);
        }

        transform.position = new Vector3(newX, newY, newZ);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) Jump();
        if (Input.GetKeyDown(KeyCode.S)) Slide();
        if (Input.GetKeyDown(KeyCode.A)) ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.D)) ChangeLane(1);

        if (Input.GetMouseButtonDown(0)) startMousePos = Input.mousePosition;
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 swipeVector = (Vector2)Input.mousePosition - startMousePos;
            if (swipeVector.magnitude > minSwipeDistance)
            {
                if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
                    ChangeLane(swipeVector.x > 0 ? 1 : -1);
                else
                    if (swipeVector.y > 0) Jump(); else Slide();
            }
        }
    }

    void CheckGrounded()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 2f, groundLayer))
        {
            isGrounded = hit.distance <= 0.3f;
            if (isGrounded && !isJumping && !isSliding)
                groundY = hit.point.y;
        }
        else
        {
            isGrounded = false;
        }
    }

    void ChangeLane(int direction)
    {
        targetLane = Mathf.Clamp(targetLane + direction, 0, 2);
    }

    void Jump()
    {
        if (!isJumping && (isGrounded || isSliding))
        {
            if (animator != null)
            {
                animator.ResetTrigger("slide");
                animator.SetTrigger("jump");
                if (jumpSound != null) audioSource.PlayOneShot(jumpSound, jumpVolume);
                StartActionCoroutine(JumpRoutine());
            }
        }
    }

    void Slide()
    {
        if (!isSliding && (isGrounded || isJumping))
        {
            if (animator != null)
            {
                animator.ResetTrigger("jump");
                animator.SetTrigger("slide");
                if (slideSound != null) audioSource.PlayOneShot(slideSound, slideVolume);
                StartActionCoroutine(SlideRoutine());
            }
        }
    }

    void StartActionCoroutine(IEnumerator routine)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        ResetCollider();
        activeRoutine = StartCoroutine(routine);
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        isSliding = false;
        if (rb != null) rb.isKinematic = true;

        if (footCollider != null) footCollider.enabled = false;

        float timeElapsed = 0f;
        // Dùng vị trí Y hiện tại thay vì groundY cố định để tránh snap
        float startY = transform.position.y;
        float effectiveJumpDuration = jumpDuration;

        while (timeElapsed < effectiveJumpDuration)
        {
            timeElapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(timeElapsed / effectiveJumpDuration);
            float curveValue = jumpCurve.Evaluate(normalizedTime);
            float currentY = startY + (jumpHeight * curveValue);

            // Chỉ override Y, giữ X/Z do HandleMovement quản lý
            transform.position = new Vector3(transform.position.x, currentY, transform.position.z);
            yield return null;
        }

        // Snap về groundY khi hạ cánh
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        ResetCollider();
    }

    IEnumerator SlideRoutine()
    {
        isSliding = true;
        isJumping = false;
        if (rb != null) rb.isKinematic = true;

        // --- Mới: Tắt Foot Collider khi trượt để không bị vướng ---
        if (footCollider != null) footCollider.enabled = false;

        if (transform.position.y > groundY + 0.1f)
        {
            float fastFallSpeed = 40f * currentAnimSpeed;
            while (transform.position.y > groundY)
            {
                float newY = Mathf.MoveTowards(transform.position.y, groundY, fastFallSpeed * Time.deltaTime);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                yield return null;
            }
        }

        // Giữ đáy capsule cố định, thu nhỏ chiều cao từ trên xuống
        float newHeight = originalHeight * slideHeightRatio;
        float capsuleBottom = originalCenter.y - originalHeight / 2f;
        bodyCollider.height = newHeight;
        bodyCollider.center = new Vector3(originalCenter.x, capsuleBottom + newHeight / 2f, originalCenter.z);

        float timeElapsed = 0f;
        while (timeElapsed < slideDuration)
        {
            timeElapsed += Time.deltaTime;
            // Nếu dùng Mixamo, hãy đảm bảo animation Slide được set "Bake Into Pose" cho Y position
            yield return null;
        }

        ResetCollider();
    }

    void ResetCollider()
    {
        if (bodyCollider != null)
        {
            bodyCollider.height = originalHeight;
            bodyCollider.center = originalCenter;
            bodyCollider.radius = originalRadius;
            bodyCollider.enabled = true;
        }

        if (footCollider != null) footCollider.enabled = true;

        if (rb != null) rb.isKinematic = false; // Trả lại trạng thái vật lý bình thường

        isJumping = false;
        isSliding = false;
        activeRoutine = null;
    }

    // --- XỬ LÝ VA CHẠM VÀ ITEM ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            CheckObstacleCollision(other.gameObject);
        }
        else if (other.CompareTag("Coin"))
        {
            CollectItem(other.gameObject);
            if (GameOverController.Instance != null) GameOverController.Instance.AddCoin();
        }
        else if (other.CompareTag("Potion"))
        {
            CollectItem(other.gameObject);
            ActivatePotion();
        }
        else if (other.CompareTag("Thunder"))
        {
            CollectItem(other.gameObject);
            ActivateThunder();
        }
        else if (other.CompareTag("Shield"))
        {
            CollectItem(other.gameObject);
            if (GameOverController.Instance != null) GameOverController.Instance.AddShield();
        }
        else if (other.CompareTag("Heart"))
        {
            CollectItem(other.gameObject);
            if (GameOverController.Instance != null) GameOverController.Instance.AddHeart();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // Nếu đang bất tử, chúng ta tạm thời tắt va chạm vật lý với vật cản này
            if (isImmortal)
            {
                Physics.IgnoreCollision(collision.collider, bodyCollider, true);
                return;
            }

            CheckObstacleCollision(collision.gameObject);
        }
    }
    private void CollectItem(GameObject item)
    {
        if (coinSound != null) audioSource.PlayOneShot(coinSound, coinVolume);
        Destroy(item);
    }

    // --- LOGIC CHI TIẾT CÁC ITEM ---

    private void CheckObstacleCollision(GameObject obstacle)
    {
        // Nếu đang bất tử (vừa hồi sinh), bỏ qua hoàn toàn logic va chạm
        if (isImmortal) return;

        // Kiểm tra Khiên (Shield)
        if (GameOverController.Instance != null && GameOverController.Instance.TryUseShield())
        {
            Destroy(obstacle);
            return;
        }

        // Nếu không có khiên và không bất tử -> Game Over
        HandleGameOver();
    }
    private void ActivatePotion()
    {
        // Mỗi lần nhặt cộng thêm 10s
        potionTimer += 10f;

        // Cập nhật UI nếu cần (hiện tại logic yêu cầu không hiển thị số lượng nhặt mà chỉ cộng dồn thời gian)
        // Tuy nhiên, GameOverController.Instance.AddPotion() đang quản lý text nên ta có thể bỏ qua hoặc giữ lại tùy UI
        if (GameOverController.Instance != null) GameOverController.Instance.AddPotion();
    }

    private void ActivateThunder()
    {
        // Sấm sét: Biến mất Obstacle trước mặt 50 unit

        // Sử dụng OverlapBox để quét các vật cản trong dải 50f phía trước
        Collider[] hitColliders = Physics.OverlapBox(transform.position + transform.forward * 25f, new Vector3(laneDistance * 1.5f, 5f, 25f));

        bool playedSound = false;
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Obstacle"))
            {
                // 1. Tạo hiệu ứng sét đánh tại vị trí vật cản
                if (thunderVFX != null)
                {
                    // Lấy vị trí của vật cản và bắn tia sét từ trên trời xuống (Y cao hơn)
                    Vector3 vfxPos = hit.transform.position;
                    GameObject vfx = Instantiate(thunderVFX, vfxPos, Quaternion.identity);
                    Destroy(vfx, 2f); // Tự hủy hiệu ứng sau 2 giây
                }

                // 2. Chơi âm thanh Zap (chỉ chơi 1 lần đại diện hoặc chơi mỗi cái tùy user, ở đây ta chơi 1 lần cho cụm này)
                if (!playedSound && thunderClip != null)
                {
                    audioSource.PlayOneShot(thunderClip, thunderVolume);
                    playedSound = true;
                }

                // 3. Xóa vật cản
                Destroy(hit.gameObject);
            }
        }

        if (thunderImpulseSource != null)
        {
            // Tạo ra một xung lực để rung Camera
            thunderImpulseSource.GenerateImpulse();
        }
    }

    // --- LOGIC HỒI SINH (Gọi từ GameOverController) ---
    public void StartReviveProcess()
    {
        // Reset trạng thái vật lý
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }

        // BẬT LẠI COLLIDER (Cực kỳ quan trọng để nhặt được item)
        if (bodyCollider != null) bodyCollider.enabled = true;
        if (footCollider != null) footCollider.enabled = true;

        // Bật lại script và animator
        this.enabled = true;
        if (animator != null) animator.speed = currentAnimSpeed;

        // Bắt đầu Coroutine đi xuyên
        StartCoroutine(ImmortalRoutine());
    }
    IEnumerator ImmortalRoutine()
    {
        isImmortal = true;

        // Chuyển sang Trigger để đi xuyên vật cản nhưng vẫn nhặt được đồ
        if (bodyCollider != null) bodyCollider.isTrigger = true;
        if (footCollider != null) footCollider.isTrigger = true;

        // Tìm tất cả Renderer của nhân vật
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
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

        // KẾT THÚC: Trở lại bình thường
        if (bodyCollider != null) bodyCollider.isTrigger = false;
        if (footCollider != null) footCollider.isTrigger = false;

        foreach (var r in renderers)
        {
            if (r != null) r.enabled = true;
        }
        isImmortal = false;
    }
    private void HandleGameOver()
    {
        if (gameOverSound != null) audioSource.PlayOneShot(gameOverSound, gameOverVolume);
        if (BGMManager.Instance != null) BGMManager.Instance.StopMusic();

        StopAllCoroutines();
        ResetCollider();

        // Giữ vị trí nhưng ngừng di chuyển
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (bodyCollider != null) bodyCollider.enabled = false;
        if (animator != null) animator.speed = 0;

        if (GameOverController.Instance != null) GameOverController.Instance.GameOver();
        this.enabled = false;
    }
}