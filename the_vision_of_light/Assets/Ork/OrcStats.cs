using UnityEngine;

[CreateAssetMenu(fileName = "NewOrcStats", menuName = "Game Data/EnemyData/Orc Stats")]
public class OrcStats : NormalEnemyStats
{
    [Header("Orc Boss Settings")]
    [Tooltip("Health percentage at which the orc becomes enraged (0.5 means 50%)")]
    [SerializeField] private float enrageHealthPercentage = 0.5f; 
    
    [Header("Phase 2 Combat Stats")]
    [Tooltip("Normal attack damage after enrage (Phase 2)")]
    [SerializeField] private float phase2NormalDamage = 15f;

    [Tooltip("Attack cooldown after enrage (lower = faster attacks)")]
    [SerializeField] private float phase2AttackCooldown = 1f; 

    [Header("Special Attacks")]
    [Tooltip("Heavy attack damage (fire pillar attack)")]
    [SerializeField] private float heavyAttackDamage = 30f;

    public float EnrageHealthPercentage => enrageHealthPercentage;
    public float Phase2NormalDamage => phase2NormalDamage;
    public float Phase2AttackCooldown => phase2AttackCooldown;
    public float HeavyAttackDamage => heavyAttackDamage;
}