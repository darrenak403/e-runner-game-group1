using UnityEngine;

/// <summary>
/// Tự động set resolution về 720x1280 (portrait mobile HD) trước khi bất kỳ scene nào load.
/// Chạy cả trên Windows build để giữ giao diện mobile.
/// </summary>
public static class MobileResolutionInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetMobileResolution()
    {
        Screen.SetResolution(630, 1120, FullScreenMode.Windowed);
    }
}
