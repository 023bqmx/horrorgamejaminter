using UnityEngine;

[RequireComponent(typeof(Collider))] // ต้องมี Collider เพื่อรับคลิกในโลก 3D
public class ClickPlayToggle : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;   // ลาก AudioSource มาใส่ (อยู่ตรงภาพหรือที่อื่นก็ได้)
    public AudioClip musicClip;       // ลากไฟล์เพลงมาใส่
    [Range(0f,1f)] public float volume = 1f;
    public bool loop = true;
    [Tooltip("ถ้า true: เมื่อกดอีกครั้งจะเริ่มเล่นใหม่จากต้นเพลง (แทนที่จะ Pause/Resume)")]
    public bool restartOnToggle = false;

    private void Reset()
    {
        // ลองหา AudioSource บนตัวเองอัตโนมัติ
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            // ถ้าไม่ได้อ้างอิง จะสร้าง AudioSource บนตัวเองให้เลย
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // ตั้งค่าพื้นฐาน (2D เสียง)
        audioSource.spatialBlend = 0f;   // 0 = 2D, ถ้าอยากให้มีตำแหน่งในโลกตั้งเป็น 1
        audioSource.loop = loop;
        audioSource.volume = volume;
        if (musicClip != null) audioSource.clip = musicClip;
    }

    // รับคลิกด้วย Collider (ง่ายสุด)
    private void OnMouseDown()
    {
        ToggleMusic();
    }

    public void ToggleMusic()
    {
        if (audioSource == null || (audioSource.clip == null && musicClip == null))
        {
            Debug.LogWarning("[ClickPlayToggle] Missing AudioSource or AudioClip.");
            return;
        }

        // เผื่อกรณีเปลี่ยน clip ใน Inspector ขณะรัน
        if (audioSource.clip == null && musicClip != null)
            audioSource.clip = musicClip;

        audioSource.loop = loop;
        audioSource.volume = volume;

        if (audioSource.isPlaying)
        {
            // คลิกซ้ำ: หยุด
            if (restartOnToggle) { audioSource.Stop(); }  // หยุดและรีสตาร์ทในครั้งต่อไป
            else                 { audioSource.Pause(); } // พัก แล้วคลิกอีกครั้งจะ Resume
        }
        else
        {
            if (restartOnToggle) audioSource.time = 0f; // เริ่มใหม่จากต้นเพลง
            audioSource.Play();
        }
    }
}
