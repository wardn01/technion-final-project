using UnityEngine;

[System.Serializable]
public class LootDrop
{
    public ItemData item;
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0f, 100f)] 
    public float dropChance = 100f; 
}

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

    [Header("Loot System")]
    [SerializeField] private LootDrop[] lootTable;

    public string EnemyName => enemyName;
    public float MaxHealth => maxHealth;
    public float WalkSpeed => walkSpeed; 
    public float RunSpeed => runSpeed;
    public float ChaseRange => chaseRange;
    public float RotationSpeed => rotationSpeed;
    
    public LootDrop[] LootTable => lootTable; 
}