using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Boss enemy AI with Genshin-style camp reset: returns to spawn and heals when the player
/// escapes or the boss is dragged too far from its starting position.
/// </summary>
public abstract class BossEnemy : EnemyBase
{
    #region State
    protected float distanceToTarget;
    protected float lastAttackTime;
    protected Vector3 startingPosition;
    private float pathUpdateTimer;
    private bool isAggroed;

    public bool isReturningToCamp { get; private set; }

    protected PlayerHealth playerHealth;
    protected EnemyStatusEffects statusEffects;

    protected BossEnemyStats BossStats => stats as BossEnemyStats;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();

        if (enemyUI != null)
        {
            enemyUI.gameObject.SetActive(false);
            enemyUI = null;
        }
    }

    protected virtual bool IsInPhase2 => false;
    protected virtual bool SuppressHitReaction => false;

    protected virtual float GetAttackCooldown()
    {
        if (BossStats == null) return 2f;
        return IsInPhase2 ? BossStats.Phase2AttackCooldown : BossStats.Phase1AttackCooldown;
    }

    protected virtual float GetDamagePercent()
    {
        if (BossStats == null) return 100f;
        return IsInPhase2 ? BossStats.Phase2NormalDamage : BossStats.Phase1Damage;
    }

    protected float GetPlayerLeashDistance()
    {
        if (BossStats != null)
            return BossStats.AggroLeashDistance;

        return stats != null ? stats.ChaseRange * 1.5f : 30f;
    }

    protected float GetMaxLeashDistance()
    {
        if (BossStats != null)
            return BossStats.MaxLeashDistance;

        return 35f;
    }

    protected override IEnumerator Start()
    {
        yield return base.Start();

        statusEffects = GetComponent<EnemyStatusEffects>();
        if (target != null)
            playerHealth = target.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
                target = playerHealth.transform;
        }

        startingPosition = transform.position;
    }

    protected virtual void Update()
    {
        if (isDead || target == null || playerHealth == null) return;

        UpdateBlendTree();

        if (isReturningToCamp)
        {
            ReturnToCampBehavior();
            return;
        }

        if (playerHealth.isDead)
        {
            TriggerCampReset();
            return;
        }

        if (isHitBase || isAttackingBase)
        {
            StopAgent();
            return;
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);
        UpdateAggroState();

        if (!isAggroed)
        {
            IdleBehavior();
            return;
        }

        float distanceFromCamp = Vector3.Distance(transform.position, startingPosition);
        if (distanceFromCamp > GetMaxLeashDistance() || distanceToTarget > GetPlayerLeashDistance())
        {
            TriggerCampReset();
            return;
        }

        if (BossStats != null && distanceToTarget <= BossStats.AttackRange)
            AttackBehavior();
        else
            ChaseBehavior();
    }
    #endregion

    #region Aggro & Camp Reset
    /// <summary>First hit or chase-range entry shows the HUD boss bar.</summary>
    public override void TakeDamage(float damage, bool playHitReaction = true)
    {
        if (isReturningToCamp) return;

        EnterAggro();
        base.TakeDamage(damage, playHitReaction);
    }

    protected void EnterAggro()
    {
        if (isAggroed) return;

        isAggroed = true;
        ShowBossBar();
    }

    /// <summary>Heals, hides the boss bar, and resets subclass phase state when the player escapes.</summary>
    protected virtual void TriggerCampReset()
    {
        isReturningToCamp = true;
        isAggroed = false;
        isHitBase = false;
        isAttackingBase = false;

        HideBossBar();

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Hit");
            anim.ResetTrigger("Enrage");
        }

        if (statusEffects != null)
            statusEffects.ResetAllEffects();

        OnCampReset();

        currentHealth = currentMaxHealth;
        UpdateHealthUI();
    }

    /// <summary>Subclass hook — e.g. Orc resets enrage phase.</summary>
    protected virtual void OnCampReset() { }
    #endregion

    #region Combat Behavior
    protected void ReturnToCampBehavior()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.speed = stats.RunSpeed;
        agent.SetDestination(startingPosition);

        if (Vector3.Distance(transform.position, startingPosition) <= agent.stoppingDistance + 0.5f)
        {
            isReturningToCamp = false;
            agent.ResetPath();
            if (anim != null) anim.SetFloat("Speed", 0f);
        }
    }

    protected override void PlayHitEffect()
    {
        if (SuppressHitReaction) return;

        base.PlayHitEffect();
        isHitBase = true;
        isAttackingBase = false;
        StopAgent();
    }

    protected virtual void AttackBehavior()
    {
        StopAgent();
        FaceTarget();

        if (Time.time >= lastAttackTime + GetAttackCooldown())
        {
            isAttackingBase = true;
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    protected abstract void PerformAttack();

    protected virtual void UpdateBlendTree()
    {
        if (anim != null && agent != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    protected virtual void ChaseBehavior()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = false;

        float slowMulti = statusEffects != null ? statusEffects.SlowMultiplier : 1f;
        agent.speed = stats.RunSpeed * slowMulti;

        if (Time.time >= pathUpdateTimer)
        {
            agent.SetDestination(target.position);
            pathUpdateTimer = Time.time + 0.2f;
        }
    }

    protected virtual void IdleBehavior()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        if (anim != null) anim.SetFloat("Speed", 0f);
    }

    /// <summary>Animation event — clears the attacking flag after a swing.</summary>
    public virtual void EndAttack()
    {
        isAttackingBase = false;
    }

    /// <summary>Animation event — clears the hit-reaction flag.</summary>
    public virtual void EndHit()
    {
        isHitBase = false;
    }

    protected override void UpdateHealthUI()
    {
        if (isAggroed && BossHealthBarUI.Instance != null)
            BossHealthBarUI.Instance.UpdateHealth(currentHealth, currentMaxHealth);
    }

    protected override void Die()
    {
        HideBossBar();
        base.Die();
    }

    private void UpdateAggroState()
    {
        if (isAggroed)
            return;

        if (stats != null && distanceToTarget <= stats.ChaseRange)
            EnterAggro();
    }

    private void ShowBossBar()
    {
        if (stats == null || BossHealthBarUI.Instance == null)
            return;

        BossHealthBarUI.Instance.ShowBoss(this, stats.EnemyName, enemyLevel, currentMaxHealth, currentHealth);
    }

    private void HideBossBar()
    {
        if (BossHealthBarUI.Instance == null)
            return;

        BossHealthBarUI.Instance.HideBoss(this);
    }
    #endregion
}
