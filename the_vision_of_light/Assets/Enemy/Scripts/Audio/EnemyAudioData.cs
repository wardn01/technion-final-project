using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyAudio", menuName = "Audio/Enemy Audio Data")]
public class EnemyAudioData : ScriptableObject
{
    [System.Serializable]
    public struct SoundEntry {
        public string actionName;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume;
    }

    public List<SoundEntry> sounds;

    public SoundEntry GetSound(string action) {
        return sounds.Find(s => s.actionName.ToLower() == action.ToLower());
    }
}