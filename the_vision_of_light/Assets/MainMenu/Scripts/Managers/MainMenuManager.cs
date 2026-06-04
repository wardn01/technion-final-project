using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the main menu flow, including opening panels, switching settings tabs, and quitting the game.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    #region UI References
    [Header("Main Panels")]
    public GameObject mainMenuPanel;
    public GameObject playPanel;
    public GameObject settingsPanel;
    public GameObject blurOverlay;

    [Header("Settings Tabs")]
    public GameObject graphicsTab;
    public GameObject audioTab;
    public GameObject keyTab;

    [Header("Settings Tab Texts")]
    public TMP_Text graphicsBtnText;
    public TMP_Text audioBtnText;
    public TMP_Text keyBtnText;
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
    /// Opens the settings panel, activates blur, and defaults to the Graphics tab.
    /// </summary>
    public void OpenSettings()
    {
        if (blurOverlay != null) blurOverlay.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        ShowGraphicsTab(); 
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

    #region Settings Tabs
    /// <summary>
    /// Displays the Graphics tab and updates tab button texts to indicate selection.
    /// </summary>
    public void ShowGraphicsTab()
    {
        if (graphicsTab != null) graphicsTab.SetActive(true);
        if (audioTab != null) audioTab.SetActive(false);
        if (keyTab != null) keyTab.SetActive(false);

        if (graphicsBtnText != null) graphicsBtnText.text = "> Graphics";
        if (audioBtnText != null) audioBtnText.text = "Audio";
        if (keyBtnText != null) keyBtnText.text = "KeyBindings";
    }

    /// <summary>
    /// Displays the Audio tab and updates tab button texts to indicate selection.
    /// </summary>
    public void ShowAudioTab()
    {
        if (graphicsTab != null) graphicsTab.SetActive(false);
        if (audioTab != null) audioTab.SetActive(true);
        if (keyTab != null) keyTab.SetActive(false);

        if (graphicsBtnText != null) graphicsBtnText.text = "Graphics";
        if (audioBtnText != null) audioBtnText.text = "> Audio";
        if (keyBtnText != null) keyBtnText.text = "KeyBindings";
    }

    /// <summary>
    /// Displays the Keybindings tab and updates tab button texts to indicate selection.
    /// </summary>
    public void ShowKeyTab()
    {
        if (graphicsTab != null) graphicsTab.SetActive(false);
        if (audioTab != null) audioTab.SetActive(false);
        if (keyTab != null) keyTab.SetActive(true);

        if (graphicsBtnText != null) graphicsBtnText.text = "Graphics";
        if (audioBtnText != null) audioBtnText.text = "Audio";
        if (keyBtnText != null) keyBtnText.text = "> KeyBindings";
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