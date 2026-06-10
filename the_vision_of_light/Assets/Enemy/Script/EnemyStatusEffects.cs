using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStatusEffects : MonoBehaviour
{
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

    public void RemoveBurn()
    {
        if (burnRoutine != null)
        {
            StopCoroutine(burnRoutine);
            burnRoutine = null;
        }
    }

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

            enemyBase.TakeDamage(damagePerTick);
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
            anim.speed = isFrozen ? 0f : SlowMultiplier;
        }
    }

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
}