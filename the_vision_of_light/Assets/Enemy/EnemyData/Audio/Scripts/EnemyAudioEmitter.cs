using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudioEmitter : MonoBehaviour
{
    public EnemyAudioData audioData; 
    private AudioSource audioSource;

    void Awake() {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; 
        audioSource.playOnAwake = false;
    }

    public void PlayEnemySound(string actionName) {
        if (audioData == null) return;

        var entry = audioData.GetSound(actionName);
        if (entry.clips != null && entry.clips.Length > 0) {
            AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
            audioSource.PlayOneShot(clip, entry.volume);
        }
    }
}