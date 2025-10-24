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

    // Animator param (��ͧ�� Bool ���� IsChasing � Animator)
    [SerializeField] string idleState = "Idle";
    [SerializeField] string walkState = "Walk";
    [SerializeField] string runState = "Run";

    readonly int SpeedHash = Animator.StringToHash("Speed");
    float smoothedSpeed = 0f;
    bool prevIgnore, prevChasing;
    // ������ի������Ѻ Idle/Walk
    const float WALK_ENTER = 0.20f;   // ��ͧ > ��ҹ��֧���Թ
    const float WALK_EXIT = 0.10f;   // ����ӡ��ҹ���Ѻ� Idle


    [SerializeField] float ignoreResumeDelay = 6f;   // ����˹�ǧ��ѧ����
    Coroutine resumePatrolRoutine;                   // handle �ͧ coroutine

    [Header("Footsteps")]
    public AudioSource footSrc;              // ��� AudioSource (3D, playOnAwake=false, loop=false)
    public AudioClip[] footClips;            // ��Ի��� 2�6 ��Ի���ѧ��
    [Tooltip("�������Թ����������Թ��ҹ�� (m/s)")]
    public float stepSpeedThreshold = 0.1f;  // �ѹ��ҡ�о�Ժ
    [Tooltip("�ӹǹ����/�Թҷ� �����������Թ (walkSpeed)")]
    public float walkStepRate = 1.8f;
    [Tooltip("�ӹǹ����/�Թҷ� ������������� (runSpeed)")]
    public float runStepRate = 3.2f;

    [Tooltip("��ǧ�����ѧ�ͧ���С��� (�Фٳ����������Ǵ���)")]
    public Vector2 stepVolumeRange = new Vector2(0.6f, 1.0f);
    [Tooltip("��ǧ pitch �����Թ")]
    public Vector2 pitchWalkRange = new Vector2(0.95f, 1.05f);
    [Tooltip("��ǧ pitch ���� ���")]
    public Vector2 pitchRunRange = new Vector2(1.05f, 1.15f);

    float _stepTimer = 0f;
    int _lastStepIndex = -1;

    [Header("Footstep 3D Attenuation")]
    [SerializeField] AudioRolloffMode footRolloff = AudioRolloffMode.Logarithmic;
    [SerializeField] float footMinDistance = 1.8f;
    [SerializeField] float footMaxDistance = 22f;
    [SerializeField] bool zeroDoppler = true;
    private void Awake()
    {
        ConfigureFootAudio3D();
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

        // ����㹡����ͧ������Ǥ����ѹ � ����Թ������������ѧ�ѹ��
        if (IsWithinViewCone(player))
        {
            headObj.LookAt(Player.transform);
        }
    }
    bool IsWithinViewCone(Transform target)
    {
        var eye = headObj ? headObj.position : transform.position + Vector3.up * 1.6f;
        var dir = target.position - eye;

        // ����
        if (dir.sqrMagnitude > viewRadius * viewRadius) return false;

        // ���
        var forward = headObj ? headObj.forward : transform.forward;
        return Vector3.Angle(forward, dir.normalized) <= viewAngle * 0.5f;
    }
    bool IsSmilingInView()
    {
        if (!player) return false;
        if (!(smileGate && smileGate.isSmiling)) return false;
        if (!(trackingHealth && trackingHealth.isTracking)) return false;

        // ��ͧ����� viewRadius + viewAngle ��ҹ�� (�������觡մ��ҧ�������ͧ���)
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

            // ��ش�Թ���Ǥ��� ���ǵ�����Ҥ��¡�Ѻ��Թ�����µ�
            agent.ResetPath();
            agent.isStopped = true;
            if (resumePatrolRoutine != null) StopCoroutine(resumePatrolRoutine);
            resumePatrolRoutine = StartCoroutine(ResumePatrolAfterDelay(true)); // pickClosest: true
        }
        else if (!ignorePlayer && prevIgnore)
        {
            // ��ԡ���������ҧ������ -> ¡��ԡ������ ���� "仵��" �ѹ��
            if (resumePatrolRoutine != null) { StopCoroutine(resumePatrolRoutine); resumePatrolRoutine = null; }
            agent.isStopped = false;

            if (canSee && player)                // ��ҡ�Ѻ����繼���������
            {
                chasing = true;
                agent.speed = runSpeed;
                TrySetDestination(player.position);
            }
            else                                 // �����繼����� -> ��Ѻ��Թ�����µ�
            {
                ResumePatrol(pickClosest: true); // ���� false �����ҡ�Թ��ͨҡ wpIndex ���
            }
        }
        else if (canSee)
        {
            chasing = true;
        }
        else if (chasing && Time.time - lastSeenTime <= searchDuration)
        {
            // �ѧ���������������ǧ��� � ��ѧ��ش��µ�
            chasing = true;
        }
        else
        {
            chasing = false;
        }
        // �����ԡ��� (������� search) ����������Թ���� ��Ѻ�����µ�
        if (!chasing && prevChasing && !ignorePlayer)
        {
            ResumePatrol(pickClosest: false); // �Թ��ͨҡ wpIndex �Ѩ�غѹ
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
                lastSeenTime = float.NegativeInfinity; // ����ͺ�Ѵ���ش�ҡ��������
        }
        else
        {
            agent.speed = walkSpeed;
            if (waypoints != null && waypoints.Length > 0 && ReachedDestination())
                GoToNextWaypoint();
        }


        UpdateLocomotionBySpeed();  // ��������ҡ�͹
        UpdateAnim();               // ���Ǥ��»�͹����������
        UpdateFootsteps();
    }

    void UpdateAnim()
    {
        // ��ҡ��ѧ transition ���� ���������ѹ����͹ ������ CrossFade �Ѻ
        if (anim.IsInTransition(0)) return;

        if (chasing)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(runState))
                anim.CrossFadeInFixedTime(runState, 0f);   // ��ҷѹ��
            return;
        }

        // ----- �Ѵ�Թ Walk/Idle ����ࡳ���׹��ԧ -----
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

        // occlusion (�� SphereCast ��� � ��鷹���� Raycast)
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
        if (!agent.hasPath) return false;                  // �Ӥѭ: ���� true ����鹢��������µ�
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
        Vector3 v = agent ? agent.desiredVelocity : Vector3.zero; // �� desired ᷹ velocity ��ԧ
        v.y = 0f;
        float speed = v.magnitude;

        // ��ࡳ�� standing ���ǡѺ��ҧ��
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
        yield return new WaitForSeconds(ignoreResumeDelay); // ˹�ǧ����������� (�տ�ŵ� 2 ��)
        agent.isStopped = false;
        ResumePatrol(pickClosest);
        resumePatrolRoutine = null;
    }
    void UpdateFootsteps()
    {
        if (footSrc == null || footClips == null || footClips.Length == 0 || agent == null) return;

        // �� "�������Ǩ�ԧ" �ͧ��ਹ�� (᡹ Y �Ѵ�͡)
        Vector3 v = agent.velocity;
        v.y = 0f;
        float speed = v.magnitude;

        // ����Թ/�١��ش/����ҡ -> ������
        if (!agent.isOnNavMesh || agent.isStopped || speed <= stepSpeedThreshold)
        {
            _stepTimer = 0f;        // ���絨�������ԧ��͹�ѹ�յ͹������Թ����
            return;
        }

        // ����ѵ�ҡ��ǵ���������� 0..runSpeed
        float t = Mathf.Clamp01(speed / Mathf.Max(0.01f, runSpeed)); // 0=���, 1=�����ش
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
        // ������ԻẺ�������ӡѺ���ǡ�͹˹��
        int idx = 0;
        if (footClips.Length == 1) idx = 0;
        else
        {
            do { idx = Random.Range(0, footClips.Length); }
            while (idx == _lastStepIndex);
        }
        _lastStepIndex = idx;

        // volume/pitch ������������� (�Թ -> ���)
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
        if (zeroDoppler) footSrc.dopplerLevel = 0f;        // �Ѵ Doppler
                                                           // ����� Custom rolloff: footSrc.SetCustomCurve(AudioSourceCurveType.CustomRolloff, yourCurve);
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
