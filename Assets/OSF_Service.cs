using UnityEngine;
using System;

public static class OSF_Service
{
    public static OpenSee.OpenSee OpenSee { get; private set; }
    public static OpenSee.OpenSeeLauncher Launcher { get; private set; }
    public static SmileGateByMouthWideAuto SmileGate { get; private set; }
    public static OpenSeeTrackingHealth TrackingHealth { get; private set; }
    public static GameObject Root { get; private set; }

    public static bool Ready =>
        OpenSee && Launcher && SmileGate && TrackingHealth;

    public static event Action OnReady;

    // เรียกครั้งเดียวจากวัตถุ FaceTracking Obj
    public static void Register(GameObject root)
    {
        Root = root;

        OpenSee = root.GetComponentInChildren<OpenSee.OpenSee>(true);
        Launcher = root.GetComponentInChildren<OpenSee.OpenSeeLauncher>(true);

        // ให้มีแน่ ๆ (ถ้าไม่มีให้เติม) แล้วผูกเข้ากับ OpenSee
        SmileGate = root.GetComponentInChildren<SmileGateByMouthWideAuto>(true)
                    ?? root.AddComponent<SmileGateByMouthWideAuto>();
        TrackingHealth = root.GetComponentInChildren<OpenSeeTrackingHealth>(true)
                         ?? root.AddComponent<OpenSeeTrackingHealth>();

        if (OpenSee)
        {
            if (SmileGate) SmileGate.openSee = OpenSee;
            if (TrackingHealth) TrackingHealth.openSee = OpenSee;
        }

        UnityEngine.Object.DontDestroyOnLoad(root);
        Debug.Log("[OSF] Service READY");
        OnReady?.Invoke();
    }
}
