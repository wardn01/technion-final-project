using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NormalEnemy : EnemyBase
{
    [Header("AI Logic")]
    protected float distanceToTarget;
    protected float lastAttackTime;
    protected Vector3 startingPosition;
    private float waitTimer;
    private float pathUpdateTimer; 
    
    protected PlayerHealth playerHealth; 
    protected EnemyStatusEffects statusEffects;

    [Header("Camp Reset System (Genshin Style)")]
    public bool isReturningToCamp = false;

    protected NormalEnemyStats MeleeStats => stats as NormalEnemyStats;

    protected override IEnumerator Start()
    {
        yield return base.Start();
        
        statusEffects = GetComponent<EnemyStatusEffects>();
        if (target != null)
        {
            playerHealth = target.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            
            if (playerHealth != null)
            {
                target = playerHealth.transform; 
            }
        }

        startingPosition = transform.position; 
    }

    protected virtual void Update()
    {
        if (isDead || target == null || playerHealth == null) return;

        UpdateBlendTree();

        if (isReturningToCamp)
        {
            ReturnToCampBehavior();
            return;
        }

        if (playerHealth.isDead)
        {
            StopAgent();
            isAttackingBase = false;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return; 
        }

        if (isHitBase || isAttackingBase) 
        {
            StopAgent();
            return; 
        }

        float distanceFromCamp = Vector3.Distance(transform.position, startingPosition);
        if (distanceFromCamp > MeleeStats.MaxLeashDistance)
        {
            TriggerCampReset();
            return;
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (MeleeStats != null && distanceToTarget <= MeleeStats.NormalAttackRange)
        {
            AttackBehavior(); 
        }
        else if (distanceToTarget <= stats.ChaseRange)
        {
            ChaseBehavior(); 
        }
        else
        {
            PatrolBehavior(); 
        }
    }

    private void TriggerCampReset()
    {
        isReturningToCamp = true;
        isHitBase = false;
        isAttackingBase = false;
        
        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Hit");
        }

        currentHealth = currentMaxHealth;
        UpdateHealthUI();
    }

    private void ReturnToCampBehavior()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = stats.RunSpeed;
            agent.SetDestination(startingPosition);

            if (Vector3.Distance(transform.position, startingPosition) <= agent.stoppingDistance + 0.5f)
            {
                isReturningToCamp = false;
            }
        }
    }

    public override void TakeDamage(float amount)
    {
        if (isReturningToCamp) return;

        base.TakeDamage(amount);
    }

    protected override void PlayHitEffect()
    {
        base.PlayHitEffect();
        isHitBase = true;     
        isAttackingBase = false; 
        StopAgent();
    }

    protected virtual void AttackBehavior()
    {
        StopAgent();
        FaceTarget();

        if (MeleeStats != null && Time.time >= lastAttackTime + MeleeStats.NormalAttackCooldown)
        {
            isAttackingBase = true; 
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    protected virtual void PerformAttack()
    {
        if (anim != null) anim.SetTrigger("Attack");
    }

    protected virtual void UpdateBlendTree()
    {
        if (anim != null && agent != null)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    protected virtual void ChaseBehavior()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            
            float slowMulti = statusEffects != null ? statusEffects.SlowMultiplier : 1f;
            agent.speed = stats.RunSpeed * slowMulti; 

            if (Time.time >= pathUpdateTimer)
            {
                agent.SetDestination(target.position);
                pathUpdateTimer = Time.time + 0.2f; 
            }
        }
    }

    protected virtual void PatrolBehavior()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            float slowMulti = statusEffects != null ? statusEffects.SlowMultiplier : 1f;
            agent.speed = stats.WalkSpeed * slowMulti; 

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    Vector3 randomDir = Random.insideUnitSphere * 5f + startingPosition;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randomDir, out hit, 5f, 1))
                    {
                        agent.SetDestination(hit.position);
                    }
                    waitTimer = 3f; 
                }
            }
        }
    }
}