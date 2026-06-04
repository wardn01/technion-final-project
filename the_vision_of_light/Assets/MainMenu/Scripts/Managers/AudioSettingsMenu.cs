using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the game's audio settings, linking UI sliders to the AudioMixer and saving user preferences.
/// </summary>
public class AudioSettingsMenu : MonoBehaviour
{   
    #region Components
    [Header("Audio Mixer")]
    /// <summary>The main audio mixer handling all sound groups.</summary>
    public AudioMixer mainMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider dialogueSlider;
    public Slider sfxSlider;
    public Slider uiSlider;

    [Header("Value Texts")]
    public TextMeshProUGUI masterText;
    public TextMeshProUGUI musicText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI sfxText;
    public TextMeshProUGUI uiText;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Loads saved volume preferences, applies them to the sliders, and initializes listeners.
    /// </summary>
    private void Start()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVol", 50f);
        float musicVol = PlayerPrefs.GetFloat("MusicVol", 50f);
        float dialogueVol = PlayerPrefs.GetFloat("DialogueVol", 50f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVol", 50f);
        float uiVol = PlayerPrefs.GetFloat("UIVol", 50f);

        if (masterSlider != null) masterSlider.value = masterVol;
        if (musicSlider != null) musicSlider.value = musicVol;
        if (dialogueSlider != null) dialogueSlider.value = dialogueVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;
        if (uiSlider != null) uiSlider.value = uiVol;

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (dialogueSlider != null) dialogueSlider.onValueChanged.AddListener(SetDialogueVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiSlider != null) uiSlider.onValueChanged.AddListener(SetUIVolume);

        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetDialogueVolume(dialogueVol);
        SetSFXVolume(sfxVol);
        SetUIVolume(uiVol);
    }
    #endregion

    #region Volume Controllers
    /// <summary>
    /// Converts slider value to a logarithmic scale, applies it to the Master channel, saves it, and updates UI text.
    /// </summary> 
    /// <param name="sliderValue">Volume value typically ranging from 0 to 100.</param>
    public void SetMasterVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;

        mainMixer.SetFloat("MasterVol", volume);
        PlayerPrefs.SetFloat("MasterVol", sliderValue);

        if (masterText != null)
            masterText.text = Mathf.RoundToInt(sliderValue).ToString();
    }

    /// <summary>
    /// Converts slider value to a logarithmic scale, applies it to the Music channel, saves it, and updates UI text.
    /// </summary>
    /// <param name="sliderValue">Volume value typically ranging from 0 to 100.</param>
    public void SetMusicVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;

        mainMixer.SetFloat("MusicVol", volume);
        PlayerPrefs.SetFloat("MusicVol", sliderValue);

        if (musicText != null)
            musicText.text = Mathf.RoundToInt(sliderValue).ToString();
    }

    /// <summary>
    /// Converts slider value to a logarithmic scale, applies it to the Dialogue channel, saves it, and updates UI text.
    /// </summary>
    /// <param name="sliderValue">Volume value typically ranging from 0 to 100.</param>
    public void SetDialogueVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;

        mainMixer.SetFloat("DialogueVol", volume);
        PlayerPrefs.SetFloat("DialogueVol", sliderValue);

        if (dialogueText != null)
            dialogueText.text = Mathf.RoundToInt(sliderValue).ToString();
    }

    /// <summary>
    /// Converts slider value to a logarithmic scale, applies it to the SFX channel, saves it, and updates UI text.
    /// </summary>
    /// <param name="sliderValue">Volume value typically ranging from 0 to 100.</param>
    public void SetSFXVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;

        mainMixer.SetFloat("SFXVol", volume);
        PlayerPrefs.SetFloat("SFXVol", sliderValue);

        if (sfxText != null)
            sfxText.text = Mathf.RoundToInt(sliderValue).ToString();
    }

    /// <summary>
    /// Converts slider value to a logarithmic scale, applies it to the UI channel, saves it, and updates UI text.
    /// </summary> 
    /// <param name="sliderValue">Volume value typically ranging from 0 to 100.</param>
    public void SetUIVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;

        mainMixer.SetFloat("UIVol", volume);
        PlayerPrefs.SetFloat("UIVol", sliderValue);

        if (uiText != null)
            uiText.text = Mathf.RoundToInt(sliderValue).ToString();
    }
    #endregion
}