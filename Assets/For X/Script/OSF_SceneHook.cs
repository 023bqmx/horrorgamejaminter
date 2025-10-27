using UnityEngine;

public class OSF_SceneHook : MonoBehaviour
{
    public bool autoStart = true;
    void OnEnable() { if (autoStart) OSF_Runtime.I?.StartTracking(); }
    void OnDisable() { OSF_Runtime.I?.StopTracking(); }
}
