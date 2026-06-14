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
    [Tooltip("Max distance to stand still and throw.")]
    [SerializeField] private float rangedAttackRange = 10f;
    [SerializeField] private float projectileSpeed = 18f;
    [Tooltip("Throw damage as % of scaled currentAttack.")]
    [SerializeField] private float throwDamage = 85f;
    [SerializeField] private float rangedAttackCooldown = 2.8f;

    public float ThrowMinDistance => throwMinDistance;
    public float RangedAttackRange => rangedAttackRange;
    public float ProjectileSpeed => projectileSpeed;
    public float ThrowDamage => throwDamage;
    public float RangedAttackCooldown => rangedAttackCooldown;
}
