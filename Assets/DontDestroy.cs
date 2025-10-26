using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    public static DontDestroy instance;
    void Start()
    {

        DontDestroyOnLoad(gameObject);
    }

}
