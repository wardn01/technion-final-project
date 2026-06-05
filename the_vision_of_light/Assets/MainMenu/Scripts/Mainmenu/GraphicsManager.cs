using UnityEngine;
using TMPro;

/// <summary>
/// Manages game graphics settings, linking UI dropdowns to Unity's QualitySettings, Screen, and Application classes, and saving user preferences.
/// </summary>
public class GraphicsManager : MonoBehaviour
{   
    #region Components
    [Header("UI Dropdowns")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown displayModeDropdown;
    public TMP_Dropdown fpsDropdown;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Loads saved graphics preferences, applies them to the game, and initializes dropdown listeners.
    /// </summary>
    private void Start()
    {
        int savedQuality = PlayerPrefs.GetInt("GraphicsQuality", 0);
        int savedDisplay = PlayerPrefs.GetInt("DisplayMode", 0);
        int savedFPS = PlayerPrefs.GetInt("FPSLimit", 0);

        // Null checks to prevent errors if dropdowns aren't assigned in the inspector
        if (qualityDropdown != null) qualityDropdown.value = savedQuality;
        if (displayModeDropdown != null) displayModeDropdown.value = savedDisplay;
        if (fpsDropdown != null) fpsDropdown.value = savedFPS;

        ApplyQuality(savedQuality);
        ApplyDisplayMode(savedDisplay);
        ApplyFPS(savedFPS);

        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(SetQuality);
        if (displayModeDropdown != null) displayModeDropdown.onValueChanged.AddListener(SetDisplayMode);
        if (fpsDropdown != null) fpsDropdown.onValueChanged.AddListener(SetFPS);
    }
    #endregion

    #region Graphics Controllers

    /// <summary>
    /// Applies the selected quality level and saves the preference.
    /// </summary>
    /// <param name="index">Dropdown selection index.</param>
    public void SetQuality(int index)
    {
        ApplyQuality(index);
        PlayerPrefs.SetInt("GraphicsQuality", index);
    }

    /// <summary>
    /// Maps the UI index to Unity's internal QualitySettings levels.
    /// </summary>
    private void ApplyQuality(int index)
    {
        switch (index)
        {
            case 0: QualitySettings.SetQualityLevel(3); break; // High
            case 1: QualitySettings.SetQualityLevel(2); break; // Medium
            case 2: QualitySettings.SetQualityLevel(1); break; // Low
            case 3: QualitySettings.SetQualityLevel(0); break; // Lowest
        }
    }

    /// <summary>
    /// Applies the selected display mode and resolution, then saves the preference.
    /// </summary>
    /// <param name="index">Dropdown selection index.</param>
    public void SetDisplayMode(int index)
    {
        ApplyDisplayMode(index);
        PlayerPrefs.SetInt("DisplayMode", index);
    }

    /// <summary>
    /// Changes screen resolution and full-screen mode based on the provided index.
    /// </summary>
    private void ApplyDisplayMode(int index)
    {
        switch (index)
        {
            case 0: Screen.SetResolution(1920, 1080, FullScreenMode.ExclusiveFullScreen); break;
            case 1: Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow); break;
            case 2: Screen.SetResolution(1280, 720, FullScreenMode.FullScreenWindow); break;
            case 3: Screen.SetResolution(960, 540, FullScreenMode.FullScreenWindow); break;
        }
    }

    /// <summary>
    /// Applies the target frame rate and saves the preference.
    /// </summary>
    /// <param name="index">Dropdown selection index.</param>
    public void SetFPS(int index)
    {
        ApplyFPS(index);
        PlayerPrefs.SetInt("FPSLimit", index);
    }

    /// <summary>
    /// Disables VSync and sets the application's target frame rate.
    /// </summary>
    private void ApplyFPS(int index)
    {
        QualitySettings.vSyncCount = 0;

        switch (index)
        {
            case 0: Application.targetFrameRate = -1; break; // Unlimited
            case 1: Application.targetFrameRate = 120; break;
            case 2: Application.targetFrameRate = 60; break;
            case 3: Application.targetFrameRate = 30; break;
        }
    }

    #endregion
}