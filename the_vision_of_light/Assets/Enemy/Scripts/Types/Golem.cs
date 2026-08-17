using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Mid-boss: opening leap, melee, stone throw, and MiniGolem summon at 30% HP.
    /// Drives the RageGolem HUD meter until minions spawn. Data: Golem/Data/GolemData.asset.
    /// Split across partial files: Attacks, Summon.
    /// </summary>
    [RequireComponent(typeof(EnemyAudioEmitter))]
    [RequireComponent(typeof(GolemAttackVFX))]
    public partial class Golem : BossEnemy
    {
        #region Serialized Fields

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

        #endregion

        #region Runtime State

        private const float MeleeStandOff = 1.75f;

        /// <summary>Health fraction at which MiniGolems are summoned.</summary>
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

        #endregion

        #region AI / Combat

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

        #endregion

        #region Unity Lifecycle

        protected override void Update()
        {
            // A dead boss must never run camp resets (they mutate health post-death).
            if (isDead)
                return;

            if (playerHealth != null && playerHealth.isDead)
            {
                // Reset once — not every frame while the player stays dead.
                if (!isReturningToCamp)
                    TriggerCampReset();
                return;
            }

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

        #endregion

        #region Health / Damage

        /// <summary>Applies damage, enrage threshold, and MiniGolem summon checks.</summary>
        public override void TakeDamage(
            float damage,
            bool playHitReaction = true,
            WeaponItemData.WeaponElement element = WeaponItemData.WeaponElement.None)
        {
            MarkFightStarted();

            if (HasLivingMiniGolems())
                return;

            // Predict the damage that will actually be applied (defense scaling +
            // developer multiplier, mirroring EnemyBase.TakeDamage) so summon/enrage
            // thresholds fire at the configured health, not before.
            float predictedFinalDamage = Mathf.Max(1f,
                damage
                * VisionOfLight.DeveloperTools.DeveloperCheatsManager.PlayerDamageMultiplier
                * (100f / (100f + currentDefense)));

            TrySummonMiniGolems(currentHealth - predictedFinalDamage);

            if (!isEnraged && BossStats != null && currentHealth > 0)
            {
                float threshold = currentMaxHealth * BossStats.EnrageHealthPercentage;
                if ((currentHealth - predictedFinalDamage) <= threshold)
                    isEnraged = true;
            }

            base.TakeDamage(damage, playHitReaction, element);
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

        protected override void Die()
        {
            ClearSummonedMiniGolems();
            base.Die();
        }

        #endregion

        #region Camp Reset

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

        #endregion
    }
}
