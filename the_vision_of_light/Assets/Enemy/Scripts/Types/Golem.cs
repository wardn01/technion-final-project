using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Mid-boss: opening leap, melee, stone throw, and MiniGolem summon at 30% HP.
    /// Drives the RageGolem HUD meter until minions spawn. Data: Golem/Data/GolemData.asset.
    /// </summary>
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

        [Header("Hit Reaction")]
        [Tooltip("Minimum seconds between GetHit animations so rapid player hits do not lock the boss out of attacking.")]
        [SerializeField] private float hitReactionCooldown = 6f;

        [Header("Mini Golem Summon")]
        [SerializeField] private GameObject miniGolemPrefab;
        [SerializeField] private Transform[] miniGolemSpawnPoints;
        [Tooltip("Spawn minions once HP drops to this fraction of max health.")]
        [SerializeField] [Range(0.05f, 1f)] private float miniGolemSummonHealthPercent = 0.3f;
        [SerializeField] private int miniGolemCount = 2;

        private const float MeleeStandOff = 1.75f;

        public float MiniGolemSummonHealthPercent => miniGolemSummonHealthPercent;

        private bool openingSequenceDone;
        private bool isInOpeningSequence;
        private bool isInStartFightSlap;
        private bool isEnraged;
        private float openingSequenceStartTime;
        private bool openingAnimReached;
        private bool isThrowing;
        private float throwAnimStartTime;
        private float lastJumpAttackTime = -999f;
        private float lastHitReactionTime = -999f;
        private bool isJumpAttackActive;
        private GolemAttackVFX attackVfx;
        private Quaternion throwLockedRotation;
        private Vector3 throwLaunchDirection;

        private enum AnimRootMotionMode { None, Full, ThrowPositionOnly }
        private AnimRootMotionMode animRootMotionMode;
        private bool miniGolemsSummoned;
        private float fightStartTime = -1f;
        private readonly List<GameObject> activeMiniGolems = new List<GameObject>();

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

        protected override Vector3 GetChaseDestination()
        {
            if (target == null)
                return transform.position;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= MeleeStandOff)
                return transform.position;

            return target.position - toTarget.normalized * MeleeStandOff;
        }

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

            MarkFightStartedIfNeeded();

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

            if (Random.value > throwChance)
            {
                lastAttackTime = Time.time;
                return false;
            }

            return true;
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
            MarkFightStarted();
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

            IgnoreStoneCollisionWithGolem(stoneObj);

            StoneProjectile projectile = null;
            if (stoneObj.TryGetComponent(out projectile))
            {
                projectile.SetDamage(damage);
                projectile.SetTarget(target);
                if (TryGetComponent(out EnemyAudioEmitter emitter))
                    projectile.BindAudio(emitter);
            }

            if (stoneObj.TryGetComponent(out Rigidbody rb))
            {
                Vector3 direction = throwLaunchDirection.sqrMagnitude > 0.01f
                    ? throwLaunchDirection
                    : throwLockedRotation * Vector3.forward;

                rb.linearVelocity = direction * projectileSpeed;
                rb.angularVelocity = Vector3.zero;
                projectile?.NotifyLaunched();
            }
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
            MarkFightStarted();

            if (HasLivingMiniGolems())
                return;

            TrySummonMiniGolems(currentHealth - damage);

            if (!isEnraged && BossStats != null && currentHealth > 0)
            {
                float threshold = currentMaxHealth * BossStats.EnrageHealthPercentage;
                if ((currentHealth - damage) <= threshold)
                    isEnraged = true;
            }

            base.TakeDamage(damage, playHitReaction);
        }

        protected override void PlayHitEffect()
        {
            if (Time.time < lastHitReactionTime + hitReactionCooldown)
                return;

            lastHitReactionTime = Time.time;
            base.PlayHitEffect();
        }

        /// <summary>Syncs HP bar and RageGolem summon meter on the HUD.</summary>
        protected override void UpdateHealthUI()
        {
            base.UpdateHealthUI();

            if (BossHealthBarUI.Instance == null)
                return;

            BossHealthBarUI.Instance.UpdateGolemSummonMeter(
                currentHealth,
                currentMaxHealth,
                miniGolemSummonHealthPercent,
                miniGolemsSummoned);
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
            lastHitReactionTime = -999f;
            miniGolemsSummoned = false;
            fightStartTime = -1f;
            ClearSummonedMiniGolems();
        }

        protected override void Die()
        {
            ClearSummonedMiniGolems();
            base.Die();
        }

        private void MarkFightStartedIfNeeded()
        {
            if (fightStartTime >= 0f || isDead || isReturningToCamp || target == null || stats == null)
                return;

            if (Vector3.Distance(transform.position, target.position) <= stats.ChaseRange)
                MarkFightStarted();
        }

        private void MarkFightStarted()
        {
            if (fightStartTime >= 0f)
                return;

            fightStartTime = Time.time;
        }

        private void TrySummonMiniGolems(float projectedHealth = -1f)
        {
            if (miniGolemsSummoned || isDead || isReturningToCamp || miniGolemPrefab == null || fightStartTime < 0f)
                return;

            float healthToCheck = projectedHealth >= 0f ? projectedHealth : currentHealth;
            if (currentMaxHealth <= 0f || healthToCheck > currentMaxHealth * miniGolemSummonHealthPercent)
                return;

            SpawnMiniGolems();
        }

        private void SpawnMiniGolems()
        {
            miniGolemsSummoned = true;
            BossHealthBarUI.Instance?.HideGolemSummonMeter();

            if (miniGolemSpawnPoints != null && miniGolemSpawnPoints.Length > 0)
            {
                int spawnCount = Mathf.Min(miniGolemCount, miniGolemSpawnPoints.Length);
                for (int i = 0; i < spawnCount; i++)
                {
                    Transform spawnPoint = miniGolemSpawnPoints[i];
                    if (spawnPoint == null)
                        continue;

                    SpawnOneMiniGolem(spawnPoint.position, spawnPoint.rotation);
                }

                return;
            }

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 basePosition = transform.position;

            SpawnOneMiniGolem(basePosition + right * 3.5f - forward * 1.5f, transform.rotation);
            if (miniGolemCount > 1)
                SpawnOneMiniGolem(basePosition - right * 3.5f - forward * 1.5f, transform.rotation);
        }

        private void SpawnOneMiniGolem(Vector3 position, Quaternion rotation)
        {
            GameObject miniGolemObject = Instantiate(miniGolemPrefab, position, rotation);
            activeMiniGolems.Add(miniGolemObject);

            if (miniGolemObject.TryGetComponent(out MiniGolem miniGolem))
                miniGolem.InitializeAsSummon(this);
        }

        private void ClearSummonedMiniGolems()
        {
            for (int i = activeMiniGolems.Count - 1; i >= 0; i--)
            {
                if (activeMiniGolems[i] != null)
                    Destroy(activeMiniGolems[i]);
            }

            activeMiniGolems.Clear();
        }

        private bool HasLivingMiniGolems()
        {
            if (!miniGolemsSummoned)
                return false;

            for (int i = activeMiniGolems.Count - 1; i >= 0; i--)
            {
                GameObject miniGolemObject = activeMiniGolems[i];
                if (miniGolemObject == null)
                {
                    activeMiniGolems.RemoveAt(i);
                    continue;
                }

                if (miniGolemObject.TryGetComponent(out MiniGolem miniGolem) && miniGolem.IsDead)
                {
                    activeMiniGolems.RemoveAt(i);
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
