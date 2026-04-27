using UnityEngine;

public abstract class NormalEnemyStats : EnemyBaseStats
{
    [Header("Melee Combat Stats")]
    [SerializeField] private float normalDamage = 10f;
    [SerializeField] private float normalAttackRange = 1.5f;
    [SerializeField] private float normalAttackCooldown = 2f;

    public float NormalDamage => normalDamage;
    public float NormalAttackRange => normalAttackRange;
    public float NormalAttackCooldown => normalAttackCooldown;
}