// LeverPickupProgress.cs
using UnityEngine;

public class LeverPickupProgress : MonoBehaviour
{
    [Range(0, 999f)]
    public float secondsAccumulated = 0f;   // เวลาที่กดค้างสะสมไว้ (ไม่หายเมื่อปล่อยปุ่ม)
}
