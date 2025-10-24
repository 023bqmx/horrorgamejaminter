using UnityEngine;
using UnityEngine.SceneManagement; // เปลี่ยนซีน + ฟัง event ตอนซีนโหลดเสร็จ
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoEndHandler : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;

    [Header("Cursor Settings for Next Scene")]
    [Tooltip("ซ่อนลูกศรเมาส์เมื่อเข้าสู่ซีนใหม่")]
    public bool hideCursor = true;
    [Tooltip("ล็อกเมาส์เมื่อเข้าสู่ซีนใหม่")]
    public bool lockCursor = true;
    [Tooltip("โหมดการล็อกเมาส์ (แนะนำ Locked)")]
    public CursorLockMode lockMode = CursorLockMode.Locked;

    private VideoPlayer videoPlayer;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.isLooping = false;
    }

    void OnEnable()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
        SceneManager.sceneLoaded += OnSceneLoaded; // ฟังตอนซีนใหม่โหลดเสร็จ
    }

    void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        // โหลดซีนปลายทาง
        SceneManager.LoadScene(sceneToLoad);
        // หมายเหตุ: ไม่ต้องซ่อนเมาส์ตรงนี้ เพราะเดี๋ยวให้ทำใน OnSceneLoaded เพื่อชัวร์ว่าเกิดในซีนใหม่
    }

    // ถูกเรียกเมื่อซีนใหม่โหลดเสร็จ
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ซ่อน/ล็อกเฉพาะเมื่อเข้าซีนที่เราตั้งไว้
        if (!string.IsNullOrEmpty(sceneToLoad) && scene.name == sceneToLoad)
        {
            if (hideCursor) Cursor.visible = false;
            if (lockCursor) Cursor.lockState = lockMode;
        }
    }
}
