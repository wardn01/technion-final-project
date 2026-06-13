using System.Collections;
using UnityEngine;

/// <summary>
/// Mid-boss enemy: opening StartFight → JumpAttack, then melee + occasional jump leap + stone throw.
/// </summary>
/// <remarks>
/// Animation events (see clips under <c>Golem/Animation/</c>):
/// <list type="bullet">
///   <item>Attack_1 / Attack_2: <c>PlayLightAttackVfx</c> + <c>AnimHit</c> (separate events)</item>
///   <item>ThrowStone: <c>PlayStoneThrowVfx</c> + <c>ShootStone</c></item>
///   <item>JumpAttack: <c>PlayHeavyAttackVfx</c> + <c>AnimHit</c></item>
///   <item>Hit: <c>PlayEnemySound("Hit")</c>, <c>EndHit</c></item>
///   <item>Die: <c>PlayEnemySound("Death")</c></item>
///   <item>Walk / Run: <c>PlayEnemySound("Step")</c></item>
/// </list>
/// Stats: <c>Golem/Data/GolemData.asset</c>.
/// Audio: <c>Golem/Data/Audio/Golem_Audio_Library.asset</c>.
/// Assign <see cref="stonePrefab"/> (<c>Golem/Weapon/Stone.prefab</c> — mesh + physics + <see cref="StoneProjectile"/>).
/// </remarks>
[RequireComponent(typeof(EnemyAudioEmitter))]
[RequireComponent(typeof(GolemAttackVFX))]
public class Golem : BossEnemy
{
    [Header("Ranged Attack")]
    [SerializeField] private float throwMinDistance = 8f;
    [SerializeField] private float throwMaxDistance = 20f;
    [Tooltip("Chance to throw when in range and off cooldown. Golem still chases between throws.")]
    [SerializeField] [Range(0f, 1f)] private float throwChance = 0.35f;
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private Transform throwPoint;
    [Tooltip("Aim height on the player when the throw wind-up locks (chest).")]
    [SerializeField] private float throwAimHeight = 1.05f;

    [Header("Jump Attack (combat)")]
    [Tooltip("Chance to leap with JumpAttack instead of a normal melee swing.")]
    [SerializeField] [Range(0f, 1f)] private float jumpAttackChance = 0.2f;
    [SerializeField] private float jumpAttackMinDistance = 3f;
    [SerializeField] private float jumpAttackMaxDistance = 8.5f;
    [SerializeField] private float jumpAttackCooldown = 12f;

    private bool openingSequenceDone;
    private bool isInOpeningSequence;
    private bool isInStartFightSlap;
    private bool isEnraged;
    private float openingSequenceStartTime;
    private bool openingAnimReached;
    private bool isThrowing;
    private float throwAnimStartTime;
    private float lastJumpAttackTime = -999f;
    private bool isJumpAttackActive;
    private GolemAttackVFX attackVfx;
    private Quaternion throwLockedRotation;
    private Vector3 throwLaunchDirection;

    private enum AnimRootMotionMode { None, Full, ThrowPositionOnly }
    private AnimRootMotionMode animRootMotionMode;

    private bool TryGetThrowStats(out float throwDamagePercent, out float projectileSpeed)
    {
        if (stats is GolemStats golemStats)
        {
            throwDamagePercent = golemStats.ThrowDamage;
            projectileSpeed = golemStats.ProjectileSpeed;
            return true;
        }

        throwDamagePercent = 95f;
        projectileSpeed = 26f;
        return false;
    }

    protected override bool IsInPhase2 => isEnraged;

