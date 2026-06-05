using UnityEngine;

/// <summary>
/// Manages the main menu flow, including opening panels and quitting the game.
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
    private void Start()
    {
        if (playPanel != null) playPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.SetActive(false);
    }
    #endregion

    #region Menu Navigation
    public void OpenPlayMenu()
    {
        if (blurOverlay != null) blurOverlay.SetActive(true);
        if (playPanel != null) playPanel.SetActive(true);
    }

    public void ClosePlayMenu()
    {
        if (blurOverlay != null) blurOverlay.SetActive(false);
        if (playPanel != null) playPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (blurOverlay != null) blurOverlay.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (blurOverlay != null) blurOverlay.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
    #endregion

    #region Game Exit
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