using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>Single loot entry with amount range and drop chance for enemy loot tables.</summary>
    [System.Serializable]
    public class LootDrop
    {
        public ItemData item;
        public int minAmount = 1;
        public int maxAmount = 1;
        [Range(0f, 100f)] 
        public float dropChance = 100f; 
    }

    /// <summary>
    /// Base ScriptableObject stats shared by all enemies — scaling, movement, loot, and XP.
    /// </summary>
    public abstract class EnemyBaseStats : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string enemyName = "New Enemy";

        [Header("Base Combat Stats")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseAttack = 10f;
        [SerializeField] private float baseDefense = 5f;

        [Header("Scaling Factors (Per Player Level)")]
        [SerializeField] private float hpScale = 20f;
        [SerializeField] private float atkScale = 5f;
        [SerializeField] private float defScale = 2f;

        [Header("Movement & AI")]
        [SerializeField] private float walkSpeed = 1.5f; 
        [SerializeField] private float runSpeed = 4f;    
        [SerializeField] private float chaseRange = 10f; 
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Loot System")]
        [SerializeField] private LootDrop[] lootTable;

        [Header("Rewards")]
        [Tooltip("XP granted to the player on kill (via PlayerData.AddXP).")]
        [SerializeField] private int xpReward = 20;

        public string EnemyName => enemyName;
        public float BaseMaxHealth => baseMaxHealth;
        public float BaseAttack => baseAttack;
        public float BaseDefense => baseDefense;
        public float HpScale => hpScale;
        public float AtkScale => atkScale;
        public float DefScale => defScale;

        public float WalkSpeed => walkSpeed; 
        public float RunSpeed => runSpeed;
        public float ChaseRange => chaseRange;
        public float RotationSpeed => rotationSpeed;

        public LootDrop[] LootTable => lootTable; 
        public int XPReward => xpReward;
    }
}
