using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using VisionOfLight.Enemy;
using VisionOfLight.Player;

/// <summary>
/// World-only dynamic BGM: exploration, village, normal combat, Orc boss, Golem boss.
/// Snow is an ambience layer: exploration keeps playing at reduced volume.
/// Place in the World scene (not MainMenu — keep MainMenuBGM there).
/// </summary>
[DefaultExecutionOrder(-50)]
public class WorldMusicManager : MonoBehaviour
{
    public static WorldMusicManager Instance { get; private set; }

    /// <summary>True while combat or a boss fight drives the music (ambience zones duck themselves).</summary>
    public static bool IsCombatMusicActive
    {
        get
        {
            if (Instance == null)
                return false;

            return Instance.currentMood == MusicMood.Combat
                || Instance.currentMood == MusicMood.OrcBoss
                || Instance.currentMood == MusicMood.GolemBoss;
        }
    }

    public enum MusicMood
    {
        Exploration,
        Village,
        Combat,
        OrcBoss,
        GolemBoss
    }

    [Header("Mixer")]
    [Tooltip("Assets/MainMenu/Audio/MainAudioMixer. Required so the Music settings slider works.")]
    public AudioMixer mainMixer;

    [Tooltip("Optional. Auto-resolved from mixer as \"Music\" if empty.")]
    public AudioMixerGroup musicGroup;

    [Tooltip("Optional. Seeds AudioMixerHub for SFX routing. Leave empty to auto-find \"SFX\".")]
    public AudioMixerGroup sfxGroup;

    [Tooltip("Optional. Leave empty to auto-find \"UI\".")]
    public AudioMixerGroup uiGroup;

    [Tooltip("Optional. Leave empty to auto-find \"Dialogue\".")]
    public AudioMixerGroup dialogueGroup;

    [Header("Tracks")]
    [Tooltip("Roaming / open world. Soft background while not fighting. Suggested: First_Light_on_the_Peak.")]
    public AudioClip explorationMusic;

    [Tooltip("Safe village theme. Plays while the player is inside a WorldMusicVillageZone.")]
    public AudioClip villageMusic;

    [Tooltip("Normal enemy chase or fight (Goblin, Bear, Skeleton, wave trash, …). Louder than exploration.")]
    public AudioClip combatMusic;

    [Tooltip("Plays while the Orc boss is aggroed. Falls back to Combat / Exploration if empty.")]
    public AudioClip orcBossMusic;

    [Tooltip("Plays while the Golem boss is aggroed. Falls back to Combat / Exploration if empty.")]
    public AudioClip golemBossMusic;

    [Header("Volume Per Mood")]
    [Tooltip("Quiet roaming level. Keep lower than combat so fights feel louder.")]
    [Range(0f, 1f)]
    public float explorationVolume = 0.28f;

    [Tooltip("Village theme level. Usually soft, similar to exploration.")]
    [Range(0f, 1f)]
    public float villageVolume = 0.35f;

    [Header("Snow Region")]
    [Tooltip("Exploration volume multiplier inside snow zones. 0.7 = 30% quieter.")]
    [Range(0f, 1f)]
    public float explorationVolumeInSnowMultiplier = 0.7f;

    [Tooltip("Normal combat level. Should be clearly louder than exploration.")]
    [Range(0f, 1f)]
    public float combatVolume = 0.7f;

    [Tooltip("Orc boss fight level.")]
    [Range(0f, 1f)]
    public float orcBossVolume = 0.8f;

    [Tooltip("Golem boss fight level.")]
    [Range(0f, 1f)]
    public float golemBossVolume = 0.8f;

    [Header("Playback")]
    [Tooltip("Seconds to crossfade when the track or volume mood changes.")]
    [Min(0f)]
    public float fadeSeconds = 1.1f;

    [Tooltip("How often enemies are scanned for combat / boss music.")]
    [Min(0.1f)]
    public float pollInterval = 0.35f;

    [Tooltip("After combat ends, wait this long before returning to exploration music.")]
    [Min(0f)]
    public float combatExitDelay = 2.5f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;
    /// <summary>Clip we are currently playing OR fading toward. Set immediately so polls don't restart the fade.</summary>
    private AudioClip targetClip;
    private MusicMood currentMood = MusicMood.Exploration;
    private float currentTargetVolume;
    private float nextPollTime;
    private float combatClearAt = -1f;
    private bool isPlayerInSnow;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;

