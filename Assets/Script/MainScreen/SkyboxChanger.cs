using UnityEngine;

/// <summary>
/// Chuyển đổi Skybox mượt mà bằng custom blend shader.
/// Hỗ trợ: Skybox/6 Sided  và  Skybox/Panoramic.
/// Đặt script này lên bất kỳ GameObject nào trong scene MainScreen.
/// </summary>
public class SkyboxChanger : MonoBehaviour
{
    [Header("Danh sách Skybox Material")]
    public Material[] skyboxMaterials;

    [Header("Thời gian chuyển đổi")]
    public float changeInterval    = 30f;  // đổi bầu trời mỗi N giây
    public float transitionDuration = 3f;  // thời gian fade (giây)
    public bool  randomOrder        = false;

    // ---- Runtime ----
    private int      currentIndex = 0;
    private float    timer        = 0f;
    private bool     isTransitioning = false;
    private float    transitionTimer = 0f;
    private Material blendMat;   // material trung gian (persistent)

    // ---- Shader & property caches ----
    private bool isPanoramic; // true = Panoramic, false = 6 Sided

    // 6-Sided property names (Unity standard)
    private static readonly string[] Props6A = { "_FrontTexA","_BackTexA","_LeftTexA","_RightTexA","_UpTexA","_DownTexA" };
    private static readonly string[] Props6B = { "_FrontTexB","_BackTexB","_LeftTexB","_RightTexB","_UpTexB","_DownTexB" };
    private static readonly string[] Props6  = { "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"  };

    // -------------------------------------------------------
    void Start()
    {
        if (skyboxMaterials == null || skyboxMaterials.Length == 0)
        {
            Debug.LogWarning("SkyboxChanger: Chưa gán Skybox Material nào!");
            enabled = false;
            return;
        }

        // Tự detect loại shader của material đầu tiên
        string shaderName = skyboxMaterials[0].shader.name;
        isPanoramic = shaderName.Contains("Panoramic");

        // Tạo BLEND shader tương ứng
        Shader blendShader = isPanoramic
            ? Shader.Find("Custom/SkyboxBlendPanoramic")
            : Shader.Find("Custom/SkyboxBlend6Sided");

        if (blendShader == null)
        {
            Debug.LogError($"SkyboxChanger: Không tìm thấy shader blend! " +
                           $"Hãy đảm bảo file .shader tồn tại trong project.");
            enabled = false;
            return;
        }

        blendMat = new Material(blendShader);
        blendMat.hideFlags = HideFlags.HideAndDontSave;

        // Load skybox đầu tiên vào slot A (blend = 0 → chỉ A hiện)
        CopyToSlotA(skyboxMaterials[currentIndex]);
        CopyToSlotB(skyboxMaterials[currentIndex]); // B = A để tránh grey
        blendMat.SetFloat("_Blend", 0f);
        CopyCommon(skyboxMaterials[currentIndex]);

        RenderSettings.skybox = blendMat;
        DynamicGI.UpdateEnvironment();
    }

    // -------------------------------------------------------
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
            BeginTransition();
        }
    }

    // -------------------------------------------------------
    private void BeginTransition()
    {
        // Skybox hiện tại (A) đã ở slot A — chỉ cần load skybox mới vào slot B
        int nextIndex = randomOrder
            ? GetRandomOther(currentIndex)
            : (currentIndex + 1) % skyboxMaterials.Length;

        currentIndex = nextIndex;
        CopyToSlotB(skyboxMaterials[currentIndex]);
        CopyCommon(skyboxMaterials[currentIndex]);

        blendMat.SetFloat("_Blend", 0f); // bắt đầu từ A
        isTransitioning  = true;
        transitionTimer  = 0f;
    }

    private void HandleTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / transitionDuration);

        // Smooth easing (ease in-out)
        float smooth = t * t * (3f - 2f * t);
        blendMat.SetFloat("_Blend", smooth);
        DynamicGI.UpdateEnvironment();

        if (t >= 1f)
        {
            // Chuyển B → A để sẵn sàng transition tiếp theo
            // (không swap RenderSettings → không giật)
            SwapBtoA();
            blendMat.SetFloat("_Blend", 0f);
            isTransitioning = false;
        }
    }

    // -------------------------------------------------------
    // Helpers

    private void CopyCommon(Material src)
    {
        if (src.HasProperty("_Tint"))     blendMat.SetColor("_Tint", src.GetColor("_Tint"));
        if (src.HasProperty("_Exposure")) blendMat.SetFloat("_Exposure", src.GetFloat("_Exposure"));
        if (src.HasProperty("_Rotation")) blendMat.SetFloat("_Rotation", src.GetFloat("_Rotation"));
    }

    private void CopyToSlotA(Material src)
    {
        if (isPanoramic)
        {
            if (src.HasProperty("_MainTex")) blendMat.SetTexture("_TexA", src.GetTexture("_MainTex"));
        }
        else
        {
            for (int i = 0; i < Props6.Length; i++)
                if (src.HasProperty(Props6[i])) blendMat.SetTexture(Props6A[i], src.GetTexture(Props6[i]));
        }
    }

    private void CopyToSlotB(Material src)
    {
        if (isPanoramic)
        {
            if (src.HasProperty("_MainTex")) blendMat.SetTexture("_TexB", src.GetTexture("_MainTex"));
        }
        else
        {
            for (int i = 0; i < Props6.Length; i++)
                if (src.HasProperty(Props6[i])) blendMat.SetTexture(Props6B[i], src.GetTexture(Props6[i]));
        }
    }

    /// <summary>Sau khi transition xong, copy slot B → slot A để chuẩn bị vòng tiếp.</summary>
    private void SwapBtoA()
    {
        if (isPanoramic)
        {
            blendMat.SetTexture("_TexA", blendMat.GetTexture("_TexB"));
        }
        else
        {
            for (int i = 0; i < Props6.Length; i++)
                blendMat.SetTexture(Props6A[i], blendMat.GetTexture(Props6B[i]));
        }
    }

    private int GetRandomOther(int current)
    {
        if (skyboxMaterials.Length == 1) return 0;
        int next;
        do { next = Random.Range(0, skyboxMaterials.Length); }
        while (next == current);
        return next;
    }

    // -------------------------------------------------------
    void OnDestroy()
    {
        if (blendMat != null) DestroyImmediate(blendMat);
    }

    /// <summary>Gọi khi Game Over / Restart để reset về skybox đầu tiên.</summary>
    public void ResetToFirst()
    {
        currentIndex    = 0;
        timer           = 0f;
        isTransitioning = false;

        if (blendMat == null || skyboxMaterials.Length == 0) return;
        CopyToSlotA(skyboxMaterials[0]);
        CopyToSlotB(skyboxMaterials[0]);
        blendMat.SetFloat("_Blend", 0f);
        RenderSettings.skybox = blendMat;
        DynamicGI.UpdateEnvironment();
    }
}
