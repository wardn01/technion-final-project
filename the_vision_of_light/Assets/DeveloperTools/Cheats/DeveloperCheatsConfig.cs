using UnityEngine;

namespace VisionOfLight.DeveloperTools
{
    /// <summary>
    /// Tunable values for the developer cheats. Serialized on <see cref="DeveloperCheatsManager"/>
    /// so nothing is stored in ScriptableObjects or save files.
    /// </summary>
    [System.Serializable]
    public class DeveloperCheatsConfig
    {
        [Header("High Damage (F2)")]
        [Min(1f)]
        [Tooltip("Player damage multiplier while High Damage is enabled.")]
        public float damageMultiplier = 100f;

        [Header("Auto Heal (Shift+F3)")]
        [Min(1f)]
        [Tooltip("Health restored per second while automatic regeneration is enabled.")]
        public float autoHealPerSecond = 50f;

        [Header("Fast Move (F6)")]
        [Min(1f)]
        [Tooltip("Movement speed multiplier while Fast Move is enabled.")]
        public float moveSpeedMultiplier = 2f;

        // Fly Mode (F7) speeds live on the DeveloperFlyController component.
    }
}
