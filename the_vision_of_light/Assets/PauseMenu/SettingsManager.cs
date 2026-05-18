using UnityEngine;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Settings Tabs")]
    public GameObject graphicsTab;
    public GameObject audioTab;
    public GameObject keyTab;

    [Header("Settings Tab Texts")]
    public TMP_Text graphicsBtnText;
    public TMP_Text audioBtnText;
    public TMP_Text keyBtnText;

    private void OnEnable()
    {
        ShowGraphicsTab();
    }

    public void ShowGraphicsTab()
    {
        if (graphicsTab != null) graphicsTab.SetActive(true);
        if (audioTab != null) audioTab.SetActive(false);
        if (keyTab != null) keyTab.SetActive(false);

        if (graphicsBtnText != null) graphicsBtnText.text = "> Graphics";
        if (audioBtnText != null) audioBtnText.text = "Audio";
        if (keyBtnText != null) keyBtnText.text = "KeyBindings";

        if (KeybindManager.Instance != null) KeybindManager.Instance.CancelRebind();
    }

    public void ShowAudioTab()
    {
        if (graphicsTab != null) graphicsTab.SetActive(false);
        if (audioTab != null) audioTab.SetActive(true);
        if (keyTab != null) keyTab.SetActive(false);

        if (graphicsBtnText != null) graphicsBtnText.text = "Graphics";
        if (audioBtnText != null) audioBtnText.text = "> Audio";
        if (keyBtnText != null) keyBtnText.text = "KeyBindings";

        if (KeybindManager.Instance != null) KeybindManager.Instance.CancelRebind();
    }

    public void ShowKeyTab()
    {
        if (graphicsTab != null) graphicsTab.SetActive(false);
        if (audioTab != null) audioTab.SetActive(false);
        if (keyTab != null) keyTab.SetActive(true);

        if (graphicsBtnText != null) graphicsBtnText.text = "Graphics";
        if (audioBtnText != null) audioBtnText.text = "Audio";
        if (keyBtnText != null) keyBtnText.text = "> KeyBindings";
    }
}