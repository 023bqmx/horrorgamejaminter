using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

        // occlusion (ใช้ SphereCast เล็ก ๆ ให้ทนกว่า Raycast)
        if (Physics.SphereCast(eye, 0.1f, dir.normalized, out _, dir.magnitude, obstacleMask))
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