    protected override void Update()
    {
        if (isInOpeningSequence)
        {
            if (isDead || target == null || playerHealth == null) return;

            if (IsInOpeningAnimState())
                openingAnimReached = true;

            // Only bail out if opening anim never started (broken trigger/avatar)
            if (!openingAnimReached && Time.time > openingSequenceStartTime + 3f)
            {
                ForceEndOpeningSequence();
                base.Update();
                return;
            }

            // Opening clip finished but EndAttack was missed
            if (openingAnimReached && !IsInOpeningAnimState() && anim != null && !anim.IsInTransition(0)
                && Time.time > openingSequenceStartTime + 0.3f)
            {
                ForceEndOpeningSequence();
                base.Update();
                return;
            }

            StopAgent();

            if (IsInStartFightState())
            {
                DisableAnimRootMotion();
                if (anim != null)
                    anim.SetFloat("Speed", 0f);
                FaceTarget();
                return;
            }

            if (IsInJumpAttackState())
            {
                if (anim != null)
                    anim.SetFloat("Speed", 0f);
                EnableFullRootMotion();
                return;
            }

            DisableAnimRootMotion();
            UpdateBlendTree();
            FaceTarget();
            return;
        }

        if (!openingSequenceDone && !isDead && target != null && playerHealth != null && !isReturningToCamp)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            float openingRange = stats != null ? Mathf.Min(stats.ChaseRange * 0.5f, 14f) : 14f;
            if (stats != null && dist <= openingRange)
                BeginOpeningSequence();
        }

        if (isThrowing && anim != null && Time.time > throwAnimStartTime + 4f)
            ForceEndThrow();

        if (isThrowing)
        {
            StopAgent();
            if (IsInThrowStoneState())
            {
                EnableThrowRootMotion();
                if (anim != null)
                    anim.SetFloat("Speed", 0f);
            }
        }
        else if (IsInJumpAttackState())
        {
            StopAgent();
            EnableFullRootMotion();
            if (anim != null)
                anim.SetFloat("Speed", 0f);
        }
        else if (animRootMotionMode == AnimRootMotionMode.Full)
        {
            DisableAnimRootMotion();
        }

