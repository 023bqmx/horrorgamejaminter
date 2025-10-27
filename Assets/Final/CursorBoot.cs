using UnityEngine;

#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_INPUT_SYSTEM_EXISTS
using StarterAssets;
#endif

[DefaultExecutionOrder(-1000)]
public class CursorBoot : MonoBehaviour
{
    [SerializeField] bool startUnlocked = true;
    [SerializeField] bool showCursor = true;
    [SerializeField] KeyCode toggleKey = KeyCode.Escape; // กดสลับระหว่างเล่น

    void Awake()
    {
        if (startUnlocked) Unlock();
        // ปิด “หมุนกล้องด้วยเมาส์” ของ Starter Assets ตอนเริ่ม
#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_INPUT_SYSTEM_EXISTS
        var inputs = FindObjectOfType<StarterAssetsInputs>(true);
        if (inputs) inputs.cursorInputForLook = false;
#endif
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Starter Assets จะพยายามล็อกอีกครั้งตรงนี้ เราก็ปลดทับอีกที
        if (hasFocus && startUnlocked) Unlock();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool unlock = Cursor.lockState == CursorLockMode.Locked;
            Set(unlock);
        }
    }

    void Unlock() => Set(true);

    void Set(bool unlock)
    {
        Cursor.lockState = unlock ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlock ? showCursor : false;
#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_INPUT_SYSTEM_EXISTS
        var inputs = FindObjectOfType<StarterAssetsInputs>(true);
        if (inputs) inputs.cursorInputForLook = !unlock;
#endif
    }
}
