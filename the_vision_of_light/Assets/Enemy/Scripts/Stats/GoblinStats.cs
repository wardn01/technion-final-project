using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// ScriptableObject stats for <see cref="Goblin"/> — mid-tier normal mob (Mitachurl-style).
    /// Hybrid melee + bone throw. Tune in <c>Goblin/Data/GoblinData.asset</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "GoblinData", menuName = "Game Data/Enemy/Normal/Goblin Stats")]
    public class GoblinStats : NormalEnemyStats
    {
        [Header("Ranged Combat (Bone Throw)")]
        [Tooltip("Max distance to stand still and throw bones.")]
        [SerializeField] private float rangedAttackRange = 11f;
        [Tooltip("Within this range the goblin chases with sword drawn before melee.")]
        [SerializeField] private float meleeChargeRange = 6f;
        [Tooltip("Rigidbody impulse applied to the bone projectile.")]
        [SerializeField] private float projectileSpeed = 14f;
        [Tooltip("Throw damage as % of scaled currentAttack.")]
        [SerializeField] private float rangedDamage = 75f;
        [SerializeField] private float rangedAttackCooldown = 2.5f;

        public float RangedAttackRange => rangedAttackRange;
        public float MeleeChargeRange => meleeChargeRange;
        public float ProjectileSpeed => projectileSpeed;
        public float RangedDamage => rangedDamage;
        public float RangedAttackCooldown => rangedAttackCooldown;
    }
}
