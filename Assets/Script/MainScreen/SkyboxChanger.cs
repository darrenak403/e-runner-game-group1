using UnityEngine;

public class SkyboxChanger : MonoBehaviour
{
    [Header("Danh sách Skybox Material")]
    public Material[] skyboxMaterials;

    [Header("Thời gian chuyển đổi")]
    public float changeInterval     = 30f; // Giây giữa mỗi lần chuyển
    public float transitionDuration = 2f;  // Giây để crossfade: A mờ dần, B sáng dần đồng thời

    [Header("Blend Shaders (kéo từ Assets/Shader vào)")]
    public Shader blend6SidedShader;    // Custom/SkyboxBlend6Sided
    public Shader blendPanoramicShader; // Custom/SkyboxBlendPanoramic
    public Shader blendCubemapShader;   // Custom/SkyboxBlendCubemap

    private int   currentIndex    = 0;
    private float timer           = 0f;

    private bool     isTransitioning = false;
    private float    transitionTimer = 0f;
    private Material fromMaterial;
    private Material toMaterial;
    private Material blendMaterial;

    // Loại skybox đang blend
    private enum SkyType { SixSided, Panoramic, Cubemap, Fallback }
    private SkyType currentBlendType;

    void Start()
    {
        if (skyboxMaterials == null || skyboxMaterials.Length == 0)
        {
            enabled = false;
            return;
        }

        // Tự tìm shader nếu chưa kéo vào Inspector
        if (blend6SidedShader    == null) blend6SidedShader    = Shader.Find("Custom/SkyboxBlend6Sided");
        if (blendPanoramicShader == null) blendPanoramicShader = Shader.Find("Custom/SkyboxBlendPanoramic");
        if (blendCubemapShader   == null) blendCubemapShader   = Shader.Find("Custom/SkyboxBlendCubemap");

        RenderSettings.skybox = skyboxMaterials[0];
        DynamicGI.UpdateEnvironment();
    }

