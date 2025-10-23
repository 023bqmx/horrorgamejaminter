using StarterAssets;
using UnityEngine;

public class HeadLook : MonoBehaviour
{
    public GameObject Player;
    public Transform headObj;

    private void Start()
    {
        Player = FirstPersonController.Instance.gameObject;
    }
    private void LateUpdate()
    {
        headObj.LookAt(Player.transform);
    }
}
