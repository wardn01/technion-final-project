using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Bear : AnimalEnemy
{
    protected BearStats SpecificBearStats => stats as BearStats;
    private float cycleTimer;
    private bool isPlayerNearby;
    private bool isRoaring = false;

    protected override IEnumerator Start()
    {
        yield return base.Start();
        
        SetSleepState(true);
    }

    protected override void Update()
    {
        if (isDead || target == null || playerHealth == null) return;

        if (playerHealth.isDead)
        {
            StopAgent();
            isAttackingBase = false;
            if (anim != null) 
            {
                anim.SetFloat("Speed", 0f);
                anim.SetBool("InCombat", false);
                if (!anim.GetBool("IsSleeping")) SetSleepState(true);
            }
            return; 
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);
        isPlayerNearby = distanceToTarget <= stats.ChaseRange;

        if (isPlayerNearby)
        {
            if (anim.GetBool("IsSleeping")) 
            {
                SetSleepState(false);
                anim.SetTrigger("Buff"); 
                cycleTimer = 0f; 
                isRoaring = true;
                StopAgent();
            }

            if (isRoaring) return; 

            base.Update(); 
        }
        else
        {
            if (isHitBase || isAttackingBase || isRoaring)
            {
                StopAgent();
                return; 
            }

            HandleLifeCycle();
        }
    }

    private void HandleLifeCycle()
    {
        cycleTimer += Time.deltaTime;
        bool currentlySleeping = anim.GetBool("IsSleeping");

        if (SpecificBearStats == null) return; 

        if (currentlySleeping)
        {
            if (agent != null && agent.isOnNavMesh) agent.velocity = Vector3.zero;

            if (cycleTimer >= SpecificBearStats.SleepDuration) 
            {
                SetSleepState(false);
                cycleTimer = 0f;
            }
        }
        else
        {
            PatrolBehavior(); 

            if (cycleTimer >= SpecificBearStats.WalkDuration) 
            {
                SetSleepState(true);
                cycleTimer = 0f;
            }
        }
    }

    private void SetSleepState(bool sleep)
    {
        if (anim != null)
        {
            anim.SetBool("IsSleeping", sleep);
        }
        
        if (sleep)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
    }

    protected override void PerformAttack()
    {
        if (anim != null)
        {
            int randomAttack = Random.Range(1, 4);
            anim.SetInteger("AttackIndex", randomAttack);
            anim.SetTrigger("Attack");
        }
    }

    public void AnimHit()
    {
        if (AnimalStats != null) 
        {
            int currentAttack = anim.GetInteger("AttackIndex");
            float damagePercentage = AnimalStats.AnimalDamage;

            switch (currentAttack)
            {
                case 1:
                    damagePercentage = AnimalStats.AnimalDamage; 
                    break;
                case 2:
                    damagePercentage = AnimalStats.AnimalDamage * 1.5f; 
                    break;
                case 3:
                    damagePercentage = AnimalStats.AnimalDamage * 2f; 
                    break;
            }

            float damageMultiplier = damagePercentage / 100f;
            ExecuteMeleeAttack(damageMultiplier, AnimalStats.AttackRange);
        }
    }

    protected override void Die()
    {
        base.Die();
    }

    public void EndAttack()
    {
        isAttackingBase = false;
    }

    public void EndHit()
    {
        isHitBase = false;
    }

    public void EndBuff()
    {
        isRoaring = false;
    }
}