using StarterAssets;
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

    readonly int IsChasingHash = Animator.StringToHash("IsChasing");
    readonly int SpeedHash = Animator.StringToHash("Speed");
    float smoothedSpeed = 0f;
    // ฮิสเทอรีซิสสำหรับ Idle/Walk
    const float WALK_ENTER = 0.20f;   // ต้อง > ค่านี้ถึงจะเดิน
    const float WALK_EXIT = 0.10f;   // ตกต่ำกว่านี้กลับไป Idle

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
        if (canSee && headObj && player)
        {
            headObj.LookAt(Player.transform);
        }
    }

    void Update()
    {
        ignorePlayer = (smileGate && smileGate.isSmiling) &&
                       (trackingHealth && trackingHealth.isTracking);

        // --- Vision ---
        canSee = false;
        if (!ignorePlayer && player && CanSeePlayer(player, out var seenPos))
        {
            canSee = true;
            lastSeenPos = seenPos;
            lastSeenTime = Time.time;
        }

        // --- State update ---
        if (ignorePlayer)
        {
            chasing = false;
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
        // ป้อนค่าที่กราฟต้องใช้
        anim.SetBool(IsChasingHash, chasing);
        anim.SetFloat(SpeedHash, smoothedSpeed);
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
        // ความเร็วระนาบจริงของ Agent (ตัดแกน Y กันเนิน)
        Vector3 v = agent ? agent.velocity : Vector3.zero;
        v.y = 0f;
        float speed = v.magnitude;

        // ทำให้ลื่นขึ้น ไม่กระตุกเวลาขึ้น/ลง
        float lerpRate = 10f;                // ไวขึ้น/ลง ปรับได้
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, Time.deltaTime * lerpRate);

        // จำกัดช่วงและส่งเข้า Animator
        smoothedSpeed = Mathf.Clamp(smoothedSpeed, 0f, runSpeed);
        anim.SetFloat(SpeedHash, smoothedSpeed);
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
