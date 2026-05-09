using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Data")]
    [SerializeField] protected EnemyBaseStats stats; 

    [Header("Current Scaled Stats")]
    public int enemyLevel = 1;
    public float currentMaxHealth;
    public float currentAttack;
    public float currentDefense;
    protected float currentHealth;
    
    [Header("Base Components")]
    protected Animator anim;
    protected NavMeshAgent agent;
    protected bool isDead = false;
    public bool IsDead => isDead;
    protected EnemyUI enemyUI;
    protected Transform target; 

    [Header("Combat States")]
    public bool isHitBase = false;
    public bool isAttackingBase = false;

    [Header("UI & Effects")]
    public GameObject damageTextPrefab;
    public Transform textSpawnPoint;

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyUI = GetComponentInChildren<EnemyUI>();
    }
    
    protected virtual void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;

        if (PlayerData.Instance == null) 
        {
            PlayerData.Instance = FindFirstObjectByType<PlayerData>();
        }

        if (stats != null)
        {
            ScaleStatsWithPlayer();
            currentHealth = currentMaxHealth;
            
            if (agent != null) agent.speed = stats.WalkSpeed; 
            
            if (enemyUI != null) 
            {
                enemyUI.SetupHealthBar(currentMaxHealth);
                enemyUI.SetupEnemyInfo(stats.EnemyName, enemyLevel);
            }
        }
    }

    protected virtual void ScaleStatsWithPlayer()
    {
        if (PlayerData.Instance != null && stats != null)
        {
            int playerLevel = PlayerData.Instance.currentLevel;

            int playerTier = playerLevel / 10;
            int enemyTier = playerTier + 1;
            
            int minEnemyLevel = enemyTier * 10;       
            int maxEnemyLevel = minEnemyLevel + 9;    
            
            enemyLevel = Random.Range(minEnemyLevel, maxEnemyLevel + 1);

            currentMaxHealth = stats.BaseMaxHealth + (enemyLevel * stats.HpScale);
            currentAttack = stats.BaseAttack + (enemyLevel * stats.AtkScale);
            currentDefense = stats.BaseDefense + (enemyLevel * stats.DefScale);
        }
        else if (stats != null)
        {
            enemyLevel = 10; 
            currentMaxHealth = stats.BaseMaxHealth + (10 * stats.HpScale);
            currentAttack = stats.BaseAttack + (10 * stats.AtkScale);
            currentDefense = stats.BaseDefense + (10 * stats.DefScale);
        }
    }

    public virtual void ResetCombatStates()
    {
        isHitBase = false;
        isAttackingBase = false;
        
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !isDead)
        {
            agent.isStopped = false;
        }
    }

    public void StopAgent()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    protected void FaceTarget()
    {
        if (target == null || stats == null) return;
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; 
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * stats.RotationSpeed);
    }

    public virtual void TakeDamage(float incomingDamage)
    {
        if (isDead) return;

        float damageMultiplier = 100f / (100f + currentDefense);
        float finalDamage = incomingDamage * damageMultiplier;

        int finalDamageInt = Mathf.RoundToInt(finalDamage);
        finalDamageInt = Mathf.Max(1, finalDamageInt); 

        currentHealth -= finalDamageInt;
        UpdateHealthUI();

        if (damageTextPrefab != null)
        {
            Vector3 spawnPos = textSpawnPoint != null ? textSpawnPoint.position : transform.position + Vector3.up * 2f;
            GameObject textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
            textObj.GetComponent<DamageText>()?.Setup(finalDamageInt);
        }

        if (currentHealth <= 0) Die();
        else PlayHitEffect();
    }

    protected virtual void PlayHitEffect()
    {
        if (anim != null) anim.SetTrigger("Hit");
    }

    protected virtual void UpdateHealthUI()
    {
        if (enemyUI != null) enemyUI.UpdateHealthBar(currentHealth);
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        if (anim != null) anim.SetTrigger("Die");
        StopAgent();
        if (agent != null) agent.enabled = false;
        GetComponent<Collider>().enabled = false;
        if (enemyUI != null) enemyUI.gameObject.SetActive(false);
        
        DropLoot();
        GiveXP();

        Destroy(gameObject, 5f);
    }

    private void GiveXP()
    {
        if (stats == null || PlayerData.Instance == null) return;
        
        PlayerData.Instance.AddXP(stats.XPReward);
    }

    private void DropLoot()
    {
        if (InventoryManager.Instance == null || stats == null || stats.LootTable == null) return;

        foreach (var loot in stats.LootTable)
        {
            float roll = Random.Range(0f, 100f);
            
            if (roll <= loot.dropChance)
            {
                int amount = Random.Range(loot.minAmount, loot.maxAmount + 1);
                InventoryManager.Instance.AddItem(loot.item, amount);
            }
        }
    }

    protected void ExecuteMeleeAttack(float damageMultiplier = 1f, float attackRange = 2f, float maxAngle = 60f)
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        
        if (distance <= attackRange + 0.8f)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            directionToTarget.y = 0; 
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle <= maxAngle) 
            {
                PlayerHealth pHealth = target.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    float finalDamage = currentAttack * damageMultiplier;
                    pHealth.TakeDamage(finalDamage);
                }
            }
        }
    }
}