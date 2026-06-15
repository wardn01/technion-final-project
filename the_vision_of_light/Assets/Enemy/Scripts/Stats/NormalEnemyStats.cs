using UnityEngine;

/// <summary>
/// Base stats for normal enemies (Skeleton, Goblin). Includes melee combat and camp-reset leash.
/// </summary>
public abstract class NormalEnemyStats : EnemyBaseStats
{
    [Header("Melee Combat Stats")]
    [Tooltip("Attack damage as % of scaled currentAttack (100 = full attack stat).")]
    [SerializeField] private float normalDamage = 100f;
    [SerializeField] private float normalAttackRange = 1.5f;
    [SerializeField] private float normalAttackCooldown = 2f;

    [Header("Camp Reset Settings")]
    [SerializeField] private float maxLeashDistance = 25f; 

    public float NormalDamage => normalDamage;
    public float NormalAttackRange => normalAttackRange;
    public float NormalAttackCooldown => normalAttackCooldown;
    public float MaxLeashDistance => maxLeashDistance; 
}