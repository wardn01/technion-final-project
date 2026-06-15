using UnityEngine;

/// <summary>
/// Boss: phase-1 melee, then enrage cutscene at 50% HP with faster phase-2 attacks.
/// Drives the RageOrc HUD meter until enrage triggers. Data: Orc/Data/OrcData.asset.
/// </summary>
[RequireComponent(typeof(EnemyAudioEmitter))]
public class Orc : BossEnemy
{
    private const float EnrageSequenceTimeout = 8f;

    private bool isEnraged;
    private bool phase2Triggered;
    private bool isInvincible;
    private bool isInEnrageSequence;
    private float enrageSequenceStartTime;

    public float EnrageHealthPercent => BossStats != null ? BossStats.EnrageHealthPercentage : 0.5f;
    public bool IsEnrageTriggered => phase2Triggered;

    protected override bool IsInPhase2 => isEnraged;
    protected override bool SuppressHitReaction => isInvincible || isEnraged;

    protected override void Update()
    {
        if (isDead || target == null)
            return;

        if (isInEnrageSequence)
        {
            FaceTarget();
            if (Time.time >= enrageSequenceStartTime + EnrageSequenceTimeout)
                ForceCompleteEnrageSequence();
            return;
        }

        if (isInvincible)
            ForceCompleteEnrageSequence();

        base.Update();
    }

    public override void TakeDamage(float damage, bool playHitReaction = true)
    {
        if (isInvincible || BossStats == null)
            return;

        float enrageThresholdHealth = GetEnrageThresholdHealth();

        if (!phase2Triggered)
        {
            if (currentHealth <= enrageThresholdHealth)
            {
                SnapHealthToEnrageThreshold(enrageThresholdHealth);
                TriggerEnragePhase();
                return;
            }

            float damageMultiplier = 100f / (100f + currentDefense);
            float predictedFinalDamage = Mathf.Max(1f, damage * damageMultiplier);

            if (currentHealth - predictedFinalDamage <= enrageThresholdHealth)
            {
                float allowedFinalDamage = currentHealth - enrageThresholdHealth;
                if (allowedFinalDamage > 0f)
                {
                    float rawAllowed = allowedFinalDamage / damageMultiplier;
                    base.TakeDamage(rawAllowed, playHitReaction);
                }

                SnapHealthToEnrageThreshold(enrageThresholdHealth);
                TriggerEnragePhase();
                return;
            }
        }

        base.TakeDamage(damage, playHitReaction);
    }

    /// <summary>Syncs HP bar and RageOrc enrage meter on the HUD.</summary>
    protected override void UpdateHealthUI()
    {
        base.UpdateHealthUI();

        if (BossHealthBarUI.Instance == null)
            return;

        BossHealthBarUI.Instance.UpdateOrcRageMeter(
            currentHealth,
            currentMaxHealth,
            EnrageHealthPercent,
            phase2Triggered);
    }

    private float GetEnrageThresholdHealth()
    {
        return Mathf.Max(1f, currentMaxHealth * BossStats.EnrageHealthPercentage);
    }

    private void SnapHealthToEnrageThreshold(float enrageThresholdHealth)
    {
        if (Mathf.Approximately(currentHealth, enrageThresholdHealth))
            return;

        currentHealth = enrageThresholdHealth;
        UpdateHealthUI();
    }

    private void TriggerEnragePhase()
    {
        if (phase2Triggered)
            return;

        phase2Triggered = true;
        isEnraged = true;
        isInEnrageSequence = true;
        isInvincible = true;
        isHitBase = false;
        isAttackingBase = false;
        enrageSequenceStartTime = Time.time;

        StopAgent();

        if (anim != null)
        {
            anim.ResetTrigger("Hit");
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Enrage");
        }

        EnemyStatusEffects statusEffects = GetComponent<EnemyStatusEffects>();
        if (statusEffects != null)
            statusEffects.ResetAllEffects();

        EnemyVFX vfxController = GetComponent<EnemyVFX>();
        if (vfxController != null)
            vfxController.PlayRageVFX();

        BossHealthBarUI.Instance?.HideOrcRageMeter();
    }

    protected override void PerformAttack()
    {
        int attackIndex = isEnraged ? Random.Range(3, 5) : Random.Range(1, 3);
        anim.SetInteger("AttackIndex", attackIndex);
        anim.SetTrigger("Attack");
    }

    /// <summary>Animation event — phase-1/2 melee damage frame.</summary>
    public void AnimHit()
    {
        if (BossStats == null || isInEnrageSequence)
            return;

        float damageMultiplier = GetDamagePercent() / 100f;
        ExecuteMeleeAttack(damageMultiplier, BossStats.AttackRange);
    }

    /// <summary>Animation event — phase-2 heavy fire strike with ground VFX.</summary>
    public void AnimHeavyHit()
    {
        if (target == null || BossStats == null || isInEnrageSequence)
            return;

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
        ForceCompleteEnrageSequence();
    }

    private void ForceCompleteEnrageSequence()
    {
        if (!isInEnrageSequence && !isInvincible)
            return;

        isInEnrageSequence = false;
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
        isInEnrageSequence = false;
        enrageSequenceStartTime = 0f;
    }
}
