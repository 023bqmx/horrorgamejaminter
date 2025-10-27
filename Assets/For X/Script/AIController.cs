using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(Rigidbody))]
public class AIController : MonoBehaviour
{
    public GameObject Player;
    public Animator anim;
    public Transform headObj;

    [Header("NavMesh")]
    public NavMeshAgent agent;
    public Transform[] waypoints;
    public bool loopPatrol = true;

    [Header("Vision")]
    [Range(1f, 50f)] public float viewRadius = 15f;
    [Range(10f, 180f)] public float viewAngle = 90f;
    public LayerMask obstacleMask;

    [Header("Chase/Search")]
    public float searchDuration = 2.5f;
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public float arriveDistance = 0.6f;

    [Header("Face Tracking")]
    public SmileGateByMouthWideAuto smileGate;   // isSmiling
    public OpenSeeTrackingHealth trackingHealth; // isTracking

    // --- internal ---
    Transform player;
    int wpIndex = 0;
    Vector3 lastSeenPos;
    float lastSeenTime = float.NegativeInfinity;
    bool chasing, wasChasing, canSee, ignorePlayer;

    // Animator param (ต้องมี Bool ชื่อ IsChasing ใน Animator)
    [SerializeField] string idleState = "Idle";
    [SerializeField] string walkState = "Walk";
    [SerializeField] string runState = "Run";

    readonly int SpeedHash = Animator.StringToHash("Speed");
    float smoothedSpeed = 0f;
    bool prevIgnore, prevChasing;
    // ฮิสเทอรีซิสสำหรับ Idle/Walk
    const float WALK_ENTER = 0.20f;   // ต้อง > ค่านี้ถึงจะเดิน
    const float WALK_EXIT = 0.10f;   // ตกต่ำกว่านี้กลับไป Idle


    [SerializeField] float ignoreResumeDelay = 6f;   // เวลาหน่วงหลังยิ้ม
    Coroutine resumePatrolRoutine;                   // handle ของ coroutine

    [Header("Footsteps")]
    public AudioSource footSrc;              // ใส่ AudioSource (3D, playOnAwake=false, loop=false)
    public AudioClip[] footClips;            // คลิปเท้า 2–6 คลิปกำลังดี
    [Tooltip("ถือว่าเดินเมื่อเร็วเกินค่านี้ (m/s)")]
    public float stepSpeedThreshold = 0.1f;  // กันค่ากระพริบ
    [Tooltip("จำนวนก้าว/วินาที ที่ความเร็วเดิน (walkSpeed)")]
    public float walkStepRate = 1.8f;
    [Tooltip("จำนวนก้าว/วินาที ที่ความเร็ววิ่ง (runSpeed)")]
    public float runStepRate = 3.2f;

    [Tooltip("ช่วงความดังของแต่ละก้าว (จะคูณตามความเร็วด้วย)")]
    public Vector2 stepVolumeRange = new Vector2(0.6f, 1.0f);
    [Tooltip("ช่วง pitch เวลาเดิน")]
    public Vector2 pitchWalkRange = new Vector2(0.95f, 1.05f);
    [Tooltip("ช่วง pitch เวลา วิ่ง")]
    public Vector2 pitchRunRange = new Vector2(1.05f, 1.15f);

    float _stepTimer = 0f;
    int _lastStepIndex = -1;

    [Header("Footstep 3D Attenuation")]
    [SerializeField] AudioRolloffMode footRolloff = AudioRolloffMode.Logarithmic;
    [SerializeField] float footMinDistance = 1.8f;
    [SerializeField] float footMaxDistance = 22f;
    [SerializeField] bool zeroDoppler = true;
    [Header("Voice (Grinlock)")]
    public AudioSource voiceSrc;           // ใส่ AudioSource ไว้ที่ปาก/ตัวผี (3D, playOnAwake=false, loop=false)
    public AudioClip keepSmileClip;        // คลิป "keep smiling"
    public float keepSmileDelay = 2f;      // ดีเลย์หลังผู้เล่นยิ้ม
    public float keepSmileCooldown = 6f;   // กันสแปม
    [Tooltip("ต้องยังเมิน(ยิ้มอยู่)จนกว่าจะครบดีเลย์ถึงจะพูด")]
    public bool requireStillIgnoring = true;

