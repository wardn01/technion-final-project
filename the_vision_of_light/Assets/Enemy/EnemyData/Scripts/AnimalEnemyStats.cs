using UnityEngine;

public abstract class AnimalEnemyStats : EnemyBaseStats
{
    [Header("Animal Combat Stats")]
    [SerializeField] private float animalDamage = 15f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 2f; 

    public float AnimalDamage => animalDamage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
}