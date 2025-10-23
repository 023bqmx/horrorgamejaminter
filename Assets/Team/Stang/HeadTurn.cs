using UnityEngine;

public class HeadTurn : MonoBehaviour
{
    public Transform headBone;
    public Transform target;
    public float rotationSpeed = 5f;
    public Vector3 rotationOffset; // ปรับได้ใน Inspector

    void LateUpdate()
    {
        if (headBone == null || target == null) return;

        Vector3 direction = target.position - headBone.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        lookRotation *= Quaternion.Euler(rotationOffset);

        headBone.rotation = Quaternion.Slerp(
            headBone.rotation,
            lookRotation,
            Time.deltaTime * rotationSpeed
        );
    }

}