using UnityEngine;

public class AnimalEnemy : EnemyBase
{
    [Header("Animal AI & Instincts")]
    [SerializeField] private bool fleesWhenLowHealth = true;
    [SerializeField] private float fleeHealthPercentage = 0.3f;
    
    private bool isFleeing = false;
    private float lastBiteTime;

    protected bool isAttacking = false; 
    protected bool isHit = false; 

    protected override void Start()
    {
        base.Start();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }

    protected virtual void Update()
    {
        if (isDead || target == null || agent == null) return;

        UpdateBlendTree(); 

        if (isAttacking || isHit) return;

        if (fleesWhenLowHealth && currentHealth <= (stats.MaxHealth * fleeHealthPercentage))
        {
            if (!isFleeing)
            {
                Debug.Log($"[{stats.EnemyName}] The animal is terrified and started fleeing!");
                isFleeing = true;
                agent.speed = stats.RunSpeed * 1.5f; 
                if (anim != null) anim.SetTrigger("Flee");
            }
            FleeBehavior();
            return; 
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= stats.AttackRange)
        {
            BiteAttack();
        }
        else if (distance <= stats.ChaseRange)
        {
            ChaseTarget();
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private void FleeBehavior()
    {
        agent.isStopped = false;
        Vector3 fleeDir = (transform.position - target.position).normalized;
        agent.SetDestination(transform.position + fleeDir * 5f);
    }

    private void ChaseTarget()
    {
        agent.isStopped = false;
        agent.speed = stats.RunSpeed;
        agent.SetDestination(target.position);
    }

    private void BiteAttack()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (Time.time >= lastBiteTime + stats.AttackCooldown)
        {
            isAttacking = true;
            Debug.Log($"[{stats.EnemyName}] bit you with {stats.Damage} damage!");
            if (anim != null) anim.SetTrigger("Bite");
            
            lastBiteTime = Time.time;
        }
    }

    private void UpdateBlendTree()
    {
        if (anim != null)
        {
            float currentVelocity = agent.velocity.magnitude;
            float normalizedSpeed = currentVelocity / stats.RunSpeed;
            anim.SetFloat("Speed", normalizedSpeed);
        }
    }

    public void AnimBiteEnd()
    {
        isAttacking = false;
    }

    protected override void PlayHitEffect()
    {
        base.PlayHitEffect(); 
        
        isAttacking = false; 
        isHit = true;        

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 
        }

        CancelInvoke("AnimHitEnd"); 
        Invoke("AnimHitEnd", 1f);   
    }

    public void AnimHitEnd()
    {
        isHit = false; 
    }
}