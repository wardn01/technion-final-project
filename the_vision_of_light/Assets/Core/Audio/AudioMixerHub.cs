using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Shared access to <c>MainAudioMixer</c> groups so SFX / UI / music honor the settings sliders.
/// Configured once by <see cref="WorldMusicManager"/> (or any bootstrap that holds the mixer).
/// </summary>
public static class AudioMixerHub
{
    public enum Bus
    {
        Music,
        SFX,
        UI,
        Dialogue
    }

    private static AudioMixer s_mixer;
    private static AudioMixerGroup s_music;
    private static AudioMixerGroup s_sfx;
    private static AudioMixerGroup s_ui;
    private static AudioMixerGroup s_dialogue;
    private static bool s_configured;

    public static bool IsConfigured => s_configured && s_mixer != null;

    public static void Configure(AudioMixer mixer)
    {
        if (mixer == null)
            return;

        s_mixer = mixer;
        s_music = FindGroup(mixer, "Music");
        s_sfx = FindGroup(mixer, "SFX");
        s_ui = FindGroup(mixer, "UI");
        s_dialogue = FindGroup(mixer, "Dialogue");
        s_configured = true;
    }

    public static void Configure(
        AudioMixer mixer,
        AudioMixerGroup music,
        AudioMixerGroup sfx,
        AudioMixerGroup ui,
        AudioMixerGroup dialogue)
    {
        if (mixer == null)
            return;

        s_mixer = mixer;
        s_music = music != null ? music : FindGroup(mixer, "Music");
        s_sfx = sfx != null ? sfx : FindGroup(mixer, "SFX");
        s_ui = ui != null ? ui : FindGroup(mixer, "UI");
        s_dialogue = dialogue != null ? dialogue : FindGroup(mixer, "Dialogue");
        s_configured = true;
    }

    /// <summary>Applies saved slider prefs (0–100) to exposed mixer parameters.</summary>
    public static void ApplySavedVolumes()
    {
        if (s_mixer == null)
            return;

        ApplySlider("MasterVol", PlayerPrefs.GetFloat("MasterVol", 50f));
        ApplySlider("MusicVol", PlayerPrefs.GetFloat("MusicVol", 50f));
        ApplySlider("DialogueVol", PlayerPrefs.GetFloat("DialogueVol", 50f));
        ApplySlider("SFXVol", PlayerPrefs.GetFloat("SFXVol", 50f));
        ApplySlider("UIVol", PlayerPrefs.GetFloat("UIVol", 50f));
    }

    public static void Route(AudioSource source, Bus bus)
    {
        if (source == null || !s_configured)
            return;

        AudioMixerGroup group = GetGroup(bus);
        if (group != null)
            source.outputAudioMixerGroup = group;
    }

    public static AudioMixerGroup GetGroup(Bus bus)
    {
        return bus switch
        {
            Bus.Music => s_music,
            Bus.SFX => s_sfx,
            Bus.UI => s_ui,
            Bus.Dialogue => s_dialogue,
            _ => null
        };
    }

    private static void ApplySlider(string parameter, float sliderValue)
    {
        float volume = Mathf.Log10(Mathf.Clamp(sliderValue / 100f, 0.0001f, 1f)) * 20f;
        s_mixer.SetFloat(parameter, volume);
    }

    private static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
    {
        AudioMixerGroup[] matches = mixer.FindMatchingGroups(name);
        if (matches == null || matches.Length == 0)
            return null;

        for (int i = 0; i < matches.Length; i++)
        {
            if (matches[i] != null && matches[i].name == name)
                return matches[i];
        }

        return matches[0];
    }
}
