using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsMenu : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer mainMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider dialogueSlider;
    public Slider sfxSlider;

    [Header("Value Texts")]
    public TextMeshProUGUI masterText;
    public TextMeshProUGUI musicText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI sfxText;

    private void Start()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVol", 50f);
        float musicVol = PlayerPrefs.GetFloat("MusicVol", 50f);
        float dialogueVol = PlayerPrefs.GetFloat("DialogueVol", 50f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVol", 50f);

        if (masterSlider != null) masterSlider.value = masterVol;
        if (musicSlider != null) musicSlider.value = musicVol;
        if (dialogueSlider != null) dialogueSlider.value = dialogueVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (dialogueSlider != null) dialogueSlider.onValueChanged.AddListener(SetDialogueVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetDialogueVolume(dialogueVol);
        SetSFXVolume(sfxVol);
    }

    public void SetMasterVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat("MasterVol", volume);
        PlayerPrefs.SetFloat("MasterVol", sliderValue);
        if (masterText != null) masterText.text = Mathf.RoundToInt(sliderValue).ToString();
    }

    public void SetMusicVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat("MusicVol", volume);
        PlayerPrefs.SetFloat("MusicVol", sliderValue);
        if (musicText != null) musicText.text = Mathf.RoundToInt(sliderValue).ToString();
    }

    public void SetDialogueVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat("DialogueVol", volume);
        PlayerPrefs.SetFloat("DialogueVol", sliderValue);
        if (dialogueText != null) dialogueText.text = Mathf.RoundToInt(sliderValue).ToString();
    }

    public void SetSFXVolume(float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat("SFXVol", volume);
        PlayerPrefs.SetFloat("SFXVol", sliderValue);
        if (sfxText != null) sfxText.text = Mathf.RoundToInt(sliderValue).ToString();
    }
}