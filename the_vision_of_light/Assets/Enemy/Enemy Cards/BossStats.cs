using UnityEngine;

[CreateAssetMenu(fileName = "NewBossStats", menuName = "Game Data/Boss Stats")]
public class BossStats : EnemyStats
{
    [Header("Boss Special Abilities")]
    [SerializeField] private float skillDamage = 50f;     
    [SerializeField] private float aoeRadius = 5f;        
    public float SkillDamage => skillDamage;
    public float AoeRadius => aoeRadius;
}