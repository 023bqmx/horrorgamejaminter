using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ProximityCameraShake : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("เว้นว่างเพื่อใช้ Camera.main")]
    public Transform cameraTarget;

    [Header("Discover Enemies")]
    public bool autoFindByTag = true;
    public string enemyTag = "Enemy";
    [Tooltip("ค้นหาศัตรูใหม่ทุกกี่วินาที")]
    public float refreshEnemiesEvery = 1.0f;
    public List<Transform> extraTargets = new();

    [Header("Distance -> Shake")]
    public float outerRadius = 12f;     // ไกลกว่า = ไม่สั่น
    public float innerRadius = 3.5f;    // ใกล้กว่านี้ = สั่นสุด

    [Header("Shake Amount")]
    public float posAmplitude = 0.06f;  // ระยะสั่น (local)
    public float rotAmplitude = 0.7f;   // องศาสั่น
    public float frequency = 12f;       // ความถี่
    public float followLerp = 18f;      // ความลื่น

    [Header("Scale by Enemy Speed (optional)")]
    public bool scaleByEnemySpeed = true;
    public float enemyMoveThreshold = 0.1f;
    public float enemySpeedMax = 6f;

    // ---- internals ----
    Vector3 _shakePosLast = Vector3.zero;   // offset ที่ใส่ไปเฟรมก่อน
    Quaternion _shakeRotLast = Quaternion.identity;

    float _seedX, _seedY, _seedR;
    float _refreshTimer;
    readonly List<Transform> _targets = new();

    void Awake()
    {
        if (!cameraTarget) cameraTarget = Camera.main ? Camera.main.transform : null;

        _seedX = Random.value * 1000f;
        _seedY = Random.value * 2000f;
        _seedR = Random.value * 3000f;

        ForceRefreshTargets();
    }

    void OnEnable()
    {
        // ล้าง offset เผื่อถูกปิด/เปิด component
        _shakePosLast = Vector3.zero;
        _shakeRotLast = Quaternion.identity;
    }

    void Update()
    {
        // รีเฟรชรายการศัตรูเป็นช่วง ๆ
        _refreshTimer -= Time.deltaTime;
        if (_refreshTimer <= 0f)
        {
            ForceRefreshTargets();
            _refreshTimer = refreshEnemiesEvery;
        }
    }

    void LateUpdate()
    {
        if (!cameraTarget) return;

        // 1) คำนวณความแรงจากศัตรูที่ใกล้ที่สุด
        float strength = 0f;
        foreach (var t in _targets)
        {
            if (!t) continue;
            float d = Vector3.Distance(transform.position, t.position);
            if (d > outerRadius) continue;

            float dist01 = Mathf.Clamp01((outerRadius - d) / Mathf.Max(0.0001f, outerRadius - innerRadius));
            float s = dist01;

            if (scaleByEnemySpeed)
            {
                float sp = EstimateEnemySpeed(t);
                float move01 = Mathf.Clamp01(Mathf.InverseLerp(enemyMoveThreshold, enemySpeedMax, sp));
                s *= move01; // ยืนเฉย ๆ ก็แทบไม่สั่น
            }
            if (s > strength) strength = s;
        }

        // 2) สร้าง offset ใหม่จาก noise
        float tNow = Time.time * frequency;
        float nX = (Mathf.PerlinNoise(_seedX, tNow) - 0.5f) * 2f;
        float nY = (Mathf.PerlinNoise(_seedY, tNow) - 0.5f) * 2f;
        float nR = (Mathf.PerlinNoise(_seedR, tNow) - 0.5f) * 2f;

        Vector3 posTarget = new Vector3(nX, nY, 0f) * (posAmplitude * strength);
        Vector3 rotEulerTarget = new Vector3(nY * 0.6f, nX * 0.6f, nR) * (rotAmplitude * strength);
        Quaternion rotTarget = Quaternion.Euler(rotEulerTarget);

        // 3) ไล่เข้าหาเป้าหมายอย่างลื่น
        float k = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        Vector3 posNew = Vector3.Lerp(_shakePosLast, posTarget, k);
        Quaternion rotNew = Quaternion.Slerp(_shakeRotLast, rotTarget, k);

        // 4) นำ “offset เก่า” ออกจากทรานส์ฟอร์ม แล้วใส่ “offset ใหม่” เข้าไป
        //    ทำใน LateUpdate เพื่อไม่ไปชนกับสคริปต์เมาส์ที่หมุนกล้องใน Update
        cameraTarget.localPosition = (cameraTarget.localPosition - _shakePosLast) + posNew;
        cameraTarget.localRotation = (cameraTarget.localRotation * Quaternion.Inverse(_shakeRotLast)) * rotNew;

        // 5) เก็บ offset ล่าสุดไว้ใช้ลบในเฟรมถัดไป
        _shakePosLast = posNew;
        _shakeRotLast = rotNew;
    }

    void OnDisable()
    {
        if (!cameraTarget) return;
        // ถอด offset ที่ค้างไว้
        cameraTarget.localPosition -= _shakePosLast;
        cameraTarget.localRotation = cameraTarget.localRotation * Quaternion.Inverse(_shakeRotLast);
        _shakePosLast = Vector3.zero;
        _shakeRotLast = Quaternion.identity;
    }

    // -------- helpers --------
    void ForceRefreshTargets()
    {
        _targets.Clear();
        if (autoFindByTag && !string.IsNullOrEmpty(enemyTag))
        {
            var found = GameObject.FindGameObjectsWithTag(enemyTag);
            for (int i = 0; i < found.Length; i++) _targets.Add(found[i].transform);
        }
        for (int i = 0; i < extraTargets.Count; i++)
            if (extraTargets[i]) _targets.Add(extraTargets[i]);
    }

    float EstimateEnemySpeed(Transform enemy)
    {
        var ag = enemy.GetComponent<NavMeshAgent>();
        if (ag) return new Vector3(ag.velocity.x, 0, ag.velocity.z).magnitude;

        var rb = enemy.GetComponent<Rigidbody>();
        if (rb) return new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

        var anim = enemy.GetComponent<Animator>();
        if (anim && anim.HasParameter("Speed", AnimatorControllerParameterType.Float))
            return anim.GetFloat("Speed");

        return 0f;
    }
}

static class AnimatorExt
{
    public static bool HasParameter(this Animator anim, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }
}
