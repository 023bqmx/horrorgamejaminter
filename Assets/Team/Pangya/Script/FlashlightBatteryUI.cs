// FlashlightBatteryUI.cs
using UnityEngine;
using UnityEngine.UI;

public class FlashlightBatteryUI : MonoBehaviour
{
    [SerializeField] FlashlightController flashlight;
    [SerializeField] Slider slider;
    [SerializeField] Image  fillImage;

    void OnEnable()
    {
        if (flashlight) flashlight.onBatteryChanged.AddListener(OnBattery);
        // init
        OnBattery(flashlight ? flashlight.BatteryPercent : 1f);
    }
    void OnDisable()
    {
        if (flashlight) flashlight.onBatteryChanged.RemoveListener(OnBattery);
    }

    void OnBattery(float percent)
    {
        if (slider)    slider.value = percent;
        if (fillImage) fillImage.fillAmount = percent;
    }
}
