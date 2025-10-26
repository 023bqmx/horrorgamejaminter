using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class TriggerCutscene : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector director;

    [Header("Trigger Settings")]
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool disableColliderAfterTrigger = true;
    [SerializeField] private bool rewindBeforePlay = true;

    [Header("Camera Switching")]
    [SerializeField] private Camera mainCamera;       // กล้องหลัก
    [SerializeField] private Camera cinematicCamera;  // กล้อง Timeline

    [Header("Object To Destroy After Timeline")]
    [SerializeField] private GameObject objectToDestroy; // วัตถุที่จะลบหลัง Timeline จบ

    private bool hasPlayed = false;
    private Collider triggerCol;

    [SerializeField] GameObject grinlock;

    private void Awake()
    {
        triggerCol = GetComponent<Collider>();
        if (triggerCol && !triggerCol.isTrigger)
            triggerCol.isTrigger = true;

        if (cinematicCamera != null)
            cinematicCamera.enabled = false; // ปิดกล้องฉากตอนเริ่ม
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed) return;
        if (!director) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        hasPlayed = true;

        // ?? สลับกล้อง
        if (mainCamera) mainCamera.enabled = false;
        if (cinematicCamera) cinematicCamera.enabled = true;

        // ?? เล่น Timeline
        if (rewindBeforePlay)
        {
            director.time = 0;
            director.Evaluate();
        }
        director.Play();

        if (disableColliderAfterTrigger && triggerCol)
            triggerCol.enabled = false;

        // ? รอจน Timeline จบ
        StartCoroutine(WaitForTimelineEnd());
    }

    private IEnumerator WaitForTimelineEnd()
    {
        // รอจนเวลาใน Timeline ใกล้ถึง duration
        yield return new WaitUntil(() =>
            director.time >= director.duration - 0.05f || director.state != PlayState.Playing
        );

        yield return new WaitForSeconds(0.1f);

        // ?? ลบวัตถุหลังจบ Timeline
        if (objectToDestroy != null)
        {
            Destroy(objectToDestroy);
        }

        // ?? กลับกล้องหลัก
        grinlock.SetActive(true);
        if (mainCamera) mainCamera.enabled = true;
        if (cinematicCamera) cinematicCamera.enabled = false;
    }
}