        ConfigureMixer();
        AudioMixerHub.Route(audioSource, AudioMixerHub.Bus.Music);
    }

    private void Start()
    {
        // Mixer SetFloat during Awake/scene-load does not stick (Unity resets it
        // when the mixer snapshot initializes) — apply saved sliders here instead.
        AudioMixerHub.ApplySavedVolumes();
        ForceMood(MusicMood.Exploration, immediate: true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextPollTime)
            return;

        nextPollTime = Time.unscaledTime + pollInterval;
        EvaluateMood();
    }

    public void ForceMood(MusicMood mood, bool immediate = false)
    {
        currentMood = mood;
        PlayClip(ResolveClip(mood), ResolveVolume(mood), immediate);
    }

    private void ConfigureMixer()
    {
        if (mainMixer == null)
        {
            Debug.LogWarning("[WorldMusicManager] mainMixer is not assigned.");
            return;
        }

        AudioMixerHub.Configure(mainMixer, musicGroup, sfxGroup, uiGroup, dialogueGroup);

        if (musicGroup == null)
            musicGroup = AudioMixerHub.GetGroup(AudioMixerHub.Bus.Music);

        if (musicGroup != null)
            audioSource.outputAudioMixerGroup = musicGroup;
    }

    private void EvaluateMood()
    {
        Transform player = PlayerRegistry.Instance != null
            ? PlayerRegistry.Instance.transform
            : SharedInteractPromptUtility.GetPlayerTransform();

        if (player == null)
        {
            ApplyMood(MusicMood.Exploration);
            return;
        }

        if (PlayerRegistry.Instance != null &&
            PlayerRegistry.Instance.Health != null &&
            PlayerRegistry.Instance.Health.isDead)
        {
            ApplyMood(MusicMood.Exploration);
            return;
        }

        ApplyMood(DetectDesiredMood(player));
    }

    private MusicMood DetectDesiredMood(Transform player)
    {
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        bool anyCombat = false;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy.IsDead || !enemy.WantsCombatMusic(player))
                continue;

            if (enemy is Orc)
                return MusicMood.OrcBoss;

            if (enemy is Golem)
                return MusicMood.GolemBoss;

            anyCombat = true;
        }

        if (anyCombat)
        {
            combatClearAt = -1f;
            return MusicMood.Combat;
        }

        if (currentMood == MusicMood.Combat ||
            currentMood == MusicMood.OrcBoss ||
            currentMood == MusicMood.GolemBoss)
        {
            if (combatClearAt < 0f)
                combatClearAt = Time.unscaledTime + combatExitDelay;

            if (Time.unscaledTime < combatClearAt)
                return currentMood;
        }

        combatClearAt = -1f;

        // Village replaces exploration and wins if zones overlap.
        if (WorldMusicVillageZone.ContainsPlayer(player.position))
        {
            isPlayerInSnow = false;
            return MusicMood.Village;
        }

        // Snow keeps exploration playing, but ducks it while snow ambience is layered above it.
        isPlayerInSnow = WorldAmbienceZone.ShouldDuckExploration(player.position);
        return MusicMood.Exploration;
    }

    private void ApplyMood(MusicMood mood)
    {
        AudioClip nextClip = ResolveClip(mood);
        float nextVolume = ResolveVolume(mood);

        // Compare against the clip we're already playing OR fading toward,
        // so the 0.35s poll doesn't restart an in-progress crossfade forever.
        bool sameClip = nextClip == targetClip;
        bool sameMood = mood == currentMood;
        bool sameVolume = Mathf.Abs(nextVolume - currentTargetVolume) < 0.01f;

        if (sameClip && sameMood && sameVolume)
            return;

        currentMood = mood;

        // Same track (e.g. only exploration assigned) — still raise/lower volume for combat.
        if (sameClip)
        {
            FadeVolumeTo(nextVolume);
            return;
        }

        PlayClip(nextClip, nextVolume, immediate: false);
    }

    private AudioClip ResolveClip(MusicMood mood)
    {
        AudioClip clip = mood switch
        {
            MusicMood.Village => villageMusic,
            MusicMood.Combat => combatMusic,
            MusicMood.OrcBoss => orcBossMusic,
            MusicMood.GolemBoss => golemBossMusic,
            _ => explorationMusic
        };

        if (clip == null)
            clip = explorationMusic;
        if (clip == null)
            clip = combatMusic;
        if (clip == null)
            clip = orcBossMusic != null ? orcBossMusic : golemBossMusic;

        return clip;
    }

    private float ResolveVolume(MusicMood mood)
    {
        return mood switch
        {
            MusicMood.Village => villageVolume,
            MusicMood.Combat => combatVolume,
            MusicMood.OrcBoss => orcBossVolume,
            MusicMood.GolemBoss => golemBossVolume,
            _ => explorationVolume * (isPlayerInSnow ? explorationVolumeInSnowMultiplier : 1f)
        };
    }

    private void PlayClip(AudioClip clip, float targetVolume, bool immediate)
    {
        currentTargetVolume = targetVolume;
        targetClip = clip;

        if (clip == null)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = 0f;
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (immediate || fadeSeconds <= 0f || !audioSource.isPlaying)
        {
            audioSource.clip = clip;
            audioSource.volume = targetVolume;
            audioSource.Play();
            return;
        }

        fadeRoutine = StartCoroutine(CrossfadeTo(clip, targetVolume));
    }

    private void FadeVolumeTo(float targetVolume)
    {
        currentTargetVolume = targetVolume;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (fadeSeconds <= 0f)
        {
            audioSource.volume = targetVolume;
            return;
        }

        fadeRoutine = StartCoroutine(VolumeOnlyFade(targetVolume));
    }

    private IEnumerator VolumeOnlyFade(float targetVolume)
    {
        float startVolume = audioSource.volume;
        float duration = Mathf.Max(0.05f, fadeSeconds);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeRoutine = null;
    }

    private IEnumerator CrossfadeTo(AudioClip next, float targetVolume)
    {
        float startVolume = audioSource.volume;
        float half = Mathf.Max(0.05f, fadeSeconds * 0.5f);

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / half);
            yield return null;
        }

        audioSource.clip = next;
        audioSource.volume = 0f;
        audioSource.Play();

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t / half);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeRoutine = null;
    }
}
