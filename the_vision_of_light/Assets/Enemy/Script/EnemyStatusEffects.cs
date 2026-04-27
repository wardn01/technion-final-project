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

    public void ApplySlow(float slowPercent)
    {
        if (enemyBase != null && enemyBase.IsDead) return;
        SlowMultiplier = Mathf.Clamp01(1f - slowPercent);
        RefreshState();
    }

    public void RemoveSlow()
    {
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
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            if (anim != null) anim.speed = 0f;
        }
    }

    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        if (enemyBase != null && enemyBase.IsDead) return;

        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, distance, duration));
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

        if (agent != null)
        {
            if (isFrozen || movementLockCount > 0)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            else
            {
                agent.isStopped = false;
            }
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

        aiPauseCount = 0;
        movementLockCount = 0;
        SlowMultiplier = 1f;
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