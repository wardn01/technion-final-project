using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Game Data/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string enemyName = "New Enemy";
    
    [Header("Combat Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    
[Header("Movement & AI")]
    [SerializeField] private float walkSpeed = 1.5f; 
    [SerializeField] private float runSpeed = 4f;    
    [SerializeField] private float chaseRange = 10f; 
    // 🛑 ضفنا سرعة الدوران هون
    [SerializeField] private float rotationSpeed = 10f;

    public string EnemyName => enemyName;
    public float MaxHealth => maxHealth;
    public float Damage => damage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    
    public float WalkSpeed => walkSpeed; 
    public float RunSpeed => runSpeed;
    public float ChaseRange => chaseRange;

    public float RotationSpeed => rotationSpeed;
}