using UnityEngine;

public class FaceIcon : MonoBehaviour
{
    [SerializeField] private SmileGateByMouthWideAuto smile;
    [SerializeField] private OpenSeeTrackingHealth face;

    [SerializeField] private GameObject normalFace;
    [SerializeField] private GameObject smileFace;
    [SerializeField] private GameObject trackingLost; // เดิมชื่อ Close เปลี่ยนให้สื่อ

    void Reset()
    {
        smile ??= GetComponent<SmileGateByMouthWideAuto>();
        face ??= GetComponent<OpenSeeTrackingHealth>();
    }

    void Awake() => Reset();

    void Update()
    {
        bool tracking = face && face.isTracking;
        bool smiling = smile && smile.isSmiling;

        if (!tracking)
        {
            SetActiveSafe(trackingLost, true);
            SetActiveSafe(normalFace, true);
            SetActiveSafe(smileFace, false);
            return;
        }

        SetActiveSafe(trackingLost, false);
        SetActiveSafe(smileFace, smiling);
        SetActiveSafe(normalFace, !smiling);
    }

    static void SetActiveSafe(GameObject go, bool state)
    {
        if (go && go.activeSelf != state) go.SetActive(state);
    }
}
