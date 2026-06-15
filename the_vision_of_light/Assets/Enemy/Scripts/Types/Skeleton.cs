using UnityEngine;

/// <summary>
/// Weakest normal enemy — single melee attack and camp reset. Data: Skeleton/Data/SkeletonData.asset.
/// </summary>
[RequireComponent(typeof(EnemyAudioEmitter))]
public class Skeleton : NormalEnemy
{
    /// <summary>Animation event — applies melee damage at the swing frame.</summary>
    public void AnimHit()
    {
        if (MeleeStats == null) return;

        float damageMultiplier = MeleeStats.NormalDamage / 100f;
        ExecuteMeleeAttack(damageMultiplier, MeleeStats.NormalAttackRange);
    }

    protected override void PerformAttack()
    {
        if (anim != null)
            anim.SetTrigger("Attack");
    }
}
