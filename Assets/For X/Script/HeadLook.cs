using StarterAssets;
using UnityEngine;

public class HeadLook : MonoBehaviour
{
    AIController aIController;
    public GameObject Player;
    public Transform headObj;

    private void Start()
    {
        Player = FirstPersonController.Instance.gameObject;
        aIController = Player.GetComponent<AIController>();
    }
    private void LateUpdate()
    {

        headObj.LookAt(Player.transform);
    }
}
