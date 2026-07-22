using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Applies crowd-control and DoT effects to enemies — slow, freeze, burn, and knockback.
    /// </summary>
    public class EnemyStatusEffects : MonoBehaviour
    {
        #region State
        private NavMeshAgent agent;
        private Animator anim;
        private EnemyBase enemyBase; 

        public float SlowMultiplier { get; private set; } = 1f;

        private int aiPauseCount;
        private int movementLockCount;
        private bool isFrozen;
        private float freezeEndTime;
        private Coroutine knockbackRoutine;
        private Coroutine burnRoutine;
        private Coroutine slowRoutine;
        private float slowEndTime;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            enemyBase = GetComponent<EnemyBase>();
        }

        private void Update()
        {
            if (enemyBase != null && enemyBase.IsDead) return;

            if (isFrozen && Time.time >= freezeEndTime)
            {
                isFrozen = false;
                freezeEndTime = 0f;
                UnlockMovement();
                ResumeAI();
            }

            RefreshState();
        }
        #endregion

        #region Effect API
        /// <summary>Stops NavMesh and disables enemy AI until <see cref="ResumeAI"/>.</summary>
        public void PauseAI()
        {
            aiPauseCount++;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
            RefreshState();
        }

        /// <summary>Re-enables AI when all pause sources are cleared.</summary>
        public void ResumeAI()
        {
            aiPauseCount = Mathf.Max(0, aiPauseCount - 1);
            RefreshState();
        }

        private void LockMovement()
        {
            movementLockCount++;
            RefreshState();
        }

        private void UnlockMovement()
        {
            movementLockCount = Mathf.Max(0, movementLockCount - 1);
            RefreshState();
        }

        /// <summary>Reduces move and animation speed for the given duration.</summary>
        public void ApplySlow(float slowPercent, float duration)
        {
            if (enemyBase != null && enemyBase.IsDead) return;
            if (slowPercent <= 0f) return;

            float newMultiplier = Mathf.Clamp01(1f - slowPercent);
            SlowMultiplier = Mathf.Min(SlowMultiplier, newMultiplier);

            if (duration > 0f)
            {
                slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
                if (slowRoutine == null)
                    slowRoutine = StartCoroutine(SlowRoutine());
            }

            RefreshState();
        }

        /// <summary>Clears slow and restores full movement speed.</summary>
        public void RemoveSlow()
        {
            if (slowRoutine != null)
            {
                StopCoroutine(slowRoutine);
                slowRoutine = null;
            }

            slowEndTime = 0f;
            SlowMultiplier = 1f;
            RefreshState();
        }

        /// <summary>Freezes NavMesh, movement, and animator until the duration expires.</summary>
        public void ApplyFreeze(float duration)
        {
            if (enemyBase != null && enemyBase.IsDead) return;

            freezeEndTime = Mathf.Max(freezeEndTime, Time.time + duration);

            if (!isFrozen)
            {
                isFrozen = true;
                PauseAI();
                LockMovement();

                if (agent != null && agent.isActiveAndEnabled)
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                        agent.velocity = Vector3.zero;
                    }
                }

                if (anim != null) anim.speed = 0f;
            }
        }

        /// <summary>Applies fire DoT. Refreshes duration if the enemy is already burning.</summary>
        public void ApplyBurn(float duration, float damagePerTick, float tickInterval = 0.5f)
        {
            if (enemyBase != null && enemyBase.IsDead) return;
            if (damagePerTick <= 0f || duration <= 0f) return;

            if (burnRoutine != null) StopCoroutine(burnRoutine);
            burnRoutine = StartCoroutine(BurnRoutine(duration, damagePerTick, tickInterval));
        }

        /// <summary>Stops burn DoT ticks.</summary>
        public void RemoveBurn()
        {
            if (burnRoutine != null)
            {
                StopCoroutine(burnRoutine);
                burnRoutine = null;
            }
        }

        /// <summary>Pushes the enemy along a direction while pausing AI.</summary>
        public void ApplyKnockback(Vector3 direction, float distance, float duration)
        {
            if (enemyBase != null && enemyBase.IsDead) return;

            if (knockbackRoutine != null) 
            {
                StopCoroutine(knockbackRoutine);

                ResumeAI(); 
            }

            knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, distance, duration));
        }

        private IEnumerator SlowRoutine()
        {
            while (Time.time < slowEndTime)
            {
                if (enemyBase != null && enemyBase.IsDead) yield break;
                yield return null;
            }

            slowRoutine = null;
            SlowMultiplier = 1f;
            RefreshState();
        }

        private IEnumerator BurnRoutine(float duration, float damagePerTick, float tickInterval)
        {
            float endTime = Time.time + duration;

            while (Time.time < endTime)
            {
                if (enemyBase != null && enemyBase.IsDead) yield break;

                enemyBase.TakeDamage(damagePerTick, playHitReaction: false, WeaponItemData.WeaponElement.Fire);
                yield return new WaitForSeconds(tickInterval);
            }

            burnRoutine = null;
        }

        private IEnumerator KnockbackRoutine(Vector3 direction, float distance, float duration)
        {
            PauseAI();

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            Vector3 dir = direction;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

            float moved = 0f;
            float speed = distance / Mathf.Max(0.01f, duration);

            while (moved < distance)
            {
                if (enemyBase != null && enemyBase.IsDead) yield break;

                float step = speed * Time.deltaTime;
                moved += step;

                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !isFrozen)
                    agent.Move(dir * step);

                yield return null;
            }

            knockbackRoutine = null;
            ResumeAI();
        }

        private void RefreshState()
        {
            if (enemyBase != null && enemyBase.IsDead) return;

            if (enemyBase != null)
                enemyBase.enabled = (aiPauseCount == 0 && !isFrozen);

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                bool shouldStop = isFrozen || movementLockCount > 0 || aiPauseCount > 0;
                agent.isStopped = shouldStop;

                if (shouldStop)
                    agent.velocity = Vector3.zero;
            }

            if (anim != null)
            {
                if (isFrozen)
                    anim.speed = 0f;
                else if (enemyBase != null && (enemyBase.isAttackingBase || enemyBase.isHitBase))
                    anim.speed = 1f;
                else
                    anim.speed = SlowMultiplier;
            }
        }

        /// <summary>Clears all active effects — used on boss camp reset.</summary>
        public void ResetAllEffects()
        {
            if (knockbackRoutine != null)
            {
                StopCoroutine(knockbackRoutine);
                knockbackRoutine = null;
            }

            RemoveBurn();
            RemoveSlow();

            aiPauseCount = 0;
            movementLockCount = 0;
            isFrozen = false;
            freezeEndTime = 0f;

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.velocity = Vector3.zero;
            }

            if (anim != null) anim.speed = 1f;
            if (enemyBase != null && !enemyBase.IsDead) enemyBase.enabled = true;
        }
        #endregion
    }
}
