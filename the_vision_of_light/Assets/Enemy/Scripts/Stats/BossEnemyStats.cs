using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Boss stats base — phase 1/2 combat values, enrage threshold, and camp-reset leash.
    /// </summary>
    public abstract class BossEnemyStats : EnemyBaseStats
    {
        [Header("Phase 1 Combat")]
        [SerializeField] private float phase1Damage = 100f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float phase1AttackCooldown = 2.5f;

        [Header("Enrage / Phase 2")]
        [Tooltip("Health fraction (0–1) at which the boss enters phase 2.")]
        [SerializeField] private float enrageHealthPercentage = 0.5f;
        [SerializeField] private float phase2NormalDamage = 130f;
        [SerializeField] private float phase2AttackCooldown = 1.5f;

        [Header("Special Attacks")]
        [SerializeField] private float heavyAttackDamage = 150f;

        [Header("Aggro / Camp Reset")]
        [Tooltip("Player farther than this triggers return-to-spawn.")]
        [SerializeField] private float aggroLeashDistance = 32f;
        [Tooltip("Boss farther than this from spawn also triggers camp reset.")]
        [SerializeField] private float maxLeashDistance = 35f;

        public float Phase1Damage => phase1Damage;
        public float AttackRange => attackRange;
        public float Phase1AttackCooldown => phase1AttackCooldown;
        public float EnrageHealthPercentage => enrageHealthPercentage;
        public float Phase2NormalDamage => phase2NormalDamage;
        public float Phase2AttackCooldown => phase2AttackCooldown;
        public float HeavyAttackDamage => heavyAttackDamage;
        public float AggroLeashDistance => aggroLeashDistance;
        public float MaxLeashDistance => maxLeashDistance;
    }
}
