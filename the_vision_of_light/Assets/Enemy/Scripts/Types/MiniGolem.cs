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
    private bool agentControlLocked;
    private Vector3 lockedPos;
    private Golem summoningGolem;
    private float lastThrowTime = -999f;

    private MiniGolemStats RangedStats => stats as MiniGolemStats;

    private const float PeerSeparationRadius = 1.15f;

    protected override IEnumerator Start()
    {
        yield return base.Start();

        if (anim != null)
            anim.applyRootMotion = false;

        RegisterAgentSettings();
    }

    /// <summary>Called by <see cref="Golem"/> on spawn — plays emerge animation and locks movement.</summary>
    public void InitializeAsSummon(Golem owner = null)
    {
        summoningGolem = owner;
        isSpawning = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (anim != null)
            anim.Play("spawn", 0, 0f);
    }

    private void RegisterAgentSettings()
    {
        if (agent != null)
            agent.avoidancePriority = 45 + (Mathf.Abs(GetInstanceID()) % 15);

        if (summoningGolem == null || !TryGetComponent(out Collider selfCollider))
            return;

        if (summoningGolem.TryGetComponent(out Collider golemCollider))
            Physics.IgnoreCollision(selfCollider, golemCollider, true);
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

        if (agent == null || stats == null)
            return;

        // Blend tree: 0 = Idle, 0.5 = Walk, 1 = Walk @ 1.8x (run substitute)
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
            ReturnToCampBehavior();
            return;
        }

        if (playerHealth.isDead)
        {
            UnlockAgentControl();
            StopAgent();
            isAttackingBase = false;
            if (anim != null)
                anim.SetFloat("Speed", 0f);
            return;
        }

        if (isAttackingBase)
        {
            FaceTarget();
            HoldAttackPosition();
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
        ApplyPeerSeparation();

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

        if (IsInThrowRange() && ShouldThrowStone())
        {
            HandleAttack(isMelee: false);
            return;
        }

        if (MeleeStats != null && distanceToTarget <= MeleeStats.NormalAttackRange)
            HandleAttack(isMelee: true);
        else if (distanceToTarget <= stats.ChaseRange)
            ChaseBehavior();
        else
            PatrolBehavior();
    }

    private void ApplyPeerSeparation()
    {
        if (isAttackingBase || isSpawning || isHitBase || agent == null || !agent.isOnNavMesh)
            return;

        Vector3 push = Vector3.zero;
        int neighbors = 0;

        foreach (MiniGolem other in FindObjectsByType<MiniGolem>(FindObjectsSortMode.None))
        {
            if (other == null || other == this)
                continue;

            Vector3 offset = transform.position - other.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance >= PeerSeparationRadius || distance < 0.001f)
                continue;

            push += offset.normalized * (PeerSeparationRadius - distance);
            neighbors++;
        }

        if (neighbors == 0)
            return;

        agent.Move((push / neighbors) * (Time.deltaTime * 8f));
    }

    private bool IsInThrowRange()
    {
        if (RangedStats == null)
            return false;

        return distanceToTarget >= RangedStats.ThrowMinDistance
            && distanceToTarget <= RangedStats.RangedAttackRange;
    }

    protected override void ChaseBehavior()
    {
        if (isAttackingBase)
            return;

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
        UnlockAgentControl();
        base.PlayHitEffect();
    }

    private bool ShouldThrowStone()
    {
        if (RangedStats == null || isAttackingBase || stonePrefab == null || throwPoint == null)
            return false;

        if (!IsInThrowRange())
            return false;

        if (Time.time < lastThrowTime + RangedStats.RangedAttackCooldown)
            return false;

        if (Random.value > RangedStats.ThrowChance)
        {
            lastThrowTime = Time.time;
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

        if (isMelee && Time.time < lastAttackTime + cooldown)
            return;

        FaceTarget();
        BeginAttack();

        if (isMelee)
            lastAttackTime = Time.time;
        else
            lastThrowTime = Time.time;

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
        LockAgentForAttack();

        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.SetFloat("Speed", 0f);
        }
    }

    private void LockAgentForAttack()
    {
        StopAgent();

        if (agent == null)
            return;

        agent.updatePosition = false;
        agent.updateRotation = false;
        agentControlLocked = true;
    }

    private void HoldAttackPosition()
    {
        transform.position = lockedPos;
        StopAgent();

        if (anim != null)
            anim.SetFloat("Speed", 0f);
    }

    private void UnlockAgentControl()
    {
        if (agent == null || !agentControlLocked)
            return;

        agent.updatePosition = true;
        agent.updateRotation = true;
        agentControlLocked = false;

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

        UnlockAgentControl();
        base.ResetCombatStates();
    }

    protected override void TriggerCampReset()
    {
        UnlockAgentControl();

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
        if (!TryGetComponent(out Collider selfCollider))
            return;

        foreach (Collider stoneCollider in stoneObj.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(stoneCollider, selfCollider, true);
    }
}
