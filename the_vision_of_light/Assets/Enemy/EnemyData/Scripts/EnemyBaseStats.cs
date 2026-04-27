using UnityEngine;

public abstract class EnemyBaseStats : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string enemyName = "New Enemy";
    
    [Header("Health Stats")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Movement & AI")]
    [SerializeField] private float walkSpeed = 1.5f; 
    [SerializeField] private float runSpeed = 4f;    
    [SerializeField] private float chaseRange = 10f; 
    [SerializeField] private float rotationSpeed = 10f;

    public string EnemyName => enemyName;
    public float MaxHealth => maxHealth;
    public float WalkSpeed => walkSpeed; 
    public float RunSpeed => runSpeed;
    public float ChaseRange => chaseRange;
    public float RotationSpeed => rotationSpeed;
}