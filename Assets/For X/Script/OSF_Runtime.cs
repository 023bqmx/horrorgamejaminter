using UnityEngine;
using System.IO;
using OSFLauncher = OpenSee.OpenSeeLauncher;   // type alias ¡Ñ¹ª¹ª×èÍ namespace
using OSFComponent = OpenSee.OpenSee;

[DefaultExecutionOrder(-1000)]
public class OSF_Runtime : MonoBehaviour
{
    public static OSF_Runtime I;

    [SerializeField] OSFLauncher launcher;
    [SerializeField] OSFComponent osf;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (!launcher) launcher = GetComponent<OSFLauncher>();
        if (!osf) osf = GetComponent<OSFComponent>();
        launcher.openSeeTarget = osf;

        var root = Path.Combine(Application.streamingAssetsPath, "OpenSeeFace");
        launcher.exePath = Path.Combine(root, "Binary", "facetracker.exe");
        launcher.modelPath = Path.Combine(root, "models");
        launcher.dynamicPort = true;
        launcher.cameraIndex = -1; // ãËé UI ä»à«çµ
    }

    public void StartTracking() => launcher?.StartTracker();
    public void StopTracking() => launcher?.StopTracker();
}