    Coroutine _keepSmileRoutine;
    float _lastKeepSmileTime = -999f;

    [Header("Voice 3D Attenuation")]
    [SerializeField] AudioRolloffMode voiceRolloff = AudioRolloffMode.Logarithmic;
    [SerializeField] float voiceMinDistance = 2.5f;
    [SerializeField] float voiceMaxDistance = 35f;
    [SerializeField] bool voiceZeroDoppler = true;

    // ========== Catch / Jumpscare ==========
    [Header("Catch / Jumpscare")]
    [SerializeField] string jumpScareSceneName = "JumpscareScene"; // ตั้งชื่อซีนใน Inspector
    [SerializeField, Tooltip("ดีเลย์เล็กน้อยก่อนโหลดซีน")]
    float jumpLoadDelay = 0.15f;
    [SerializeField, Tooltip("ครั้งเดียวต่อการโดนจับ ป้องกันซ้อนกันหลายครั้ง")]
    bool oneShotCatch = true;

    bool _caught = false;

    [Header("Auto-Wire (FaceTracking Obj)")]
    [SerializeField] bool autoFindFaceTracking = true;
    [SerializeField] string faceTrackingObjectName = "FaceTracking Obj";
    [SerializeField] string faceTrackingTag = ""; // ว่างได้ ถ้าอยากใช้เฉพาะชื่อ
    [SerializeField] float faceTrackingBindTimeout = 3f;      // เผื่อโหลดช้า/ DDOL

    public bool IsChasing => chasing;
    public System.Action<bool> OnChaseStateChanged;

    private void Awake()
    {
        ConfigureFootAudio3D();
        ConfigureVoiceAudio3D();  // << เพิ่มบรรทัดนี้
        EnsureKinematicRigidbody(); // << เพิ่มบรรทัดนี้

        if (autoFindFaceTracking) StartCoroutine(AutoWireFaceTracking());
    }

    void Start()
    {
        Player = FirstPersonController.Instance ? FirstPersonController.Instance.gameObject : Player;
        if (!anim) anim = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        agent.speed = walkSpeed;
        if (waypoints != null && waypoints.Length > 0)
            TrySetDestination(waypoints[wpIndex].position);
    }

    void LateUpdate()
    {
        if (!headObj || !player) return;

        // อยู่ในกรวยมองเห็นแล้วค่อยหัน — จะเมินผู้เล่นอยู่ก็ยังหันได้
        if (IsWithinViewCone(player))
        {
            headObj.LookAt(Player.transform);
        }
    }
    bool IsWithinViewCone(Transform target)
    {
        var eye = headObj ? headObj.position : transform.position + Vector3.up * 1.6f;
        var dir = target.position - eye;

        // ระยะ
        if (dir.sqrMagnitude > viewRadius * viewRadius) return false;

        // มุม
        var forward = headObj ? headObj.forward : transform.forward;
        return Vector3.Angle(forward, dir.normalized) <= viewAngle * 0.5f;
    }
    bool IsSmilingInView()
    {
        if (!player) return false;
        if (!(smileGate && smileGate.isSmiling)) return false;
        if (!(trackingHealth && trackingHealth.isTracking)) return false;

        // ต้องอยู่ใน viewRadius + viewAngle เท่านั้น (ไม่เช็คสิ่งกีดขวางตามที่ต้องการ)
        return IsWithinViewCone(player);
    }
    void EnsureKinematicRigidbody()
    {
        // ถ้าไม่มี Rigidbody ที่ราก ให้ใส่ให้เอง (จำเป็นมากสำหรับรับ OnTrigger จากลูก)
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;   // ใช้ร่วมกับ NavMeshAgent ได้
        rb.useGravity = false;
    }

