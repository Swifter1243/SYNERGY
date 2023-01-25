using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    static bool initialized = false;

    public enum DisplayMode
    {
        Fullscreen,
        Windowed
    }

    // Values
    static float rawMasterVolume;
    static float volumeReduction = 0.17f;
    public static float masterVolume {
        get => rawMasterVolume * volumeReduction;
    }
    public static DisplayMode display;
    public static bool noFail;
    public static bool hideUI;

    // Objects
    public Slider masterVolumeObj;
    public Text masterVolumeText;
    public Dropdown displayObj;
    public Toggle noFailObj;
    public Toggle hideUIObj;

    static Vector2 initResolution;
    static Vector2 windowedResolution;

    public void Initialize()
    {
        if (!initialized)
        {
            initialized = true;
            initResolution = new Vector2(Display.main.systemWidth, Display.main.systemHeight);

            rawMasterVolume = Utils.InitPlayerPrefsFloat("masterVolume", 1);
            display = (DisplayMode)Utils.InitPlayerPrefsInt("display", (int)DisplayMode.Fullscreen);
            noFail = Utils.InitPlayerPrefsInt("noFail", 0) == 1;
            hideUI = Utils.InitPlayerPrefsInt("hideUI", 0) == 1;
        }

        if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow) windowedResolution = new Vector2(Screen.width, Screen.height);

        masterVolumeObj.value = rawMasterVolume;
        displayObj.value = (int)display;
        noFailObj.isOn = noFail;
        hideUIObj.isOn = hideUI;

        UpdateMasterVolume();
        UpdateDisplay();
        UpdateNoFail();
        UpdateHideUI();
    }

    public void UpdateMasterVolume()
    {
        rawMasterVolume = masterVolumeObj.value;
        masterVolumeText.text = "Master Volume: " + Mathf.Round(rawMasterVolume * 100) + "%";
    }

    public void SaveMasterVolume() => PlayerPrefs.SetFloat("masterVolume", rawMasterVolume);

    public void UpdateDisplay()
    {
        display = (DisplayMode)displayObj.value;
        PlayerPrefs.SetInt("display", (int)display);

        if (display == DisplayMode.Fullscreen && Screen.fullScreenMode != FullScreenMode.FullScreenWindow)
        {
            windowedResolution = new Vector2(Screen.width, Screen.height);
            Screen.SetResolution((int)initResolution.x, (int)initResolution.y, FullScreenMode.FullScreenWindow);
        }
        if (display == DisplayMode.Windowed && Screen.fullScreenMode != FullScreenMode.Windowed)
        {
            if (windowedResolution != null) Screen.SetResolution((int)windowedResolution.x, (int)windowedResolution.y, FullScreenMode.Windowed);
            else Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }

    public void UpdateNoFail() {
        noFail = noFailObj.isOn;
        PlayerPrefs.SetInt("noFail", noFail ? 1 : 0);
    }

    public void UpdateHideUI() {
        hideUI = hideUIObj.isOn;
        PlayerPrefs.SetInt("hideUI", hideUI ? 1 : 0);
    }

    public void Close() => this.gameObject.SetActive(false);
}
