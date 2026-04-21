using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WindSkillQDamage : MonoBehaviour
{
    [Header("Tornado Settings")]
    public float pushForce = 2f;
    public float maxPushPerFrame = 0.15f;
    public float stopDistance = 4f;

    public float tickInterval = 0.5f;
    public float lifeTime = 4f;

    private float damagePerTick;
    private float nextDamageTime;

    private readonly List<EnemyHealth> enemiesInTornado = new List<EnemyHealth>();

    public void SetDamage(float damage)
    {
        damagePerTick = damage;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        bool dealDamageThisFrame = Time.time >= nextDamageTime;

        if (dealDamageThisFrame)
            nextDamageTime = Time.time + tickInterval;

        for (int i = enemiesInTornado.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = enemiesInTornado[i];

            if (enemy == null || enemy.isDead)
            {
                enemiesInTornado.RemoveAt(i);
                continue;
            }

            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                Vector3 pushDir = enemy.transform.position - transform.position;
                pushDir.y = 0f;

                float distance = pushDir.magnitude;

                if (distance > 0.1f && distance < stopDistance)
                {
                    pushDir.Normalize();

                    Vector3 move = pushDir * pushForce * Time.deltaTime;
                    move = Vector3.ClampMagnitude(move, maxPushPerFrame);

                    agent.Move(move);
                }
            }

            if (dealDamageThisFrame)
                enemy.TakeDamage(damagePerTick);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy == null || enemy.isDead || enemiesInTornado.Contains(enemy))
            return;

        enemiesInTornado.Add(enemy);

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null)
            status.PauseAI();
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy == null || !enemiesInTornado.Contains(enemy))
            return;

        enemiesInTornado.Remove(enemy);
        RestoreEnemy(enemy);
    }

    private void OnDestroy()
    {
        foreach (var enemy in enemiesInTornado)
        {
            if (enemy != null && !enemy.isDead)
                RestoreEnemy(enemy);
        }

        enemiesInTornado.Clear();
    }

    private void RestoreEnemy(EnemyHealth enemy)
    {
        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null)
            status.ResumeAI();
    }
}