using UnityEngine;

/// <summary>
/// ScriptableObject stats for <see cref="MiniGolem"/> — melee punch + stone throw at range.
/// Tune in <c>MiniGolem/Data/MiniGolemData.asset</c>.
/// </summary>
[CreateAssetMenu(fileName = "MiniGolemData", menuName = "Game Data/Enemy/Normal/Mini Golem Stats")]
public class MiniGolemStats : NormalEnemyStats
{
    [Header("Ranged Combat (Stone Throw)")]
    [Tooltip("Will not throw while closer than this.")]
    [SerializeField] private float throwMinDistance = 4f;
    [Tooltip("Max distance to consider a throw while chasing.")]
    [SerializeField] private float rangedAttackRange = 10f;
    [Tooltip("Chance to stop and throw when in throw range and off cooldown — same idea as Golem.")]
    [SerializeField] [Range(0f, 1f)] private float throwChance = 0.35f;
    [SerializeField] private float projectileSpeed = 18f;
    [Tooltip("Throw damage as % of scaled currentAttack.")]
    [SerializeField] private float throwDamage = 85f;
    [SerializeField] private float rangedAttackCooldown = 2.8f;

    public float ThrowMinDistance => throwMinDistance;
    public float RangedAttackRange => rangedAttackRange;
    public float ThrowChance => throwChance;
    public float ProjectileSpeed => projectileSpeed;
    public float ThrowDamage => throwDamage;
    public float RangedAttackCooldown => rangedAttackCooldown;
}
