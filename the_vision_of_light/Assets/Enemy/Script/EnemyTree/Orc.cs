using UnityEngine;

public class Orc : NormalEnemy
{
    [Header("Orc Boss Settings")]
    private bool isEnraged = false;
    private bool phase2Triggered = false;
    private bool isInvincible = false;

    protected OrcStats SpecificOrcStats => stats as OrcStats;

    protected override void Update()
    {
        if (isDead || target == null) return;

        if (isInvincible)
        {
            FaceTarget();
        }

        base.Update();
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
        {
            statusEffects.ResetAllEffects();
        }

        anim.SetTrigger("Enrage");
        
        EnemyVFX vfxController = GetComponent<EnemyVFX>();
        if (vfxController != null)
        {
            vfxController.PlayRageVFX();
        }
    }

    public override void TakeDamage(float damage)
    {
        if (isInvincible) return; 
        if (SpecificOrcStats == null) return;

        float enrageThreshold = currentMaxHealth * SpecificOrcStats.EnrageHealthPercentage;

        float damageMultiplier = 100f / (100f + currentDefense);
        float predictedFinalDamage = damage * damageMultiplier;

        if (!phase2Triggered && (currentHealth - predictedFinalDamage) <= enrageThreshold)
        {
            float actualDamageAllowed = currentHealth - enrageThreshold;
            float rawDamageAllowed = actualDamageAllowed / damageMultiplier;
            
            if (rawDamageAllowed > 0)
            {
                base.TakeDamage(rawDamageAllowed);
            }

            TriggerEnragePhase();
            return; 
        }

        base.TakeDamage(damage); 
    }

    protected override void PerformAttack()
    {
        int attackIndex;

        if (!isEnraged)
        {
            attackIndex = Random.Range(1, 3); 
        }
        else
        {
            attackIndex = Random.Range(3, 5); 
        }

        anim.SetInteger("AttackIndex", attackIndex);
        anim.SetTrigger("Attack");
    }

    protected override void PlayHitEffect()
    {
        if (isEnraged) return; 

        isAttackingBase = false;
        base.PlayHitEffect();
    }

    public void AnimHit()
    {
        if (SpecificOrcStats != null) 
        {
            float damagePercentage = isEnraged ? SpecificOrcStats.Phase2NormalDamage : SpecificOrcStats.NormalDamage;
            float damageMultiplier = damagePercentage / 100f; 
            
            ExecuteMeleeAttack(damageMultiplier, SpecificOrcStats.NormalAttackRange);
        }
    }

    public void AnimHeavyHit()
    {
        if (target != null && SpecificOrcStats != null) 
        {
            EnemyVFX vfxController = GetComponent<EnemyVFX>();
            if (vfxController != null)
            {
                vfxController.PlayHeavyAttackVFX(target.position);
            }

            PlayerHealth pHealth = target.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                float heavyDamageMultiplier = SpecificOrcStats.HeavyAttackDamage / 100f;
                float finalHeavyDamage = currentAttack * heavyDamageMultiplier; 
                
                pHealth.TakeDamage(finalHeavyDamage);
            }
        }
    }

    public void EndAttack()
    {
        isAttackingBase = false;
        
        if (isInvincible)
        {
            isInvincible = false;
        }
    }

    public void EndHit()
    {
        isHitBase = false;
    }
}