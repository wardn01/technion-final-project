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
        
        Debug.Log("Orc is ENRAGED! Invincibility ON, and all CC cleared!");
    }

    public override void TakeDamage(float damage)
    {
        if (isInvincible) return; 
        if (SpecificOrcStats == null) return;

        float enrageThreshold = stats.MaxHealth * SpecificOrcStats.EnrageHealthPercentage;

        if (!phase2Triggered && (currentHealth - damage) <= enrageThreshold)
        {
            float allowedDamage = currentHealth - enrageThreshold;
            
            if (allowedDamage > 0)
            {
                base.TakeDamage(allowedDamage);
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
        
        Debug.Log($"[Orc] Attacking with move number: {attackIndex}");
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
            float damageToApply = isEnraged ? SpecificOrcStats.Phase2NormalDamage : SpecificOrcStats.NormalDamage;
            ExecuteMeleeAttack(damageToApply, SpecificOrcStats.NormalAttackRange);
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
                float heavyDamage = SpecificOrcStats.HeavyAttackDamage; 
                pHealth.TakeDamage(heavyDamage);
                
                Debug.Log($"[Orc] HEAVY MAGIC ATTACK hit for {heavyDamage} damage!");
            }
        }
    }

    public void EndAttack()
    {
        isAttackingBase = false;
        
        if (isInvincible)
        {
            isInvincible = false;
            Debug.Log("🎯 Rage Period Over: Invincibility OFF.");
        }
        
        Debug.Log("🎯 EndAttack Fired! The Orc can move now.");
    }

    public void EndHit()
    {
        isHitBase = false;
        Debug.Log("🎯 EndHit Fired! The Orc should be chasing you now.");
    }
}