using UnityEngine;

/// <summary>
/// Plays one-shot SFX from an <see cref="EnemyAudioData"/> library.
/// Wire animation events as <c>PlayEnemySound("Attack")</c> on the enemy root (via <see cref="EnemyBase"/>).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EnemyAudioEmitter : MonoBehaviour
{
    public EnemyAudioData audioData;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    /// <summary>Plays a random clip for <paramref name="actionName"/>. Called from <see cref="EnemyBase.PlayEnemySound"/>.</summary>
    public void PlayClip(string actionName)
    {
        if (audioData == null) return;

        var entry = audioData.GetSound(actionName);
        if (entry.clips == null || entry.clips.Length == 0)
            return;

        Object clipObject = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clipObject is not AudioClip clip)
            return;

        audioSource.PlayOneShot(clip, entry.volume);
    }

    /// <summary>3D one-shot at a world position (e.g. stone impact away from the enemy).</summary>
    public void PlayClipAt(string actionName, Vector3 worldPosition)
    {
        if (audioData == null) return;

        var entry = audioData.GetSound(actionName);
        if (entry.clips == null || entry.clips.Length == 0)
            return;

        Object clipObject = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clipObject is not AudioClip clip)
            return;

        AudioSource.PlayClipAtPoint(clip, worldPosition, entry.volume);
    }
}
