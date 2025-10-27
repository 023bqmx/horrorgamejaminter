using UnityEngine;
using TMPro;
using OpenSee;
using System.Collections.Generic;

public class OSF_CameraPicker : MonoBehaviour
{
    [SerializeField] private OpenSeeLauncher osfLauncher;
    [SerializeField] private TMP_Dropdown dropdown;
    const string Key = "OSF.CameraIndex";

    void Awake()
    {
        if (!osfLauncher) osfLauncher = FindObjectOfType<OpenSeeLauncher>(true);

        var names = osfLauncher.ListCameras();
        var opts = new List<TMP_Dropdown.OptionData>();
        if (names.Length == 0) opts.Add(new TMP_Dropdown.OptionData("No camera found"));
        else for (int i = 0; i < names.Length; i++) opts.Add(new TMP_Dropdown.OptionData($"{i}: {names[i]}"));

        dropdown.ClearOptions();
        dropdown.AddOptions(opts);

        int saved = Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Mathf.Max(0, names.Length - 1));
        dropdown.value = saved;
        dropdown.RefreshShownValue();
        osfLauncher.cameraIndex = saved;

        dropdown.onValueChanged.AddListener(i => {
            osfLauncher.cameraIndex = i;
            PlayerPrefs.SetInt(Key, i);
        });
    }

    public void StartTracker() => osfLauncher.StartTracker();
    public void StopTracker() => osfLauncher.StopTracker();
}
