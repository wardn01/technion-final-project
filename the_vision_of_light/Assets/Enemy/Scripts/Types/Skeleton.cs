using UnityEngine;

/// <summary>
/// Weakest normal enemy tier (Hilichurl-style grunt).
/// Uses default <see cref="NormalEnemy"/> AI: patrol, chase, one melee swing, camp reset.
/// </summary>
/// <remarks>
/// Animation events on the Attack clip:
/// <list type="bullet">
///   <item><c>PlayEnemySound("Attack")</c> — via <see cref="EnemyAudioEmitter"/></item>
///   <item><c>AnimHit()</c> — damage frame</item>
///   <item><c>ResetCombatStates()</c> — resume NavMesh after swing</item>
/// </list>
/// Stats: <c>Skeleton/Data/SkeletonData.asset</c>.
/// Audio: <c>Skeleton/Data/Audio/Skeleton_Audio_Library.asset</c>.
/// </remarks>
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
