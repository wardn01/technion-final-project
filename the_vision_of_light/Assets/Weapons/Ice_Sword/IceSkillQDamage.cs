using System.Collections.Generic;
using UnityEngine;

public class IceSkillQDamage : MonoBehaviour
{
    public float tickInterval = 1f;
    public float lifeTime = 8f;
    public float slowPercentage = 0.5f;

    private float damagePerTick;
    private float nextDamageTime;
    private readonly List<EnemyHealth> enemiesInAura = new List<EnemyHealth>();

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

        for (int i = enemiesInAura.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = enemiesInAura[i];

            if (enemy == null || enemy.isDead)
            {
                enemiesInAura.RemoveAt(i);
                continue;
            }

            if (dealDamageThisFrame)
                enemy.TakeDamage(damagePerTick);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy == null || enemy.isDead || enemiesInAura.Contains(enemy))
            return;

        enemiesInAura.Add(enemy);

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null)
            status.ApplySlow(slowPercentage);
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy == null || !enemiesInAura.Contains(enemy))
            return;

        enemiesInAura.Remove(enemy);

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null)
            status.RemoveSlow();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < enemiesInAura.Count; i++)
        {
            EnemyHealth enemy = enemiesInAura[i];
            if (enemy == null || enemy.isDead)
                continue;

            EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
            if (status != null)
                status.RemoveSlow();
        }

        enemiesInAura.Clear();
    }
}