using UnityEngine;
using System.Collections;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Forest animal — sleeps and patrols until provoked, then chases within leash. Data: Bear/Data/BearData.asset.
    /// </summary>
    [RequireComponent(typeof(EnemyAudioEmitter))]
    public class Bear : AnimalEnemy
    {
        private float cycleTimer;
        private bool isAggroed;
        private bool isRoaring;

        private BearStats BearData => stats as BearStats;

        protected override IEnumerator Start()
        {
            yield return base.Start();
            SetSleepState(true);
        }

        protected override void Update()
        {
            if (isDead || target == null || playerHealth == null) return;

            if (playerHealth.isDead)
            {
                ResetAggro();
                StopAgent();
                isAttackingBase = false;
                if (anim != null)
                {
                    anim.SetFloat("Speed", 0f);
                    SetSleepState(true);
                }
                return;
            }

            distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (!isAggroed && distanceToTarget <= stats.ChaseRange)
                EnterAggro(playRoar: IsSleeping());

            if (isAggroed)
            {
                if (BearData != null && distanceToTarget > BearData.AggroLeashDistance)
                {
                    ResetAggro();
                    cycleTimer = 0f;
                    SetSleepState(true);
                    return;
                }

                if (isRoaring) return;

                AggroCombatUpdate();
                return;
            }

            if (isHitBase || isAttackingBase || isRoaring)
            {
                StopAgent();
                return;
            }

            HandleLifeCycle();
        }

        /// <summary>Waking the bear on damage or proximity.</summary>
        public override void TakeDamage(float amount, bool playHitReaction = true)
        {
            EnterAggro(playRoar: IsSleeping());
            base.TakeDamage(amount, playHitReaction);
        }

        private void EnterAggro(bool playRoar)
        {
            if (isDead) return;

            isAggroed = true;
            SetSleepState(false);

            if (playRoar && anim != null && !isRoaring)
            {
                anim.SetTrigger("Buff");
                isRoaring = true;
                StopAgent();
                cycleTimer = 0f;
            }
        }

        private void ResetAggro()
        {
            isAggroed = false;
            isRoaring = false;
        }

        private bool IsSleeping() => anim != null && anim.GetBool("IsSleeping");

        /// <summary>Chase/attack while aggroed — keeps pursuing within leash (unlike peaceful patrol).</summary>
        private void AggroCombatUpdate()
        {
            UpdateBlendTree();

            if (isHitBase || isAttackingBase)
            {
                StopAgent();
                return;
            }

            if (AnimalStats != null && distanceToTarget <= AnimalStats.AttackRange)
                AttackBehavior();
            else
                ChaseBehavior();
        }

        private void HandleLifeCycle()
        {
            if (anim == null || BearData == null) return;

            cycleTimer += Time.deltaTime;

            if (IsSleeping())
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                if (cycleTimer >= BearData.SleepDuration)
                {
                    SetSleepState(false);
                    cycleTimer = 0f;
                }
            }
            else
            {
                UpdateBlendTree();
                PatrolBehavior();

                if (cycleTimer >= BearData.WalkDuration)
                {
                    SetSleepState(true);
                    cycleTimer = 0f;
                }
            }
        }

        private void SetSleepState(bool sleep)
        {
            if (anim != null)
                anim.SetBool("IsSleeping", sleep);

            if (sleep && agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        protected override void PerformAttack()
        {
            if (anim == null) return;

            int randomAttack = Random.Range(1, 4);
            anim.SetInteger("AttackIndex", randomAttack);
            anim.SetTrigger("Attack");
        }

        /// <summary>Animation event — applies damage based on <c>AttackIndex</c>.</summary>
        public void AnimHit()
        {
            if (AnimalStats == null || anim == null || BearData == null) return;

            float damagePercent = AnimalStats.AnimalDamage;
            switch (anim.GetInteger("AttackIndex"))
            {
                case 2:
                    damagePercent *= BearData.HeavyAttackMultiplier;
                    break;
                case 3:
                    damagePercent *= BearData.SlamAttackMultiplier;
                    break;
            }

            ExecuteMeleeAttack(damagePercent / 100f, AnimalStats.AttackRange);
        }

        /// <summary>Animation event — ends the attack clip and resumes movement.</summary>
        public void EndAttack() => ResetCombatStates();

        /// <summary>Animation event — ends the hit reaction clip.</summary>
        public void EndHit() => ResetCombatStates();

        /// <summary>Animation event — ends the wake-up roar and resumes combat.</summary>
        public void EndBuff() => isRoaring = false;
    }
}
