using UnityEngine;
using UnityEngine.AI;

public class NormalEnemy : EnemyBase
{
    [Header("AI Logic")]
    protected float distanceToTarget;
    protected float lastAttackTime;
    private Vector3 startingPosition;
    private float waitTimer;

    protected override void Start()
    {
        base.Start(); 
        startingPosition = transform.position; 
    }

    protected virtual void Update()
    {
        if (isDead || target == null || agent == null) return;

        UpdateBlendTree(); 

        if (isHitBase || isAttackingBase) 
        {
            StopAgent();
            return; 
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= stats.AttackRange)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            directionToTarget.y = 0; 
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle <= 30f) 
            {
                AttackBehavior(); 
            }
            else 
            {
                StopAgent(); 
                FaceTarget(); 
            }
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
        if (Time.time >= lastAttackTime + stats.AttackCooldown)
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

    private void UpdateBlendTree()
    {
        if (anim != null)
        {
            float normalizedSpeed = agent.velocity.magnitude / stats.RunSpeed;
            anim.SetFloat("Speed", normalizedSpeed);
        }
    }

    protected virtual void ChaseBehavior()
    {
        agent.isStopped = false;
        agent.speed = stats.RunSpeed; 
        agent.SetDestination(target.position);
    }

    protected virtual void PatrolBehavior()
    {
        agent.isStopped = false;
        agent.speed = stats.WalkSpeed; 

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