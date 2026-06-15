using UnityEngine;

/// <summary>
/// Weak melee enemy — dual attack animations via <c>AttackIndex</c>. Data: Imp/Data/ImpData.asset.
/// </summary>
[RequireComponent(typeof(EnemyAudioEmitter))]
public class Imp : NormalEnemy
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
        if (anim == null) return;

        anim.SetInteger("AttackIndex", Random.Range(0, 2));
        anim.SetTrigger("Attack");
    }

    protected override void UpdateBlendTree()
    {
        if (anim == null)
            return;

        if (isHitBase || isAttackingBase)
        {
            anim.SetFloat("Speed", 0f);
            return;
        }

        if (agent == null || stats == null)
            return;

        // Blend tree: 0 = Idle, 0.5 = Walk, 1 = Move @ 1.5x (run)
        float velocity = agent.velocity.magnitude;
        float walkSpeed = stats.WalkSpeed;
        float runSpeed = stats.RunSpeed;

        float blend;
        if (velocity < 0.05f)
            blend = 0f;
        else if (velocity <= walkSpeed)
            blend = walkSpeed > 0f ? Mathf.Lerp(0f, 0.5f, velocity / walkSpeed) : 0.5f;
        else
            blend = runSpeed > walkSpeed
                ? Mathf.Lerp(0.5f, 1f, (velocity - walkSpeed) / (runSpeed - walkSpeed))
                : 1f;

        anim.SetFloat("Speed", blend);
    }
}
