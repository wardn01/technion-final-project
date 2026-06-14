using UnityEngine;
using System.Collections;

/// <summary>
/// Normal enemy: close-range punch (Attack02) + stone throw when the player is far.
/// </summary>
/// <remarks>
/// Combat bands:
/// <list type="bullet">
///   <item>Melee range → Attack02 punch</item>
///   <item>Throw range → Attack01 rock throw (<see cref="ShootStone"/>)</item>
///   <item>Chase range → run toward player</item>
/// </list>
/// Animation events:
/// <list type="bullet">
///   <item>Attack01: <c>PlayEnemySound("Throw")</c>, <c>ShootStone</c>, <c>ResetCombatStates</c></item>
///   <item>Attack02: <c>PlayEnemySound("Attack")</c>, <c>AnimHit</c>, <c>ResetCombatStates</c></item>
/// </list>
/// Weapon: scale the thrown stone in <c>MiniGolem/Weapon/Stone.prefab</c>.
/// Stats: <c>MiniGolem/Data/MiniGolemData.asset</c>.
/// Audio: <c>MiniGolem/Data/Audio/MiniGolem_Audio_Library.asset</c>.
/// </remarks>
[RequireComponent(typeof(EnemyAudioEmitter))]
public class MiniGolem : NormalEnemy
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwAimHeight = 1.05f;

    private bool isSpawning;

    private MiniGolemStats RangedStats => stats as MiniGolemStats;

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

        UpdateBlendTree();

        if (isReturningToCamp)
        {
            ReturnToCampBehavior();
            return;
        }

        if (playerHealth.isDead)
        {
            StopAgent();
            isAttackingBase = false;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        if (isHitBase || isAttackingBase)
        {
            StopAgent();
            if (isAttackingBase)
                FaceTarget();
            return;
        }

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
        else if (ShouldThrowStone())
            HandleAttack(isMelee: false);
        else if (distanceToTarget <= stats.ChaseRange)
            ChaseBehavior();
        else
            PatrolBehavior();
    }

    private bool ShouldThrowStone()
    {
        if (RangedStats == null || isAttackingBase)
            return false;

        if (distanceToTarget < RangedStats.ThrowMinDistance)
            return false;

        return distanceToTarget <= RangedStats.RangedAttackRange;
    }

    private void HandleAttack(bool isMelee)
    {
        if (isMelee && MeleeStats == null)
            return;

        if (!isMelee && RangedStats == null)
            return;

        StopAgent();
        FaceTarget();

        float cooldown = isMelee
            ? MeleeStats.NormalAttackCooldown
            : RangedStats.RangedAttackCooldown;

        if (Time.time < lastAttackTime + cooldown)
            return;

        isAttackingBase = true;
        lastAttackTime = Time.time;

        if (anim == null)
            return;

        anim.ResetTrigger("Throw");
        anim.SetInteger("AttackIndex", isMelee ? 1 : 0);
        anim.SetTrigger("Attack");
    }

    /// <summary>Animation event on Attack01 clip — spawns <see cref="MiniGolemStoneProjectile"/>.</summary>
    public void ShootStone()
    {
        if (RangedStats == null || stonePrefab == null || throwPoint == null)
            return;

        float damage = currentAttack * (RangedStats.ThrowDamage / 100f);
        GameObject stoneObj = Instantiate(stonePrefab, throwPoint.position, throwPoint.rotation);

        if (stoneObj.TryGetComponent(out MiniGolemStoneProjectile projectile))
        {
            projectile.SetDamage(damage);
            if (TryGetComponent(out EnemyAudioEmitter emitter))
                projectile.BindAudio(emitter);
        }

        if (target != null && stoneObj.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 aimPoint = target.position + Vector3.up * throwAimHeight;
            Vector3 direction = aimPoint - throwPoint.position;
            direction.y -= new Vector3(direction.x, 0f, direction.z).magnitude * 0.012f;

            if (direction.sqrMagnitude > 0.01f)
                direction.Normalize();
            else
                direction = transform.forward;

            rb.AddForce(direction * RangedStats.ProjectileSpeed, ForceMode.Impulse);
        }

        IgnoreStoneCollisionWithSelf(stoneObj);
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

    protected override void TriggerCampReset()
    {
        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Throw");
        }

        base.TriggerCampReset();
    }

    private void IgnoreStoneCollisionWithSelf(GameObject stoneObj)
    {
        if (!TryGetComponent(out CapsuleCollider selfCollider))
            return;

        foreach (Collider stoneCollider in stoneObj.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(stoneCollider, selfCollider, true);
    }
}
