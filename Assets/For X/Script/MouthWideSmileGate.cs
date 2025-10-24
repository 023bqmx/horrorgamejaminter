using UnityEngine;
using OpenSee;

public class SmileGateByMouthWideAuto : MonoBehaviour
{
    [Header("OpenSee")]
    public OpenSee.OpenSee openSee;

    [Header("Smile Detection")]
    [Range(0f, 1f)] public float threshold = 0.50f;     // ��������� MouthWide >= threshold
    [Range(0f, 1f)] public float minConfidence = 0.25f; // ��Ҥ���������蹢�鹵�Ӣͧ�˹��

    [Header("Smile Budget / Cooldown")]
    [Tooltip("����������ͧ���٧�ش����Թҷ� (������ǵѴ�ѹ��)")]
    public float maxSmileSeconds = 2f;
    [Tooltip("���Ҥ�Ŵ�ǹ���ѧ�����ѧ����")]
    public float cooldownSeconds = 2f;
    [Tooltip("�ѵ�ҿ�鹾�ѧ��������Թҷ� (�����ҧ���������/��Ŵ�ǹ�)")]
    public float regenPerSecond = 1.5f;

    // ===== Output (����������) =====
    /// <summary>�ѭ�ҳ�Ժ�ҡ OpenSee (��� threshold/confidence) � ���١�ӡѴ����</summary>
    public bool rawSmiling { get; private set; }
    /// <summary>�ѭ�ҳ��� "͹حҵ����" ����Ѻ������ (�����Ե/��Ŵ�ǹ�) � ���ѹ���Ѻ AI</summary>
    public bool isSmiling { get; private set; }
    /// <summary>���ѧ��Ŵ�ǹ������������</summary>
    public bool isCoolingDown => Time.time < _lockUntil;
    /// <summary>��ѧ�����������ͤԴ�� 0..1 (���������/�� UI ��)</summary>
    public float charge01 => Mathf.Clamp01(_charge / Mathf.Max(0.0001f, maxSmileSeconds));

    // Debug
    public int usedFaceIndex = -1;
    public float lastMouthWide;
    public float lastConfidence;

    // ===== Internal =====
    float _charge;                // �Թҷ�������������
    float _lockUntil = -999f;     // ������ԡ��Ŵ�ǹ�

    void Awake()
    {
        _charge = maxSmileSeconds;   // ���������ѧ
    }

    void Update()
    {
        // 1) ��ҹ˹����Фӹǳ "�ѭ�ҳ�Ժ"
        var data = openSee ? openSee.trackingData : null;
        if (data == null || data.Length == 0)
        {
            usedFaceIndex = -1;
            lastConfidence = 0f;
            lastMouthWide = 0f;
            rawSmiling = false;

            // ��ش˹��: ���絾�ѧ��Ѻ���� ���͡ѹ�Դʶҹ�
            HardResetToFull();
            return;
        }

        int best = -1; float bestConf = minConfidence;
        for (int i = 0; i < data.Length; i++)
        {
            float conf = Mean(data[i].confidence);
            if (conf > bestConf) { best = i; bestConf = conf; }
        }
        if (best < 0)
        {
            usedFaceIndex = -1;
            lastConfidence = 0f;
            lastMouthWide = 0f;
            rawSmiling = false;

            HardResetToFull();
            return;
        }

        usedFaceIndex = best;
        lastConfidence = bestConf;
        lastMouthWide = data[best].features.MouthWide; // PascalCase �ç�Ѻ����

        rawSmiling = lastMouthWide >= threshold;

        // 2) Gate �����ͨԡ���� (state machine)
        if (isCoolingDown)
        {
            // ��Ŵ�ǹ�����: �������� ���鹾�ѧ��Ѻ������
            isSmiling = false;
            RegenTowardFull();
        }
        else
        {
            if (isSmiling)
            {
                // ����� session ��������
                if (rawSmiling)
                {
                    _charge -= Time.deltaTime;
                    if (_charge <= 0f)
                    {
                        // ����ѧ �Դ���� + �������Ŵ�ǹ�
                        _charge = 0f;
                        isSmiling = false;
                        _lockUntil = Time.time + cooldownSeconds;
                    }
                }
                else
                {
                    // ��ԡ������ҧ�ѹ �Դ���� ������������
                    isSmiling = false;
                }
            }
            else
            {
                // �ѧ�������� session ����
                if (rawSmiling && IsFullCharge())
                {
                    // ͹حҵ���������੾�е͹ "����ش/����ѧ" ��ҹ��
                    isSmiling = true;
                    _charge = Mathf.Max(0f, _charge - Time.deltaTime); // �ѡ�ѹ������á
                }
                else
                {
                    // ��������� ��鹡�Ѻ������
                    RegenTowardFull();
                }
            }
        }

        // 3) Debug �ء ~10 ���
        if (Time.frameCount % 10 == 0)
            Debug.Log($"[SmileGate] idx={usedFaceIndex} mw={lastMouthWide:F3} conf={lastConfidence:F2} raw={rawSmiling} gate={isSmiling} cd={isCoolingDown} charge={charge01:F2}");
    }

    // ===== Helpers =====
    float Mean(float[] a)
    {
        if (a == null || a.Length == 0) return 0f;
        float s = 0f; for (int i = 0; i < a.Length; i++) s += a[i];
        return s / a.Length;
    }

    void RegenTowardFull()
    {
        if (_charge < maxSmileSeconds)
        {
            _charge += regenPerSecond * Time.deltaTime;
            if (_charge >= maxSmileSeconds) _charge = maxSmileSeconds;
        }
    }

    bool IsFullCharge() => _charge >= maxSmileSeconds - 1e-4f;

    void HardResetToFull()
    {
        isSmiling = false;
        _lockUntil = -999f;
        _charge = maxSmileSeconds;
    }
}
