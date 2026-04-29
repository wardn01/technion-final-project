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
        if (MeleeStats != null) 
        {
            ExecuteMeleeAttack(MeleeStats.NormalDamage, MeleeStats.NormalAttackRange);
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("The skeleton shattered into pieces!");
    }
}