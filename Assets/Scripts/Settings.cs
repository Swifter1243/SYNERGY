using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Handler for the game settings. </summary>
public class Settings : MonoBehaviour
{
    /// <summary> Whether the settings have been initialized. </summary>
    static bool initialized = false;

    /// <summary> The possible forms the game can be displayed. </summary>
    public enum DisplayMode
    {
        Fullscreen,
        Windowed
    }

    // Values
    /// <summary> The volume of the game without volume reduction. </summary>
    static float rawMasterVolume;
    /// <summary> The amount to multiply the game's volume by. </summary>
    static float volumeReduction = 0.17f;
    /// <summary> The volume of the game. </summary>
    public static float masterVolume {
        get => rawMasterVolume * volumeReduction;
    }
    /// <summary> The way the game is displayed. </summary>
    public static DisplayMode display;
    /// <summary> Whether you fail when your health reaches zero. </summary>
    public static bool noFail;
    /// <summary> Whether the UI when playing is displayed. </summary>
    public static bool hideUI;

    // Objects
    /// <summary> The slider for the master volume. </summary>
    public Slider masterVolumeObj;
    /// <summary> The text for the master volume. </summary>
    public Text masterVolumeText;
    /// <summary> The dropdown menu for the display mode. </summary>
    public Dropdown displayObj;
    /// <summary> The toggle object for no fail. </summary>
    public Toggle noFailObj;
    /// <summary> The toglge object for hide UI. </summary>
    public Toggle hideUIObj;

    /// <summary> The resolution the game is at upon launching. </summary>
    static Vector2 initResolution;
    /// <summary> The resolution the game is at when it's windowed. </summary>
    static Vector2 windowedResolution;

    /// <summary> Initializes the settings. </summary>
    public void Initialize()
    {
        // Loading from player preferences
        if (!initialized)
        {
            initialized = true;
            initResolution = new Vector2(Display.main.systemWidth, Display.main.systemHeight);

            rawMasterVolume = Utils.InitPlayerPrefsFloat("masterVolume", 1);
            display = (DisplayMode)Utils.InitPlayerPrefsInt("display", (int)DisplayMode.Fullscreen);
            noFail = Utils.InitPlayerPrefsInt("noFail", 0) == 1;
            hideUI = Utils.InitPlayerPrefsInt("hideUI", 0) == 1;
        }

        // Initializing windowed resolution if started in fullscreen
        if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow) windowedResolution = new Vector2(Screen.width, Screen.height);

        // Updating setting UI objects
        masterVolumeObj.value = rawMasterVolume;
        displayObj.value = (int)display;
        noFailObj.isOn = noFail;
        hideUIObj.isOn = hideUI;

        // Applying settings
        UpdateMasterVolume();
        UpdateDisplay();
        UpdateNoFail();
        UpdateHideUI();
    }

    /// <summary> Updates the master volume. </summary>
    public void UpdateMasterVolume()
    {
        rawMasterVolume = masterVolumeObj.value;
        masterVolumeText.text = "Master Volume: " + Mathf.Round(rawMasterVolume * 100) + "%";
    }

    /// <summary> Saves the master volume value to player preferences. </summary>
    public void SaveMasterVolume() => PlayerPrefs.SetFloat("masterVolume", rawMasterVolume);

    /// <summary> Updates the display mode. </summary>
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

    /// <summary> Updates no fail. </summary>
    public void UpdateNoFail() {
        noFail = noFailObj.isOn;
        PlayerPrefs.SetInt("noFail", noFail ? 1 : 0);
    }

    /// <summary> Updates hide UI. </summary>
    public void UpdateHideUI() {
        hideUI = hideUIObj.isOn;
        PlayerPrefs.SetInt("hideUI", hideUI ? 1 : 0);
    }

    /// <summary> Closes the settings panel. </summary>
    public void Close() => this.gameObject.SetActive(false);
}
