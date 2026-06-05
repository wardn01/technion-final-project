using UnityEngine;
using TMPro;

/// <summary>
/// Manages the navigation and display of different settings tabs (Graphics, Audio, Keybindings) within the Settings UI.
/// </summary>
public class SettingsTabManager : MonoBehaviour
{
    #region UI References
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
    /// Ensures the Graphics tab is displayed by default whenever the settings menu is enabled or opened.
    /// </summary>
    private void OnEnable()
    {
        ShowGraphicsTab(); 
    }
    #endregion

    #region Tab Navigation
    /// <summary>
    /// Activates the Graphics tab, hides other tabs, and updates button text indicators.
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
    /// Activates the Audio tab, hides other tabs, and updates button text indicators.
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
    /// Activates the Keybindings tab, hides other tabs, and updates button text indicators.
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
}