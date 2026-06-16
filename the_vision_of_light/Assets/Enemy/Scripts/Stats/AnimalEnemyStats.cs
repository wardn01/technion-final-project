using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Base stats for animal enemies (e.g. <see cref="Bear"/>).
    /// Animal tier sits above normal mobs like Goblin. Tune per-enemy in <c>Bear/Data/BearData.asset</c>.
    /// </summary>
    public abstract class AnimalEnemyStats : EnemyBaseStats
    {
        [Header("Animal Combat Stats")]
        [Tooltip("Attack damage as % of scaled currentAttack.")]
        [SerializeField] private float animalDamage = 100f;
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float attackCooldown = 2f;

        public float AnimalDamage => animalDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
    }
}
