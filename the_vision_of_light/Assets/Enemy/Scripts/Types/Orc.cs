using UnityEngine;

/// <summary>
/// Boss enemy: two combat phases with an enrage cutscene, multi-attack index, and heavy fire strike.
/// </summary>
/// <remarks>
/// Phase 1: melee attacks 1–2 at normal damage/cooldown.
/// Phase 2: triggered at <see cref="BossEnemyStats.EnrageHealthPercentage"/> HP — invincible rage
/// animation, then faster attacks 3–4 including heavy fire (<c>AnimHeavyHit</c>).
/// Animation events: see clips under <c>Orc/Animation/</c> (rage calls <c>EndEnrage</c>).
/// Stats: <c>Orc/Data/OrcData.asset</c>.
/// Audio: <c>Orc/Data/Audio/Orc_Audio_Library.asset</c>.
/// VFX: <c>Orc/VFX/Orc_RageVFX.prefab</c>, <c>Orc/VFX/Orc_HeavyAttackVFX.prefab</c>.
/// UI: boss HP bar on player HUD via <see cref="BossHealthBarUI"/> (not world-space).
/// Camp reset: returns to spawn and heals when the player escapes (see <see cref="BossEnemy"/>).
/// </remarks>
[RequireComponent(typeof(EnemyAudioEmitter))]
public class Orc : BossEnemy
{
    private bool isEnraged;
    private bool phase2Triggered;
    private bool isInvincible;

    protected override bool IsInPhase2 => isEnraged;
    protected override bool SuppressHitReaction => isInvincible || isEnraged;

    protected override void Update()
    {
        if (isDead || target == null) return;

        if (isInvincible)
            FaceTarget();

        base.Update();
    }

    public override void TakeDamage(float damage, bool playHitReaction = true)
    {
        if (isInvincible || BossStats == null) return;

        float enrageThreshold = currentMaxHealth * BossStats.EnrageHealthPercentage;
        float damageMultiplier = 100f / (100f + currentDefense);
        float predictedFinalDamage = damage * damageMultiplier;

        if (!phase2Triggered && (currentHealth - predictedFinalDamage) <= enrageThreshold)
        {
            float actualDamageAllowed = currentHealth - enrageThreshold;
            float rawDamageAllowed = actualDamageAllowed / damageMultiplier;

            if (rawDamageAllowed > 0)
                base.TakeDamage(rawDamageAllowed, playHitReaction);

            TriggerEnragePhase();
            return;
        }

        base.TakeDamage(damage, playHitReaction);
    }

    private void TriggerEnragePhase()
    {
        phase2Triggered = true;
        isEnraged = true;
        isHitBase = false;
        isAttackingBase = true;
        isInvincible = true;

        StopAgent();
        anim.ResetTrigger("Hit");

        EnemyStatusEffects statusEffects = GetComponent<EnemyStatusEffects>();
        if (statusEffects != null)
            statusEffects.ResetAllEffects();

        anim.SetTrigger("Enrage");

        EnemyVFX vfxController = GetComponent<EnemyVFX>();
        if (vfxController != null)
            vfxController.PlayRageVFX();
    }

    protected override void PerformAttack()
    {
        int attackIndex = isEnraged ? Random.Range(3, 5) : Random.Range(1, 3);
        anim.SetInteger("AttackIndex", attackIndex);
        anim.SetTrigger("Attack");
    }

    public void AnimHit()
    {
        if (BossStats == null) return;

        float damageMultiplier = GetDamagePercent() / 100f;
        ExecuteMeleeAttack(damageMultiplier, BossStats.AttackRange);
    }

    public void AnimHeavyHit()
    {
        if (target == null || BossStats == null) return;

        EnemyVFX vfxController = GetComponent<EnemyVFX>();
        if (vfxController != null)
            vfxController.PlayHeavyAttackVFX(target.position);

        PlayerHealth pHealth = target.GetComponent<PlayerHealth>();
        if (pHealth != null)
        {
            float heavyDamageMultiplier = BossStats.HeavyAttackDamage / 100f;
            pHealth.TakeDamage(currentAttack * heavyDamageMultiplier);
        }
    }

    /// <summary>Called from rage animation event when the cutscene finishes.</summary>
    public void EndEnrage()
    {
        isInvincible = false;
        ResetCombatStates();
    }

    public override void EndAttack() => ResetCombatStates();

    public override void EndHit() => ResetCombatStates();

    protected override void OnCampReset()
    {
        phase2Triggered = false;
        isEnraged = false;
        isInvincible = false;
    }
}