        base.Update();
    }

    protected override IEnumerator Start()
    {
        yield return base.Start();

        attackVfx = GetComponent<GolemAttackVFX>();

        if (agent != null && !agent.isOnNavMesh)
            agent.Warp(transform.position);
    }

    protected override void PerformAttack()
    {
        if (ShouldJumpAttack())
        {
            BeginJumpAttack();
            return;
        }

        anim.SetInteger("AttackIndex", Random.Range(0, 2));
        anim.SetTrigger("Attack");
    }

    private void BeginJumpAttack()
    {
        isJumpAttackActive = true;
        lastJumpAttackTime = Time.time;

        if (anim != null)
        {
            anim.ResetTrigger("JumpAttack");
            anim.SetTrigger("JumpAttack");
        }
    }

    private bool ShouldJumpAttack()
    {
        if (isInOpeningSequence || isThrowing)
            return false;

        if (BossStats == null)
            return false;

        if (distanceToTarget < jumpAttackMinDistance || distanceToTarget > jumpAttackMaxDistance)
            return false;

        if (Time.time < lastJumpAttackTime + jumpAttackCooldown)
            return false;

        return Random.value <= jumpAttackChance;
    }

    protected override void ChaseBehavior()
    {
        if (ShouldThrowStone())
        {
            StopAgent();
            LockThrowFacing();
            isAttackingBase = true;
            isThrowing = true;
            throwAnimStartTime = Time.time;
            anim.ResetTrigger("Throw");
            anim.SetTrigger("Throw");
            return;
        }

        if (ShouldJumpAttack())
        {
            StopAgent();
            FaceTarget();
            isAttackingBase = true;
            lastAttackTime = Time.time;
            BeginJumpAttack();
            return;
        }

        base.ChaseBehavior();
    }

    private bool ShouldThrowStone()
    {
        if (isThrowing || isAttackingBase)
            return false;

        if (BossStats == null || distanceToTarget < throwMinDistance)
            return false;

        if (distanceToTarget > throwMaxDistance)
            return false;

        if (Time.time < lastAttackTime + GetAttackCooldown())
            return false;

        return Random.value <= throwChance;
    }

    private void ForceEndThrow()
    {
        DisableAnimRootMotion();
        isThrowing = false;
        throwLaunchDirection = Vector3.zero;
        lastAttackTime = Time.time;

        if (anim != null)
            anim.ResetTrigger("Throw");

        ResetCombatStates();
    }

    private void BeginOpeningSequence()
    {
        if (openingSequenceDone || isInOpeningSequence) return;

        EnterAggro();
        isInOpeningSequence = true;
        isInStartFightSlap = true;
        openingAnimReached = false;
        openingSequenceStartTime = Time.time;
        StopAgent();
        anim.SetTrigger("StartFight");
    }

    private bool IsInStartFightState()
    {
        if (anim == null) return false;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        return state.IsName("StartFight");
    }

    private bool IsInJumpAttackState()
    {
        if (anim == null) return false;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        return state.IsName("JumpAttack");
    }

    private bool IsInThrowStoneState()
    {
        if (anim == null) return false;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        return state.IsName("ThrowStone");
    }

    private bool IsInOpeningAnimState()
    {
        if (anim == null) return false;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        return state.IsName("StartFight") || state.IsName("JumpAttack");
    }

    private void ForceEndOpeningSequence()
    {
        DisableAnimRootMotion();
        isInOpeningSequence = false;
        isInStartFightSlap = false;
        openingSequenceDone = true;
        ResetCombatStates();
    }

    private void EnableFullRootMotion()
    {
        if (animRootMotionMode == AnimRootMotionMode.Full || anim == null || agent == null)
            return;

        animRootMotionMode = AnimRootMotionMode.Full;
        anim.applyRootMotion = true;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private void EnableThrowRootMotion()
    {
        if (animRootMotionMode == AnimRootMotionMode.ThrowPositionOnly || anim == null || agent == null)
            return;

        animRootMotionMode = AnimRootMotionMode.ThrowPositionOnly;
        anim.applyRootMotion = true;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private void DisableAnimRootMotion()
    {
        if (animRootMotionMode == AnimRootMotionMode.None)
            return;

        animRootMotionMode = AnimRootMotionMode.None;

        if (anim != null)
            anim.applyRootMotion = false;

        RestoreAgentAfterRootMotion();
    }

    private void RestoreAgentAfterRootMotion()
    {
        if (agent == null) return;

        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = true;

        if (agent.isOnNavMesh)
            agent.Warp(transform.position);
    }

    private void OnAnimatorMove()
    {
        if (anim == null || animRootMotionMode == AnimRootMotionMode.None)
            return;

        switch (animRootMotionMode)
        {
            case AnimRootMotionMode.Full:
                anim.ApplyBuiltinRootMotion();
                break;
            case AnimRootMotionMode.ThrowPositionOnly:
                transform.position += anim.deltaPosition;
                transform.rotation = throwLockedRotation;
                break;
        }

        if (agent != null && agent.isOnNavMesh)
            agent.Warp(transform.position);
    }

    private void LockThrowFacing()
    {
        Vector3 flatDirection = transform.forward;
        if (target != null)
        {
            flatDirection = target.position - transform.position;
            flatDirection.y = 0f;
        }

        if (flatDirection.sqrMagnitude < 0.01f)
            flatDirection = transform.forward;

        flatDirection.Normalize();
        throwLockedRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
        transform.rotation = throwLockedRotation;

        Vector3 origin = throwPoint != null ? throwPoint.position : transform.position + Vector3.up * 2f;
        if (target != null)
        {
            Vector3 aimPoint = target.position + Vector3.up * throwAimHeight;
            throwLaunchDirection = aimPoint - origin;

            float horizontalDistance = new Vector3(throwLaunchDirection.x, 0f, throwLaunchDirection.z).magnitude;
            throwLaunchDirection.y -= horizontalDistance * 0.01f;
        }
        else
        {
            throwLaunchDirection = flatDirection;
            throwLaunchDirection.y -= 0.04f;
        }

        if (throwLaunchDirection.sqrMagnitude > 0.01f)
            throwLaunchDirection.Normalize();
        else
            throwLaunchDirection = flatDirection;
    }

    /// <summary>Animation event at end of StartFight — chains into JumpAttack.</summary>
    public void EndStartFight()
    {
        isInStartFightSlap = false;
        FaceTarget();
        BeginJumpAttack();
    }

    /// <summary>Animation event — light melee VFX (Attack_1 / Attack_2).</summary>
    public void PlayLightAttackVfx()
    {
        attackVfx?.PlayLightEffect();
    }

    /// <summary>Animation event — JumpAttack impact VFX.</summary>
    public void PlayHeavyAttackVfx()
    {
        attackVfx?.PlayHeavyEffect();
    }

    /// <summary>Animation event — stone release VFX (ThrowStone clip).</summary>
    public void PlayStoneThrowVfx()
    {
        attackVfx?.PlayStoneThrowEffect();
    }

    /// <summary>Animation event on melee / jump attack clips — damage only.</summary>
    public void AnimHit()
    {
        if (BossStats == null || isInStartFightSlap)
            return;

        float damageMultiplier = GetDamagePercent() / 100f;
        float range = BossStats.AttackRange + (isInOpeningSequence ? 1.5f : 0f);
        ExecuteMeleeAttack(damageMultiplier, range);
    }

    /// <summary>Animation event on ThrowStone clip — spawns <see cref="StoneProjectile"/>.</summary>
    public void ShootStone()
    {
        if (throwPoint == null || stonePrefab == null)
            return;

        TryGetThrowStats(out float throwDamagePercent, out float projectileSpeed);
        float damage = currentAttack * (throwDamagePercent / 100f);

        transform.rotation = throwLockedRotation;

        GameObject stoneObj = Instantiate(stonePrefab, throwPoint.position, throwLockedRotation);

        if (stoneObj.TryGetComponent(out StoneProjectile projectile))
        {
            projectile.SetDamage(damage);
            if (TryGetComponent(out EnemyAudioEmitter emitter))
                projectile.BindAudio(emitter);
        }

        if (stoneObj.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 direction = throwLaunchDirection.sqrMagnitude > 0.01f
                ? throwLaunchDirection
                : throwLockedRotation * Vector3.forward;

            rb.AddForce(direction * projectileSpeed, ForceMode.Impulse);
        }

        IgnoreStoneCollisionWithGolem(stoneObj);
    }

    private void IgnoreStoneCollisionWithGolem(GameObject stoneObj)
    {
        if (!TryGetComponent(out CapsuleCollider golemCollider))
            return;

        foreach (Collider stoneCollider in stoneObj.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(stoneCollider, golemCollider, true);
    }

    public override void TakeDamage(float damage, bool playHitReaction = true)
    {
        if (!isEnraged && BossStats != null && currentHealth > 0)
        {
            float threshold = currentMaxHealth * BossStats.EnrageHealthPercentage;
            if ((currentHealth - damage) <= threshold)
                isEnraged = true;
        }

        base.TakeDamage(damage, playHitReaction);
    }

    public override void EndAttack()
    {
        DisableAnimRootMotion();

        if (isThrowing)
        {
            isThrowing = false;
            throwLaunchDirection = Vector3.zero;
            lastAttackTime = Time.time;
        }

        if (anim != null)
        {
            anim.ResetTrigger("Throw");
            anim.ResetTrigger("JumpAttack");
        }

        if (isJumpAttackActive)
        {
            isJumpAttackActive = false;
            lastJumpAttackTime = Time.time;
            lastAttackTime = Time.time;
        }

        if (isInOpeningSequence)
        {
            isInOpeningSequence = false;
            openingSequenceDone = true;
        }

        ResetCombatStates();
    }

    public override void EndHit() => ResetCombatStates();

    protected override void TriggerCampReset()
    {
        if (anim != null)
        {
            anim.ResetTrigger("Throw");
            anim.ResetTrigger("StartFight");
            anim.ResetTrigger("JumpAttack");
        }

        base.TriggerCampReset();
    }

    protected override void OnCampReset()
    {
        DisableAnimRootMotion();
        openingSequenceDone = false;
        isInOpeningSequence = false;
        isInStartFightSlap = false;
        openingAnimReached = false;
        isEnraged = false;
        isThrowing = false;
        throwLaunchDirection = Vector3.zero;
        isJumpAttackActive = false;
        lastJumpAttackTime = -999f;
    }
}
