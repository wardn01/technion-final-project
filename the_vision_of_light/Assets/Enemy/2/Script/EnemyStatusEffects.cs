using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStatusEffects : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private EnemyAI enemyAI;
    private EnemyHealth enemyHealth;

    private float originalSpeed;
    private float slowMultiplier = 1f;

    private int aiPauseCount;
    private int movementLockCount;

    private bool isFrozen;
    private float freezeEndTime;

    private Coroutine knockbackRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (agent != null)
            originalSpeed = agent.speed;
    }

    private void Update()
    {
        if (enemyHealth != null && enemyHealth.isDead)
            return;

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
        if (enemyHealth != null && enemyHealth.isDead)
            return;

        slowMultiplier = Mathf.Clamp01(1f - slowPercent);
        RefreshState();
    }

    public void RemoveSlow()
    {
        slowMultiplier = 1f;
        RefreshState();
    }

    public void ApplyFreeze(float duration)
    {
        if (enemyHealth != null && enemyHealth.isDead)
            return;

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

            if (anim != null)
                anim.speed = 0f;
        }
    }

    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        if (enemyHealth != null && enemyHealth.isDead)
            return;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

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

        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();

        float moved = 0f;
        float speed = distance / Mathf.Max(0.01f, duration);

        while (moved < distance)
        {
            if (enemyHealth != null && enemyHealth.isDead)
                yield break;

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
        if (enemyHealth != null && enemyHealth.isDead)
            return;

        if (enemyAI != null)
            enemyAI.enabled = aiPauseCount == 0 && !isFrozen;

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
                agent.speed = originalSpeed * slowMultiplier;
            }
        }

        if (anim != null)
        {
            if (isFrozen)
                anim.speed = 0f;
            else
                anim.speed = slowMultiplier;
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
        slowMultiplier = 1f;
        isFrozen = false;
        freezeEndTime = 0f;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
            agent.speed = originalSpeed;
        }

        if (anim != null)
            anim.speed = 1f;

        if (enemyAI != null && (enemyHealth == null || !enemyHealth.isDead))
            enemyAI.enabled = true;
    }
}