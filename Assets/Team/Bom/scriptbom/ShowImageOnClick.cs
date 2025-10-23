using UnityEngine;

public class ShowImageOnClick : MonoBehaviour
{
    [SerializeField] private GameObject uiImage;   // ลาก Image/Panel จาก Canvas มาใส่

    [SerializeField] private bool toggle = true;   // true = คลิกสลับโชว์/ซ่อน, false = โชว์ครั้งเดียว

    private void Start()
    {
        if (uiImage != null) uiImage.SetActive(false); // เริ่มต้นซ่อนไว้
    }

    private void OnMouseDown()
    {
        if (uiImage == null) return;

        if (toggle)
        {
            uiImage.SetActive(!uiImage.activeSelf); // คลิกแล้วสลับ
        }
        else
        {
            uiImage.SetActive(true); // คลิกแล้วโชว์อย่างเดียว
        }
    }
}