    void Update()
    {
        ignorePlayer = (smileGate && smileGate.isSmiling)   // gated signal
               && (trackingHealth && trackingHealth.isTracking)
               && IsSmilingInView();

        // --- Vision ---
        canSee = false;
        if (!ignorePlayer && player && CanSeePlayer(player, out var seenPos))
        {
            canSee = true;
            lastSeenPos = seenPos;
            lastSeenTime = Time.time;
        }

        if (ignorePlayer && !prevIgnore)
        {
            chasing = false;

            // หยุดเดินชั่วคราว แล้วตั้งเวลาค่อยกลับไปเดินเวย์พอยต์
            agent.ResetPath();
            agent.isStopped = true;
            if (resumePatrolRoutine != null) StopCoroutine(resumePatrolRoutine);
            resumePatrolRoutine = StartCoroutine(ResumePatrolAfterDelay(true)); // pickClosest: true

            if (prevChasing) TryScheduleKeepSmile();
        }
        else if (!ignorePlayer && prevIgnore)
        {
            // เลิกยิ้มระหว่างดีเลย์ -> ยกเลิกดีเลย์ แล้ว "ไปต่อ" ทันที
            if (resumePatrolRoutine != null) { StopCoroutine(resumePatrolRoutine); resumePatrolRoutine = null; }
            agent.isStopped = false;

            if (canSee && player)                // ถ้ากลับมาเห็นผู้เล่นแล้ว
            {
                chasing = true;
                agent.speed = runSpeed;
                TrySetDestination(player.position);
            }
            else                                 // ไม่เห็นผู้เล่น -> กลับไปเดินเวย์พอยต์
            {
                ResumePatrol(pickClosest: true); // หรือ false ถ้าอยากเดินต่อจาก wpIndex เดิม
            }
        }
        else if (canSee)
        {
            chasing = true;
        }
        else if (chasing && Time.time - lastSeenTime <= searchDuration)
        {
            // ยังคงโหมดค้นหาอยู่ช่วงสั้น ๆ หลังหลุดสายตา
            chasing = true;
        }
        else
        {
            chasing = false;
        }
        // เพิ่งเลิกไล่ (หมดเวลา search) และไม่ได้เมินอยู่ กลับเวย์พอยต์
        if (!chasing && prevChasing && !ignorePlayer)
        {
            ResumePatrol(pickClosest: false); // เดินต่อจาก wpIndex ปัจจุบัน
        }

        prevIgnore = ignorePlayer;

        if (chasing != prevChasing) OnChaseStateChanged?.Invoke(chasing);
        prevChasing = chasing;

        // --- Movement ---
        if (chasing)
        {
            agent.speed = runSpeed;
            var targetPos = canSee && player ? player.position : lastSeenPos;
            TrySetDestination(targetPos);

            if (!canSee && ReachedDestination())
                lastSeenTime = float.NegativeInfinity; // ให้รอบถัดไปหลุดจากโหมดค้นหา
        }
        else
        {
            agent.speed = walkSpeed;
            if (waypoints != null && waypoints.Length > 0 && ReachedDestination())
                GoToNextWaypoint();
        }


        UpdateLocomotionBySpeed();  // ย้ายให้มาก่อน
        UpdateAnim();               // แล้วค่อยป้อนพารามิเตอร์
        UpdateFootsteps();
    }
    // เรียกจาก Trigger/Collision ของ hitbox
    void TryCatchPlayerNow()
    {
        if (_caught && oneShotCatch) { Debug.Log("[AI] already caught (oneShot)"); return; }
        if (!chasing) { Debug.Log("[AI] hit but NOT chasing -> ignore"); return; }

        _caught = true;
        Debug.Log("[AI] Jumpscare!!!");
        if (agent) agent.isStopped = true;
        StartCoroutine(LoadJumpScareAfterDelay());
    }

    IEnumerator LoadJumpScareAfterDelay()
    {
        if (jumpLoadDelay > 0f) yield return new WaitForSeconds(jumpLoadDelay);
        Debug.Log($"[AI] Loading scene '{jumpScareSceneName}'");
        SceneManager.LoadScene(jumpScareSceneName, LoadSceneMode.Single);
    }

