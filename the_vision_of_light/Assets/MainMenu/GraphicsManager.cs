using UnityEngine;
using TMPro; 

public class GraphicsManager : MonoBehaviour
{
    [Header("UI Dropdowns")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown displayModeDropdown;
    public TMP_Dropdown fpsDropdown;

    void Start()
    {
        int savedQuality = PlayerPrefs.GetInt("GraphicsQuality", 0);
        int savedDisplay = PlayerPrefs.GetInt("DisplayMode", 0);     
        int savedFPS = PlayerPrefs.GetInt("FPSLimit", 0);        

        qualityDropdown.value = savedQuality;
        displayModeDropdown.value = savedDisplay;
        fpsDropdown.value = savedFPS;

        ApplyQuality(savedQuality);
        ApplyDisplayMode(savedDisplay);
        ApplyFPS(savedFPS);

        qualityDropdown.onValueChanged.AddListener(SetQuality);
        displayModeDropdown.onValueChanged.AddListener(SetDisplayMode);
        fpsDropdown.onValueChanged.AddListener(SetFPS);
    }

    public void SetQuality(int index)
    {
        ApplyQuality(index);
        PlayerPrefs.SetInt("GraphicsQuality", index); 
    }

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

    public void SetDisplayMode(int index)
    {
        ApplyDisplayMode(index);
        PlayerPrefs.SetInt("DisplayMode", index); 
    }

    private void ApplyDisplayMode(int index)
    {
        switch (index)
        {
            case 0: Screen.SetResolution(1920, 1080, FullScreenMode.ExclusiveFullScreen); break; // 1920x1080 FullScreen
            case 1: Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow); break;    // 1920x1080 Borderless
            case 2: Screen.SetResolution(1280, 720, FullScreenMode.FullScreenWindow); break;     // 1280x720 Borderless
            case 3: Screen.SetResolution(960, 540, FullScreenMode.FullScreenWindow); break;      // 960x540 Borderless
        }
    }

    public void SetFPS(int index)
    {
        ApplyFPS(index);
        PlayerPrefs.SetInt("FPSLimit", index); 
    }

    private void ApplyFPS(int index)
    {
        QualitySettings.vSyncCount = 0;

        switch (index)
        {
            case 0: Application.targetFrameRate = -1; break;  // Unlimited (-1)
            case 1: Application.targetFrameRate = 120; break;
            case 2: Application.targetFrameRate = 60; break;
            case 3: Application.targetFrameRate = 30; break;
        }
    }
}