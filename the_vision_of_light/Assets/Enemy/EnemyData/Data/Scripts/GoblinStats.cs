using UnityEngine;

[CreateAssetMenu(fileName = "NewGoblinStats", menuName = "Game Data/EnemyData/Goblin Stats")]
public class GoblinStats : NormalEnemyStats
{
    [Header("Ranged Combat Settings")]
    [SerializeField] private float rangedAttackRange = 7f;
    [SerializeField] private float meleeChargeRange = 5f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float rangedDamage = 10f; 
    [SerializeField] private float rangedAttackCooldown = 3f;

    public float RangedAttackRange => rangedAttackRange;
    public float MeleeChargeRange => meleeChargeRange;
    public float ProjectileSpeed => projectileSpeed;
    public float RangedDamage => rangedDamage;
    public float RangedAttackCooldown => rangedAttackCooldown;
}