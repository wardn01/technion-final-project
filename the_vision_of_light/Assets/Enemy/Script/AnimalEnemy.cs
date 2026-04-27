using UnityEngine;
using UnityEngine.AI;

public class AnimalEnemy : EnemyBase
{
    protected float distanceToTarget;
    protected float lastAttackTime;
    private Vector3 startingPosition;
    private float waitTimer;
    private float pathUpdateTimer; 
    
    protected PlayerHealth playerHealth; 
    protected EnemyStatusEffects statusEffects; 

    protected AnimalEnemyStats AnimalStats => stats as AnimalEnemyStats;

    protected override void Start()
    {
        base.Start(); 

        statusEffects = GetComponent<EnemyStatusEffects>(); 

        if (target != null)
        {
            playerHealth = target.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null) target = playerHealth.transform; 
        }

        startingPosition = transform.position; 
    }

    protected virtual void Update()
    {
        if (isDead || target == null || playerHealth == null) return;

        if (playerHealth.isDead)
        {
            StopAgent();
            isAttackingBase = false;
            if (anim != null) 
            {
                anim.SetFloat("Speed", 0f);
            }
            return; 
        }

        UpdateBlendTree(); 

        if (isHitBase || isAttackingBase) 
        {
            StopAgent();
            return; 
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (AnimalStats != null && distanceToTarget <= AnimalStats.AttackRange)
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

        if (AnimalStats != null && Time.time >= lastAttackTime + AnimalStats.AttackCooldown)
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