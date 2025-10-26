using UnityEngine;

[DefaultExecutionOrder(-10000)] // ให้ตื่นก่อนชาวบ้าน
public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    [Range(0, 9)] public int Digit1;
    [Range(0, 9)] public int Digit2;
    [Range(0, 9)] public int Digit3;

    // กรณีปิด Domain Reload ใน Project Settings > Editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    // สร้าง instance อัตโนมัติ "ก่อน" โหลดซีนแรกเสมอ
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureBootstrapped()
    {
        if (Instance != null) return;

        // ถ้ามี Prefab ชื่อ GameDataPrefab ใน Resources ให้ใช้
        var prefab = Resources.Load<GameObject>("GameDataPrefab");
        if (prefab)
        {
            Object.Instantiate(prefab).name = prefab.name;
        }
        else
        {
            // fallback: ไม่มีพรีแฟบก็สร้างเปล่า ๆ ไปเลย
            new GameObject("GameData (auto)").AddComponent<GameData>();
        }
    }

    void Awake()
    {
        // กันซ้ำ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