    void Update()
    {
        if (skyboxMaterials.Length <= 1) return;

        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= changeInterval)
        {
            timer = 0f;
            BeginTransition();
        }
    }

    // ─── Bắt đầu crossfade ───────────────────────────────────────────────────

    void BeginTransition()
    {
        fromMaterial = skyboxMaterials[currentIndex];
        currentIndex = (currentIndex + 1) % skyboxMaterials.Length;
        toMaterial   = skyboxMaterials[currentIndex];

        if (blendMaterial != null) Destroy(blendMaterial);

        currentBlendType = DetectType(fromMaterial);
        Debug.Log($"[SkyboxChanger] BeginTransition: shader='{fromMaterial.shader.name}' → type={currentBlendType}");

        blendMaterial = currentBlendType switch
        {
            SkyType.SixSided  => Create6SidedBlend(),
            SkyType.Panoramic => CreatePanoramicBlend(),
            SkyType.Cubemap   => CreateCubemapBlend(),
            _                 => new Material(fromMaterial) // Fallback: Material.Lerp
        };

        RenderSettings.skybox = blendMaterial;
        transitionTimer  = 0f;
        isTransitioning  = true;
    }

    // ─── Cập nhật blend mỗi frame ────────────────────────────────────────────

    void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float t      = Mathf.Clamp01(transitionTimer / transitionDuration);
        float smooth = Mathf.SmoothStep(0f, 1f, t); // ease in-out

        if (currentBlendType == SkyType.Fallback)
            blendMaterial.Lerp(fromMaterial, toMaterial, smooth);
        else
            blendMaterial.SetFloat("_Blend", smooth); // shader tự blend A->B

        if (t >= 1f)
        {
            RenderSettings.skybox = toMaterial;
            Destroy(blendMaterial);
            blendMaterial   = null;
            isTransitioning = false;
            DynamicGI.UpdateEnvironment();
        }
    }

    // ─── Tạo blend material ───────────────────────────────────────────────────

    Material Create6SidedBlend()
    {
        var mat = new Material(blend6SidedShader);

        // Copy 6 textures từ A (suffix A)
        mat.SetTexture("_FrontTexA", fromMaterial.GetTexture("_FrontTex"));
        mat.SetTexture("_BackTexA",  fromMaterial.GetTexture("_BackTex"));
        mat.SetTexture("_LeftTexA",  fromMaterial.GetTexture("_LeftTex"));
        mat.SetTexture("_RightTexA", fromMaterial.GetTexture("_RightTex"));
        mat.SetTexture("_UpTexA",    fromMaterial.GetTexture("_UpTex"));
        mat.SetTexture("_DownTexA",  fromMaterial.GetTexture("_DownTex"));

        // Copy 6 textures từ B (suffix B)
        mat.SetTexture("_FrontTexB", toMaterial.GetTexture("_FrontTex"));
        mat.SetTexture("_BackTexB",  toMaterial.GetTexture("_BackTex"));
        mat.SetTexture("_LeftTexB",  toMaterial.GetTexture("_LeftTex"));
        mat.SetTexture("_RightTexB", toMaterial.GetTexture("_RightTex"));
        mat.SetTexture("_UpTexB",    toMaterial.GetTexture("_UpTex"));
        mat.SetTexture("_DownTexB",  toMaterial.GetTexture("_DownTex"));

        mat.SetFloat("_Blend",    0f);
        mat.SetFloat("_Exposure", fromMaterial.GetFloat("_Exposure"));
        mat.SetFloat("_Rotation", fromMaterial.GetFloat("_Rotation"));
        mat.SetColor("_Tint",     fromMaterial.GetColor("_Tint"));

        return mat;
    }

    Material CreatePanoramicBlend()
    {
        var mat = new Material(blendPanoramicShader);

        mat.SetTexture("_TexA", fromMaterial.GetTexture("_Tex"));
        mat.SetTexture("_TexB", toMaterial.GetTexture("_Tex"));

        mat.SetFloat("_ExposureA", SafeGetFloat(fromMaterial, "_Exposure", 1f));
        mat.SetFloat("_ExposureB", SafeGetFloat(toMaterial,   "_Exposure", 1f));
        mat.SetFloat("_RotationA", SafeGetFloat(fromMaterial, "_Rotation", 0f));
        mat.SetFloat("_RotationB", SafeGetFloat(toMaterial,   "_Rotation", 0f));
        mat.SetColor("_TintA",     SafeGetColor(fromMaterial, "_Tint", new Color(.5f,.5f,.5f,.5f)));
        mat.SetColor("_TintB",     SafeGetColor(toMaterial,   "_Tint", new Color(.5f,.5f,.5f,.5f)));

        mat.SetFloat("_Blend", 0f);
        return mat;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    Material CreateCubemapBlend()
    {
        var mat = new Material(blendCubemapShader);

        mat.SetTexture("_TexA", fromMaterial.GetTexture("_Tex"));
        mat.SetTexture("_TexB", toMaterial.GetTexture("_Tex"));

        mat.SetFloat("_ExposureA", SafeGetFloat(fromMaterial, "_Exposure", 1f));
        mat.SetFloat("_ExposureB", SafeGetFloat(toMaterial,   "_Exposure", 1f));
        mat.SetFloat("_RotationA", SafeGetFloat(fromMaterial, "_Rotation", 0f));
        mat.SetFloat("_RotationB", SafeGetFloat(toMaterial,   "_Rotation", 0f));
        mat.SetColor("_TintA",     SafeGetColor(fromMaterial, "_Tint", new Color(.5f,.5f,.5f,.5f)));
        mat.SetColor("_TintB",     SafeGetColor(toMaterial,   "_Tint", new Color(.5f,.5f,.5f,.5f)));

        mat.SetFloat("_Blend", 0f);
        return mat;
    }

    SkyType DetectType(Material mat)
    {
        string shaderName = mat.shader.name;
        if (shaderName.Contains("6 Sided") && blend6SidedShader != null)
            return SkyType.SixSided;
        if ((shaderName.Contains("Panoramic") || shaderName.Contains("Equirect")) && blendPanoramicShader != null)
            return SkyType.Panoramic;
        if (shaderName.Contains("Cubemap") && blendCubemapShader != null)
            return SkyType.Cubemap;
        return SkyType.Fallback;
    }

    float SafeGetFloat(Material mat, string prop, float fallback)
        => mat.HasProperty(prop) ? mat.GetFloat(prop) : fallback;

    Color SafeGetColor(Material mat, string prop, Color fallback)
        => mat.HasProperty(prop) ? mat.GetColor(prop) : fallback;

    void OnDestroy()
    {
        if (blendMaterial != null) Destroy(blendMaterial);
    }

    /// <summary>Reset về skybox đầu (gọi khi Restart)</summary>
    public void ResetToFirst()
    {
        if (blendMaterial != null) { Destroy(blendMaterial); blendMaterial = null; }
        isTransitioning = false;
        currentIndex    = 0;
        timer           = 0f;

        if (skyboxMaterials.Length > 0)
        {
            RenderSettings.skybox = skyboxMaterials[0];
            DynamicGI.UpdateEnvironment();
        }
    }
}
