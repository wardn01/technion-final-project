using UnityEngine;

public class Skeleton : NormalEnemy
{
    protected override void PerformAttack()
    {
        if (anim != null) 
        {
            anim.SetTrigger("Attack"); 
        }
    }

    public void AnimHit()
    {
        if (target != null && MeleeStats != null) 
        {
            float distance = Vector3.Distance(transform.position, target.position);
            
            if (distance <= MeleeStats.NormalAttackRange + 0.5f)
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                directionToTarget.y = 0; 
                float angle = Vector3.Angle(transform.forward, directionToTarget);

                if (angle <= 60f) 
                {
                    PlayerHealth pHealth = target.GetComponent<PlayerHealth>();
                    if (pHealth != null)
                    {
                        pHealth.TakeDamage(MeleeStats.NormalDamage);
                        Debug.Log($"[{stats.EnemyName}] slashed the player with {MeleeStats.NormalDamage} damage!");
                    }
                }
                else
                {
                    Debug.Log("Player dodged the attack! The player was behind the enemy.");
                }
            }
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("The skeleton shattered into pieces!");
    }
}