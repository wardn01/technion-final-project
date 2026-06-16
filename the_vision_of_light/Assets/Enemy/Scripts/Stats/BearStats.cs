using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// ScriptableObject stats for <see cref="Bear"/> — forest animal tier, slightly above Goblin.
    /// Sleep/walk cycle when idle; roars then fights when player enters chase range.
    /// Tune in <c>Bear/Data/BearData.asset</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "BearData", menuName = "Game Data/Enemy/Animal/Bear Stats")]
    public class BearStats : AnimalEnemyStats
    {
        [Header("Bear Life Cycle")]
        [Tooltip("Seconds the bear stays asleep while patrolling area is clear.")]
        [SerializeField] private float sleepDuration = 12f;
        [Tooltip("Seconds of walking patrol before sleeping again.")]
        [SerializeField] private float walkDuration = 20f;
        [Tooltip("Once aggroed, the bear keeps chasing until the player is farther than this.")]
        [SerializeField] private float aggroLeashDistance = 28f;

        [Header("Multi-Hit Attacks")]
        [Tooltip("Attack index 2 damage as a multiplier of animalDamage.")]
        [SerializeField] private float heavyAttackMultiplier = 1.5f;
        [Tooltip("Attack index 3 damage as a multiplier of animalDamage.")]
        [SerializeField] private float slamAttackMultiplier = 2f;

        public float SleepDuration => sleepDuration;
        public float WalkDuration => walkDuration;
        public float AggroLeashDistance => aggroLeashDistance;
        public float HeavyAttackMultiplier => heavyAttackMultiplier;
        public float SlamAttackMultiplier => slamAttackMultiplier;
    }
}
