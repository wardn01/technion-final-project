using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using VisionOfLight.Player;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Normal enemy AI: patrol, chase, melee attack, and camp reset when the player leaves combat range.
    /// </summary>
    public class NormalEnemy : EnemyBase
    {
        #region State
        [Header("AI Logic")]
        protected float distanceToTarget;
        protected float lastAttackTime;
        protected Vector3 startingPosition;
        private float waitTimer;
        private float pathUpdateTimer; 

        protected PlayerHealth playerHealth; 
        protected EnemyStatusEffects statusEffects;

        [Header("Camp Reset System")]
        public bool isReturningToCamp = false;

        protected NormalEnemyStats MeleeStats => stats as NormalEnemyStats;
        #endregion

        #region Unity Lifecycle
        protected override IEnumerator Start()
        {
            yield return base.Start();

            statusEffects = GetComponent<EnemyStatusEffects>();
            if (target != null)
            {
                playerHealth = target.GetComponent<PlayerHealth>();
            }

            if (playerHealth == null)
            {
                playerHealth = PlayerRegistry.Instance?.Health;

                if (playerHealth != null)
                {
                    target = playerHealth.transform;
                }
            }

            startingPosition = transform.position; 
        }

        public override bool WantsCombatMusic(Transform player)
        {
            if (isReturningToCamp)
                return false;

            return base.WantsCombatMusic(player);
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
                StopAgent();
                isAttackingBase = false;
                if (anim != null) anim.SetFloat("Speed", 0f);
                return; 
            }

            if (isHitBase || isAttackingBase) 
            {
                StopAgent();
                return; 
            }

            float distanceFromCamp = Vector3.Distance(transform.position, startingPosition);
            if (MeleeStats != null && distanceFromCamp > MeleeStats.MaxLeashDistance)
            {
                TriggerCampReset();
                return;
            }

            distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (MeleeStats != null && distanceToTarget <= MeleeStats.NormalAttackRange)
            {
                AttackBehavior(); 
            }
            else if (distanceToTarget <= stats.ChaseRange)
            {
                ChaseBehavior(); 
            }
            else
            {
                PatrolBehavior(); 
            }
        }
        #endregion

        #region Camp Reset
        /// <summary>Heals to full HP and sends the enemy back to its spawn when the leash is broken.</summary>
        protected virtual void TriggerCampReset()
        {
            isReturningToCamp = true;
            isHitBase = false;
            isAttackingBase = false;

            if (anim != null)
            {
                anim.ResetTrigger("Attack");
                anim.ResetTrigger("Hit");
            }

            currentHealth = currentMaxHealth;
            UpdateHealthUI();
        }

        protected void ReturnToCampBehavior()
        {
            if (agent != null)
            {
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                    else
                    {
                        isReturningToCamp = false;
                        agent.ResetPath();
                    }
                }

                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = stats.RunSpeed;
                    agent.SetDestination(startingPosition);

                    if (Vector3.Distance(transform.position, startingPosition) <= agent.stoppingDistance + 0.5f)
                    {
                        isReturningToCamp = false;
                    }
                }
            }
        }

        /// <summary>Ignores damage while walking home; resumes normal AI on arrival.</summary>
        public override void TakeDamage(
            float amount,
            bool playHitReaction = true,
            WeaponItemData.WeaponElement element = WeaponItemData.WeaponElement.None)
        {
            if (isReturningToCamp) return;

            base.TakeDamage(amount, playHitReaction, element);
        }

        protected override void PlayHitEffect()
        {
            base.PlayHitEffect();
            isHitBase = true;     
            isAttackingBase = false; 
            StopAgent();
        }
        #endregion

        #region Combat Behavior
        protected virtual void AttackBehavior()
        {
            StopAgent();
            FaceTarget();

            if (MeleeStats != null && Time.time >= lastAttackTime + MeleeStats.NormalAttackCooldown)
            {
                isAttackingBase = true; 
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        protected virtual void PerformAttack()
        {
            if (anim != null) anim.SetTrigger("Attack");
        }

        protected virtual void UpdateBlendTree()
        {
            if (anim != null && agent != null)
            {
                anim.SetFloat("Speed", agent.velocity.magnitude);
            }
        }

        protected virtual void ChaseBehavior()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;

                float slowMulti = statusEffects != null ? statusEffects.SlowMultiplier : 1f;
                agent.speed = stats.RunSpeed * slowMulti; 

                if (Time.time >= pathUpdateTimer)
                {
                    agent.SetDestination(GetChaseDestination());
                    pathUpdateTimer = Time.time + 0.2f; 
                }
            }
        }

        protected virtual Vector3 GetChaseDestination()
        {
            return target != null ? target.position : transform.position;
        }

        protected virtual void PatrolBehavior()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;

                float slowMulti = statusEffects != null ? statusEffects.SlowMultiplier : 1f;
                agent.speed = stats.WalkSpeed * slowMulti; 

                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0)
                    {
                        Vector3 randomDir = Random.insideUnitSphere * 5f + startingPosition;
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomDir, out hit, 5f, 1))
                        {
                            agent.SetDestination(hit.position);
                        }
                        waitTimer = 3f; 
                    }
                }
            }
        }
        #endregion
    }
}
