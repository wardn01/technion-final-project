using UnityEngine;
using System.Collections.Generic;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// ScriptableObject library mapping action names to one-shot enemy SFX clips.
    /// </summary>
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

        /// <summary>Finds a sound entry by action name (case-insensitive).</summary>
        public SoundEntry GetSound(string action) {
            return sounds.Find(s => s.actionName.ToLower() == action.ToLower());
        }
    }
}
