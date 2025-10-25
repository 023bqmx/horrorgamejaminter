using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class Sceneloadbomja : MonoBehaviour
{

    [Header("Assign a RenderPipelineAsset for this scene")]
    [Tooltip("Leave null to use Built-in; assign URP/HDRP asset to switch.")]
    [SerializeField] private RenderPipelineAsset pipelineForThisScene;

    [Header("Advanced")]
    [SerializeField] private bool useQualityOverrideInstead = true; // safer toggle

    private RenderPipelineAsset _prevDefault;
    private RenderPipelineAsset _prevQualityOverride;

    void OnEnable()
    {
        // Remember current settings
        _prevDefault = GraphicsSettings.defaultRenderPipeline;
        _prevQualityOverride = QualitySettings.renderPipeline;

        // Apply this scene's pipeline (either via Quality override or Default)
        if (useQualityOverrideInstead)
            QualitySettings.renderPipeline = pipelineForThisScene; // per-quality override
        else
            GraphicsSettings.defaultRenderPipeline = pipelineForThisScene; // global default
    }

    void OnDisable()
    {
        // Restore previous settings
        if (useQualityOverrideInstead)
            QualitySettings.renderPipeline = _prevQualityOverride;
        else
            GraphicsSettings.defaultRenderPipeline = _prevDefault;
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game.");
        Application.Quit();
    }
    
}