    // ---------- รองรับทั้ง Trigger และ Collision ----------
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[AI] OnTriggerEnter from {other.name} (layer={LayerMask.LayerToName(other.gameObject.layer)}), chasing={chasing}");
        if (IsPlayerCollider(other)) TryCatchPlayerNow();
    }

    // ช่วยเช็คว่าเป็นผู้เล่นจริงไหม (เผื่อโดนชิ้นส่วนอื่น)
    bool IsPlayerCollider(Collider c)
    {
        if (!c) return false;
        if (c.CompareTag("Player")) return true;                         // ให้ตั้ง Tag ผู้เล่น = "Player"
        if (c.GetComponentInParent<FirstPersonController>() != null) return true;
        return false;
    }
    void TryScheduleKeepSmile()
    {
        if (!voiceSrc || !keepSmileClip) return;
        if (Time.time - _lastKeepSmileTime < keepSmileCooldown) return;

        if (_keepSmileRoutine != null) StopCoroutine(_keepSmileRoutine);
        _keepSmileRoutine = StartCoroutine(KeepSmileRoutine());
    }

    IEnumerator KeepSmileRoutine()
    {
        float deadline = Time.time + keepSmileDelay;

        while (Time.time < deadline)
        {
            // ถ้าตั้งให้ต้อง "ยังเมินอยู่" แต่ผู้เล่นเลิกยิ้มก่อนครบเวลา -> ยกเลิก
            if (requireStillIgnoring && !ignorePlayer)
            {
                _keepSmileRoutine = null;
                yield break;
            }
            yield return null;
        }

        voiceSrc.PlayOneShot(keepSmileClip);
        _lastKeepSmileTime = Time.time;
        _keepSmileRoutine = null;
    }

    void UpdateAnim()
    {
        // ถ้ากำลัง transition อยู่ ปล่อยให้มันจบก่อน ไม่สั่ง CrossFade ทับ
        if (anim.IsInTransition(0)) return;

        if (chasing)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(runState))
                anim.CrossFadeInFixedTime(runState, 0f);   // เข้าทันที
            return;
        }

        // ----- ตัดสิน Walk/Idle ด้วยเกณฑ์ยืนจริง -----
        bool standing =
            agent == null ||
            agent.pathPending ||
            !agent.hasPath ||
            agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveDistance) ||
            agent.desiredVelocity.sqrMagnitude < 0.01f;

        var cur = anim.GetCurrentAnimatorStateInfo(0);
        if (standing)
        {
            if (!cur.IsName(idleState))
                anim.CrossFadeInFixedTime(idleState, 0.05f);
        }
        else
        {
            if (!cur.IsName(walkState))
                anim.CrossFadeInFixedTime(walkState, 0.05f);
        }
    }

    // -------- Vision helpers --------
    bool CanSeePlayer(Transform target, out Vector3 seenPoint)
    {
        var eye = headObj ? headObj.position : transform.position + Vector3.up * 1.6f;
        var dir = target.position - eye;
        seenPoint = target.position;

        if (dir.sqrMagnitude > viewRadius * viewRadius) return false; // radius
        var forward = headObj ? headObj.forward : transform.forward;
        if (Vector3.Angle(forward, dir.normalized) > viewAngle * 0.5f) return false; // FOV

        int mask = (obstacleMask.value == 0) ? ~0 : obstacleMask.value;
        if (Physics.SphereCast(eye, 0.1f, dir.normalized, out _, dir.magnitude, mask))
            return false;

        return true;
    }

    // -------- NavMesh helpers --------
    bool TrySetDestination(Vector3 pos)
    {
        if (!agent.isOnNavMesh) return false;
        if (NavMesh.SamplePosition(pos, out var hit, 5f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            return agent.SetDestination(hit.position);
        }
        return false;
    }

    bool ReachedDestination()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath) return false;                  // สำคัญ: ห้าม true ไม่งั้นข้ามเวย์พอยต์
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return false;
        return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveDistance);
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (loopPatrol)
        {
            wpIndex = (wpIndex + 1) % waypoints.Length;
        }
        else
        {
            wpIndex = Mathf.Min(wpIndex + 1, waypoints.Length - 1);
            if (wpIndex >= waypoints.Length - 1)
            {
                TrySetDestination(waypoints[wpIndex].position);
                return;
            }
        }
        TrySetDestination(waypoints[wpIndex].position);
    }
    void UpdateLocomotionBySpeed()
    {
        Vector3 v = agent ? agent.desiredVelocity : Vector3.zero; // ใช้ desired แทน velocity จริง
        v.y = 0f;
        float speed = v.magnitude;

        // ใช้เกณฑ์ standing เดียวกับข้างบน
        bool standing =
            agent == null ||
            agent.pathPending ||
            !agent.hasPath ||
            agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveDistance) ||
            agent.desiredVelocity.sqrMagnitude < 0.01f;

        if (standing) speed = 0f;

        float lerpRate = 10f;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, Time.deltaTime * lerpRate);
        smoothedSpeed = Mathf.Clamp(smoothedSpeed, 0f, runSpeed);
        anim.SetFloat(SpeedHash, smoothedSpeed);
    }
    void ResumePatrol(bool pickClosest)
    {
        agent.speed = walkSpeed;
        lastSeenTime = float.NegativeInfinity;
        agent.ResetPath();

        if (waypoints != null && waypoints.Length > 0)
        {
            if (pickClosest) wpIndex = ClosestWaypointIndex();
            TrySetDestination(waypoints[wpIndex].position);
        }
    }

    int ClosestWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return 0;
        int best = 0;
        float bestSqr = Mathf.Infinity;
        Vector3 p = transform.position;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = (waypoints[i].position - p).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = i; }
        }
        return best;
    }
    IEnumerator ResumePatrolAfterDelay(bool pickClosest)
    {
        yield return new WaitForSeconds(ignoreResumeDelay); // หน่วงตามที่ตั้งไว้ (ดีฟอลต์ 2 วิ)
        agent.isStopped = false;
        ResumePatrol(pickClosest);
        resumePatrolRoutine = null;
    }
    void UpdateFootsteps()
    {
        if (footSrc == null || footClips == null || footClips.Length == 0 || agent == null) return;

        // ใช้ "ความเร็วจริง" ของเอเจนต์ (แกน Y ตัดออก)
        Vector3 v = agent.velocity;
        v.y = 0f;
        float speed = v.magnitude;

        // ไม่เดิน/ถูกหยุด/ช้ามาก -> ไม่เล่น
        if (!agent.isOnNavMesh || agent.isStopped || speed <= stepSpeedThreshold)
        {
            _stepTimer = 0f;        // รีเซ็ตจะได้ไม่ยิงซ้อนทันทีตอนเริ่มเดินใหม่
            return;
        }

        // สเกลอัตราก้าวตามความเร็ว 0..runSpeed
        float t = Mathf.Clamp01(speed / Mathf.Max(0.01f, runSpeed)); // 0=ช้า, 1=เร็วสุด
        float stepsPerSec = Mathf.Lerp(walkStepRate, runStepRate, t);
        float period = 1f / Mathf.Max(0.01f, stepsPerSec);

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= period)
        {
            _stepTimer -= period;
            PlayFootstep(t);
        }
    }

    void PlayFootstep(float speed01)
    {
        // สุ่มคลิปแบบไม่ให้ซ้ำกับก้าวก่อนหน้า
        int idx = 0;
        if (footClips.Length == 1) idx = 0;
        else
        {
            do { idx = Random.Range(0, footClips.Length); }
            while (idx == _lastStepIndex);
        }
        _lastStepIndex = idx;

        // volume/pitch ไล่ตามความเร็ว (เดิน -> วิ่ง)
        float volBase = Mathf.Lerp(stepVolumeRange.x, stepVolumeRange.y, speed01);
        float pitchWalk = Random.Range(pitchWalkRange.x, pitchWalkRange.y);
        float pitchRun = Random.Range(pitchRunRange.x, pitchRunRange.y);
        float pitch = Mathf.Lerp(pitchWalk, pitchRun, speed01);

        footSrc.pitch = pitch;
        footSrc.PlayOneShot(footClips[idx], volBase);
    }

    void ConfigureFootAudio3D()
    {
        if (!footSrc) return;
        footSrc.spatialBlend = 1f;                         // 3D
        footSrc.rolloffMode = footRolloff;                 // Logarithmic/Custom
        footSrc.minDistance = footMinDistance;
        footSrc.maxDistance = footMaxDistance;
        if (zeroDoppler) footSrc.dopplerLevel = 0f;        // ตัด Doppler
                                                           // ถ้าใช้ Custom rolloff: footSrc.SetCustomCurve(AudioSourceCurveType.CustomRolloff, yourCurve);
    }
    void ConfigureVoiceAudio3D()
    {
        if (!voiceSrc) return;
        voiceSrc.spatialBlend = 1f;            // 3D
        voiceSrc.rolloffMode = voiceRolloff;
        voiceSrc.minDistance = voiceMinDistance;
        voiceSrc.maxDistance = voiceMaxDistance;
        if (voiceZeroDoppler) voiceSrc.dopplerLevel = 0f;
    }
    IEnumerator AutoWireFaceTracking()
    {
        float t = 0f;
        while (t < faceTrackingBindTimeout && (!smileGate || !trackingHealth))
        {
            TryWireFaceTrackingOnce();
            if (smileGate && trackingHealth) yield break;
            t += Time.unscaledDeltaTime;
            yield return null; // รอเฟรมถัดไป เผื่อ DDOL/Prefab สร้างช้า
        }
    }

    void TryWireFaceTrackingOnce()
    {
        // 1) พยายามหา Player ก่อน (tag = Player)
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 2) ถ้ามี Player แล้ว ลองหาลูกชื่อ FaceTracking Obj ใต้ PlayerCapsule
        if (player)
        {
            var ft = FindChildRecursive(player, faceTrackingObjectName);
            if (ft)
            {
                if (!smileGate) smileGate = ft.GetComponentInChildren<SmileGateByMouthWideAuto>(true);
                if (!trackingHealth) trackingHealth = ft.GetComponentInChildren<OpenSeeTrackingHealth>(true);
            }
            // ยังไม่เจอจากชื่อลูก? ลองทั้งกิ่งใต้ PlayerCapsule
            if (!smileGate || !trackingHealth)
            {
                if (!smileGate) smileGate = player.GetComponentInChildren<SmileGateByMouthWideAuto>(true);
                if (!trackingHealth) trackingHealth = player.GetComponentInChildren<OpenSeeTrackingHealth>(true);
            }
        }

        // 3) เผื่อคุณตั้ง Tag ให้ FaceTracking Obj ไว้
        if ((!smileGate || !trackingHealth) && !string.IsNullOrEmpty(faceTrackingTag))
        {
            var tagged = GameObject.FindGameObjectWithTag(faceTrackingTag);
            if (tagged)
            {
                if (!smileGate) smileGate = tagged.GetComponentInChildren<SmileGateByMouthWideAuto>(true);
                if (!trackingHealth) trackingHealth = tagged.GetComponentInChildren<OpenSeeTrackingHealth>(true);
            }
        }

        // 4) Fallback: หาโดยชื่อ/ทุกที่ (รวม DDOL) ด้วย Resources API
        if (!smileGate || !trackingHealth)
        {
            var go = FindLoadedObjectByName(faceTrackingObjectName);
            if (go)
            {
                if (!smileGate) smileGate = go.GetComponentInChildren<SmileGateByMouthWideAuto>(true);
                if (!trackingHealth) trackingHealth = go.GetComponentInChildren<OpenSeeTrackingHealth>(true);
            }
        }
    }

    // หา child ตามชื่อแบบ recursive
    Transform FindChildRecursive(Transform root, string name)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;
        foreach (Transform c in root)
        {
            if (c.name == name) return c;
            var hit = FindChildRecursive(c, name);
            if (hit) return hit;
        }
        return null;
    }

    // หา GameObject ที่ "ถูกโหลดแล้ว" (รวม DontDestroyOnLoad) ตามชื่อ
    GameObject FindLoadedObjectByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var g in all)
        {
            if (!g) continue;
            // ตัด prefab asset ออก ให้เหลือเฉพาะ object ที่ active ใน scene/ DDOL
            if (g.hideFlags != HideFlags.None) continue;
            if (g.name == name) return g;
        }
        return null;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, .9f, 0f, .25f);
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        Vector3 l = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 r = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.color = new Color(1f, .6f, 0f, .6f);
        Gizmos.DrawLine(transform.position, transform.position + l * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + r * viewRadius);
    }
}
