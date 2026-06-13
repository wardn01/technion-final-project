using UnityEngine;

/// <summary>
/// ScriptableObject stats for <see cref="Golem"/> — mid-boss tier above Orc mini-boss.
/// Melee + stone throw at range. Tune in <c>Golem/Data/GolemData.asset</c>.
/// </summary>
[CreateAssetMenu(fileName = "GolemData", menuName = "Game Data/Enemy/Boss/Golem Stats")]
public class GolemStats : BossEnemyStats
{
    [Header("Ranged Combat (Stone Throw)")]
    [Tooltip("Rigidbody impulse applied to the stone projectile.")]
    [SerializeField] private float projectileSpeed = 26f;
    [Tooltip("Throw damage as % of scaled currentAttack.")]
    [SerializeField] private float throwDamage = 85f;

    public float ProjectileSpeed => projectileSpeed;
    public float ThrowDamage => throwDamage;
}
