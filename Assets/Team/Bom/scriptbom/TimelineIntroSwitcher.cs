using UnityEngine;
using UnityEngine.Playables;

public class TimelineIntroSwitcher : MonoBehaviour
{
    [Header("Refs")]
    public PlayableDirector director;   // ตัว Timeline ในซีนนี้
    public Camera introCamera;          // กล้องที่ Timeline ใช้ (เช่น Camera2)
    public Camera playerCamera;         // กล้องเล่นจริงของผู้เล่น

    [Header("Optional (ล็อกอินพุต/ซ่อน UI ระหว่างอินโทร)")]
    public Behaviour[] disableDuringIntro;     // ใส่สคริปต์ควบคุมที่อยากปิดระหว่างอินโทร เช่น PlayerController
    public GameObject[] disableObjectsDuringIntro; // ใส่ HUD/Canvas ที่อยากซ่อนไว้ก่อน

    void Start()
    {
        // สภาพเริ่ม: ใช้กล้องอินโทร
        if (playerCamera) playerCamera.enabled = false;
        if (introCamera) introCamera.enabled = true;

        foreach (var b in disableDuringIntro) if (b) b.enabled = false;
        foreach (var g in disableObjectsDuringIntro) if (g) g.SetActive(false);

        if (director)
        {
            director.stopped += OnIntroFinished;
            director.Play(); // ให้ Timeline เล่นทันทีที่เข้าซีน
        }
        else
        {
            SwitchToPlayer();
        }
    }

    void OnDestroy()
    {
        if (director != null) director.stopped -= OnIntroFinished;
    }

    void OnIntroFinished(PlayableDirector d) => SwitchToPlayer();

    void SwitchToPlayer()
    {
        if (introCamera) introCamera.enabled = false;
        if (playerCamera) playerCamera.enabled = true;

        foreach (var b in disableDuringIntro) if (b) b.enabled = true;
        foreach (var g in disableObjectsDuringIntro) if (g) g.SetActive(true);
    }
}
