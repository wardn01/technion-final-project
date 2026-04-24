using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimalStats", menuName = "Game Data/Animal Stats")]
public class AnimalStats : EnemyStats
{
    [Header("Animal Instincts")]
    [SerializeField] private bool fleesWhenLowHealth = true;
    [SerializeField] private float fleeHealthPercentage = 0.3f;
    [SerializeField] private float fleeSpeedMultiplier = 1.5f;

    public bool FleesWhenLowHealth => fleesWhenLowHealth;
    public float FleeHealthPercentage => fleeHealthPercentage;
    public float FleeSpeedMultiplier => fleeSpeedMultiplier;
}