using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Data")]
    [SerializeField] protected EnemyBaseStats stats; 

    [Header("Base Components")]
    protected float currentHealth;
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

        if (stats != null)
        {
            currentHealth = stats.MaxHealth;
            if (agent != null) agent.speed = stats.WalkSpeed; 
            if (enemyUI != null) enemyUI.SetupHealthBar(stats.MaxHealth);
            Debug.Log($"{stats.EnemyName} initialized successfully.");
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

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        UpdateHealthUI();

        if (damageTextPrefab != null)
        {
            Vector3 spawnPos = textSpawnPoint != null ? textSpawnPoint.position : transform.position + Vector3.up * 2f;
            GameObject textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
            textObj.GetComponent<DamageText>()?.Setup(amount);
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

        Destroy(gameObject, 5f);
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

    protected void ExecuteMeleeAttack(float damageAmount, float attackRange, float maxAngle = 60f)
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
                    pHealth.TakeDamage(damageAmount);
                    Debug.Log($"[{gameObject.name}] Hit the player for {damageAmount} damage!");
                }
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Missed! Player dodged out of angle.");
            }
        }
    }
}