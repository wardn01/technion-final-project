using UnityEngine;

/// <summary>
/// Manages the main menu flow, including opening panels, settings, and quitting the application.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    #region UI References
    [Header("Main Panels")]
    public GameObject mainMenuPanel;
    public GameObject playPanel;
    public GameObject settingsPanel;
    public GameObject blurOverlay;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the menu state by hiding sub-panels and the blur overlay.
    /// </summary>
    private void Start()
    {
        if (playPanel != null) playPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.SetActive(false);
    }
    #endregion

    #region Menu Navigation
    /// <summary>
    /// Opens the play panel and activates the background blur.
    /// </summary>
    public void OpenPlayMenu()
    {
        if (blurOverlay != null) blurOverlay.SetActive(true);
        if (playPanel != null) playPanel.SetActive(true);
    }

    /// <summary>
    /// Closes the play panel and deactivates the background blur.
    /// </summary>
    public void ClosePlayMenu()
    {
        if (blurOverlay != null) blurOverlay.SetActive(false);
        if (playPanel != null) playPanel.SetActive(false);
    }

    /// <summary>
    /// Opens the settings panel and activates the background blur.
    /// </summary>
    public void OpenSettings()
    {
        if (blurOverlay != null) blurOverlay.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Closes the settings panel and deactivates the background blur.
    /// </summary>
    public void CloseSettings()
    {
        if (blurOverlay != null) blurOverlay.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
    #endregion

    #region Game Exit
    /// <summary>
    /// Quits the application. If running in the Unity Editor, it safely stops play mode.
    /// </summary>
    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    #endregion
}