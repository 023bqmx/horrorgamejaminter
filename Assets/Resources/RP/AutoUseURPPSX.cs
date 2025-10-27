// Assets/Scripts/AutoUseURPPSX.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AutoUseURPPSX
{
    // รันก่อนแม้แต่ Splash Screen -> กันคุณภาพ/กราฟิกเปลี่ยนเอง
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void ForceSRP()
    {
        // โหลด RP asset จาก Resources (โฟลเดอร์ในโปรเจกต์)
        var rp = Resources.Load<RenderPipelineAsset>("RP/URP_PSX");
        if (rp == null) { Debug.LogWarning("[AutoUseURPPSX] Not found Resources/RP/URP_PSX"); return; }

        GraphicsSettings.defaultRenderPipeline = rp;
        QualitySettings.renderPipeline = rp; // ปิดทาง quality override

        // เปิดกล้องช่วยที่ URP-PSX มักต้องใช้
        if (rp is UniversalRenderPipelineAsset urp)
        {
            urp.supportsCameraDepthTexture = true;
            urp.supportsCameraOpaqueTexture = true;
            urp.supportsHDR = true;
        }

        Debug.Log($"[AutoUseURPPSX] RP={rp.name}  qualityRP={QualitySettings.renderPipeline?.name}  currentRP={GraphicsSettings.currentRenderPipeline?.name}");
    }
}
