using UnityEngine;
using System.Collections;

/// <summary>
/// Golem minion — melee punch and 35% stone throw. Data: MiniGolem/Data/MiniGolemData.asset.
/// </summary>
[RequireComponent(typeof(EnemyAudioEmitter))]
public class MiniGolem : NormalEnemy
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwAimHeight = 1.05f;

    [Header("Attack")]
    [Tooltip("Resume chasing after the attack clip reaches this normalized time.")]
    [SerializeField] [Range(0.5f, 1f)] private float attackAnimFinishThreshold = 0.88f;

    private bool isSpawning;
    private Vector3 lockedPos;

    private MiniGolemStats RangedStats => stats as MiniGolemStats;

    protected override IEnumerator Start()
    {
        yield return base.Start();

        if (anim != null)
            anim.applyRootMotion = false;
    }

    /// <summary>Called by <see cref="Golem"/> on spawn — plays emerge animation and locks movement.</summary>
    public void InitializeAsSummon()
    {
        isSpawning = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (anim != null)
            anim.Play("spawn", 0, 0f);
    }

    protected override void UpdateBlendTree()
    {
        if (anim == null)
            return;

        if (isAttackingBase || isSpawning || isHitBase)
        {
            anim.SetFloat("Speed", 0f);
            return;
        }

        base.UpdateBlendTree();
    }

    protected override void Update()
    {
        if (isSpawning)
        {
            if (anim == null)
            {
                isSpawning = false;
            }
            else
            {
                AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("spawn") && state.normalizedTime < 1f)
                    return;

                isSpawning = false;
            }
        }

        if (isDead || target == null || playerHealth == null)
            return;

        if (isReturningToCamp)
        {
            EnableAgentIfNeeded();
            ReturnToCampBehavior();
            return;
        }

        if (playerHealth.isDead)
        {
            EnableAgentIfNeeded();
            StopAgent();
            isAttackingBase = false;
            if (anim != null)
                anim.SetFloat("Speed", 0f);
            return;
        }

        if (isAttackingBase)
        {
            transform.position = lockedPos;

            if (anim != null)
                anim.SetFloat("Speed", 0f);

            FaceTarget();
            TryFinishAttackAnimation();
            return;
        }

        if (isHitBase)
        {
            StopAgent();
            if (anim != null)
                anim.SetFloat("Speed", 0f);
            return;
        }

        UpdateBlendTree();

        if (MeleeStats != null)
        {
            float distanceFromCamp = Vector3.Distance(transform.position, startingPosition);
            if (distanceFromCamp > MeleeStats.MaxLeashDistance)
            {
                TriggerCampReset();
                return;
            }
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (MeleeStats != null && distanceToTarget <= MeleeStats.NormalAttackRange)
            HandleAttack(isMelee: true);
        else if (distanceToTarget <= stats.ChaseRange)
            ChaseBehavior();
        else
            PatrolBehavior();
    }

    protected override void ChaseBehavior()
    {
        if (isAttackingBase)
            return;

        if (ShouldThrowStone())
        {
            HandleAttack(isMelee: false);
            return;
        }

        base.ChaseBehavior();
    }

    protected override void PatrolBehavior()
    {
        if (isAttackingBase)
            return;

        base.PatrolBehavior();
    }

    protected override void PlayHitEffect()
    {
        EnableAgentIfNeeded();
        base.PlayHitEffect();
    }

    private bool ShouldThrowStone()
    {
        if (RangedStats == null || isAttackingBase)
            return false;

        if (distanceToTarget < RangedStats.ThrowMinDistance)
            return false;

        if (distanceToTarget > RangedStats.RangedAttackRange)
            return false;

        if (Time.time < lastAttackTime + RangedStats.RangedAttackCooldown)
            return false;

        // One roll per cooldown window — without this, 35% every frame ≈ guaranteed throw.
        if (Random.value > RangedStats.ThrowChance)
        {
            lastAttackTime = Time.time;
            return false;
        }

        return true;
    }

    private void HandleAttack(bool isMelee)
    {
        if (isMelee && MeleeStats == null)
            return;

        if (!isMelee && RangedStats == null)
            return;

        float cooldown = isMelee
            ? MeleeStats.NormalAttackCooldown
            : RangedStats.RangedAttackCooldown;

        if (Time.time < lastAttackTime + cooldown)
            return;

        FaceTarget();
        BeginAttack();
        lastAttackTime = Time.time;

        if (anim == null)
            return;

        anim.ResetTrigger("Attack");
        anim.SetInteger("AttackIndex", isMelee ? 1 : 0);
        anim.SetTrigger("Attack");
    }

    private void BeginAttack()
    {
        isAttackingBase = true;
        lockedPos = transform.position;

        if (agent != null)
            agent.enabled = false;

        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.SetFloat("Speed", 0f);
        }
    }

    private void EnableAgentIfNeeded()
    {
        if (agent == null || agent.enabled)
            return;

        agent.enabled = true;

        if (agent.isOnNavMesh)
            agent.Warp(transform.position);
    }

    /// <summary>Animation event on Attack01 clip — spawns <see cref="MiniGolemStoneProjectile"/>.</summary>
    public void ShootStone()
    {
        if (RangedStats == null || stonePrefab == null || throwPoint == null)
            return;

        float damage = currentAttack * (RangedStats.ThrowDamage / 100f);
        GameObject stoneObj = Instantiate(stonePrefab, throwPoint.position, throwPoint.rotation);

        IgnoreStoneCollisionWithSelf(stoneObj);

        MiniGolemStoneProjectile projectile = null;
        if (stoneObj.TryGetComponent(out projectile))
        {
            projectile.SetDamage(damage);
            projectile.SetTarget(target);
            if (TryGetComponent(out EnemyAudioEmitter emitter))
                projectile.BindAudio(emitter);
        }

        if (!stoneObj.TryGetComponent(out Rigidbody rb))
            return;

        Vector3 direction = transform.forward;
        if (target != null)
        {
            Vector3 aimPoint = target.position + Vector3.up * throwAimHeight;
            direction = aimPoint - throwPoint.position;
            direction.y -= new Vector3(direction.x, 0f, direction.z).magnitude * 0.012f;

            if (direction.sqrMagnitude > 0.01f)
                direction.Normalize();
        }

        rb.linearVelocity = direction * RangedStats.ProjectileSpeed;
        rb.angularVelocity = Vector3.zero;
        projectile?.NotifyLaunched();
    }

    /// <summary>Animation event on Attack02 clip.</summary>
    public void AnimHit()
    {
        if (MeleeStats == null)
            return;

        float damageMultiplier = MeleeStats.NormalDamage / 100f;
        ExecuteMeleeAttack(damageMultiplier, MeleeStats.NormalAttackRange);
    }

    protected override void PerformAttack() { }

    public override void ResetCombatStates()
    {
        if (TryGetAttackAnimationNormalizedTime(out float normalizedTime) && normalizedTime < attackAnimFinishThreshold)
            return;

        if (agent != null)
            agent.enabled = true;

        base.ResetCombatStates();
    }

    protected override void TriggerCampReset()
    {
        if (agent != null)
            agent.enabled = true;

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Hit");
        }

        base.TriggerCampReset();
    }

    private void TryFinishAttackAnimation()
    {
        if (!TryGetAttackAnimationNormalizedTime(out float normalizedTime))
        {
            ResetCombatStates();
            return;
        }

        if (normalizedTime >= attackAnimFinishThreshold)
            ResetCombatStates();
    }

    private bool TryGetAttackAnimationNormalizedTime(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (anim == null) return false;

        AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo nextState = anim.GetNextAnimatorStateInfo(0);

        if (currentState.IsName("Attack01") || currentState.IsName("Attack02"))
        {
            normalizedTime = currentState.normalizedTime;
            return true;
        }
        else if (nextState.IsName("Attack01") || nextState.IsName("Attack02"))
        {
            // We are currently transitioning INTO the attack animation.
            // Don't abort the attack!
            normalizedTime = 0f;
            return true;
        }

        return false;
    }

    private void IgnoreStoneCollisionWithSelf(GameObject stoneObj)
    {
        if (!TryGetComponent(out CapsuleCollider selfCollider))
            return;

        foreach (Collider stoneCollider in stoneObj.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(stoneCollider, selfCollider, true);
    }
}
