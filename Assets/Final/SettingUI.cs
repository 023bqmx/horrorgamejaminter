using UnityEngine;

public class SettingUI : MonoBehaviour
{
    public GameObject settingUI;
    void Start()
    {
        settingUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            settingUI.SetActive(false);
        }
    }

    public void Open()
    {
        settingUI.SetActive(true);
    }
    public void Quit()
    {
#if UNITY_EDITOR
        // ใน Editor: หยุด Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // WebGL ไม่มีการปิดแอปจริง ๆ — ปิดแท็บ/โหลดหน้าเปล่าแทน
        Application.OpenURL("about:blank");
#else
        // Windows / macOS / Linux / Android
        Application.Quit();
#endif
    }

    // (ออปชัน) ปิดแบบหน่วงเวลา เผื่อเล่นเสียง/เอฟเฟกต์ก่อนออก
    public void QuitAfter(float seconds) => Invoke(nameof(Quit), seconds);
}
