using UnityEngine;

public class SkyboxChanger : MonoBehaviour
{
    [Header("Danh sách Skybox Material")]
    public Material[] skyboxMaterials; // Kéo các Material skybox vào đây trong Inspector

    [Header("Thời gian chuyển đổi")]
    public float changeInterval = 30f;    // Bao nhiêu giây thì đổi 1 lần
    public float transitionDuration = 2f; // Thời gian blend mượt (giây)
    public bool randomOrder = false;       // true = ngẫu nhiên, false = theo thứ tự

    private int currentIndex = 0;
    private float timer = 0f;

    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Material fromMaterial;
    private Material toMaterial;

    // Material trung gian để blend 2 skybox không qua màu đen
    private Material blendedMaterial;

    void Start()
    {
        if (skyboxMaterials == null || skyboxMaterials.Length == 0)
        {
            Debug.LogWarning("SkyboxChanger: Chưa có Skybox Material nào được gán!");
            enabled = false;
            return;
        }

        RenderSettings.skybox = skyboxMaterials[currentIndex];
        DynamicGI.UpdateEnvironment();
    }

    void Update()
    {
        if (skyboxMaterials.Length <= 1) return;

        if (isTransitioning)
        {
            HandleTransition();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= changeInterval)
        {
            timer = 0f;
            StartTransitionToNext();
        }
    }

    private void StartTransitionToNext()
    {
        fromMaterial = skyboxMaterials[currentIndex];

        // Chọn skybox tiếp theo
        if (randomOrder)
        {
            int nextIndex;
            do { nextIndex = Random.Range(0, skyboxMaterials.Length); }
            while (nextIndex == currentIndex && skyboxMaterials.Length > 1);
            currentIndex = nextIndex;
        }
        else
        {
            currentIndex = (currentIndex + 1) % skyboxMaterials.Length;
        }

        toMaterial = skyboxMaterials[currentIndex];

        // Tạo material trung gian để blend 2 skybox trực tiếp (không qua màu đen)
        // Cả 2 Material phải dùng cùng loại Shader (ví dụ đều là Skybox/6 Sided)
        if (blendedMaterial != null) Destroy(blendedMaterial);
        blendedMaterial = new Material(fromMaterial);
        RenderSettings.skybox = blendedMaterial;

        isTransitioning = true;
        transitionTimer = 0f;
    }

    private void HandleTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / transitionDuration);

        // Blend mượt tất cả property của 2 material (màu, texture, float...)
        // mà không qua màu đen
        blendedMaterial.Lerp(fromMaterial, toMaterial, t);
        DynamicGI.UpdateEnvironment();

        if (t >= 1f)
        {
            // Chuyển sang material thật để tránh giữ material tạm trong memory
            RenderSettings.skybox = toMaterial;
            Destroy(blendedMaterial);
            blendedMaterial = null;
            isTransitioning = false;
        }
    }

    void OnDestroy()
    {
        // Dọn dẹp material tạm khi script bị hủy
        if (blendedMaterial != null) Destroy(blendedMaterial);
    }

    /// <summary>
    /// Reset về skybox đầu tiên (gọi khi Game Over / Restart)
    /// </summary>
    public void ResetToFirst()
    {
        if (blendedMaterial != null) Destroy(blendedMaterial);
        blendedMaterial = null;
        isTransitioning = false;

        currentIndex = 0;
        timer = 0f;

        if (skyboxMaterials.Length > 0)
        {
            RenderSettings.skybox = skyboxMaterials[0];
            DynamicGI.UpdateEnvironment();
        }
    }
}
