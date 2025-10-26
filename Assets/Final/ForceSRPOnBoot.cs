using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ForceSRPOnBoot : MonoBehaviour
{
    [Header("Assign your URP-PSX Render Pipeline Asset here")]
    public RenderPipelineAsset rpAsset;

    [Header("Quality")]
    [Tooltip("ถ้าต้องการล็อก Quality level (ดูลำดับใน Project Settings > Quality)")]
    public int forceQualityIndex = -1;

    [Header("Housekeeping")]
    public bool clearSavedQualityOnce = true; // เคยรันแล้วโดนจำค่าเก่าไว้

    void Awake()
    {
        if (rpAsset == null) rpAsset = Resources.Load<RenderPipelineAsset>("RP/URP_PSX");
        // ลบ Quality ที่ Unity เคยจำไว้บนเครื่อง (ถ้ามี)
        if (clearSavedQualityOnce && PlayerPrefs.HasKey("UnityGraphicsQuality"))
            PlayerPrefs.DeleteKey("UnityGraphicsQuality");

        if (forceQualityIndex >= 0)
            QualitySettings.SetQualityLevel(forceQualityIndex, true);

        if (rpAsset != null)
        {
            // ตั้ง SRP ทั้ง global และ per-quality
            GraphicsSettings.defaultRenderPipeline = rpAsset; // global
            QualitySettings.renderPipeline = rpAsset;         // override ของ quality ปัจจุบัน
        }

        // เปิด Post-Processing flags เผื่อโดนปิด
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            if (!urp.supportsHDR) urp.supportsHDR = true;
            if (!urp.supportsCameraDepthTexture) urp.supportsCameraDepthTexture = true;
            if (!urp.supportsCameraOpaqueTexture) urp.supportsCameraOpaqueTexture = true;
        }

        Debug.Log($"[ForceSRPOnBoot] quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} " +
                  $"defaultRP={GraphicsSettings.defaultRenderPipeline?.name ?? "null"} " +
                  $"qualityRP={QualitySettings.renderPipeline?.name ?? "null"} " +
                  $"currentRP={GraphicsSettings.currentRenderPipeline?.name ?? "null"}");
    }
}
