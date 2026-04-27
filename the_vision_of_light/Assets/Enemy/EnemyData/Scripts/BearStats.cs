using UnityEngine;

[CreateAssetMenu(fileName = "NewBearStats", menuName = "Game Data/EnemyData/Bear Stats")]
public class BearStats : AnimalEnemyStats
{
    [Header("Bear Cycle Settings")]
    [SerializeField] private float sleepDuration = 10f;
    [SerializeField] private float walkDuration = 15f;

    public float SleepDuration => sleepDuration;
    public float WalkDuration => walkDuration;
}