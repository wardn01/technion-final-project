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
        if (target != null) 
        {
            float distance = Vector3.Distance(transform.position, target.position);
            
            if (distance <= stats.AttackRange + 0.5f)
            {
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                directionToTarget.y = 0; 
                float angle = Vector3.Angle(transform.forward, directionToTarget);

                if (angle <= 60f) 
                {
                    PlayerHealth pHealth = target.GetComponent<PlayerHealth>();
                    if (pHealth != null)
                    {
                        pHealth.TakeDamage(stats.Damage);
                        Debug.Log($"[{stats.EnemyName}] slashed you with {stats.Damage} damage!");
                    }
                }
                else
                {
                    Debug.Log("You dodged the attack! You were behind the enemy!");
                }
            }
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("The skeleton shattered into 100 bones!");
    }
}