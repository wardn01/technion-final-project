using System.Collections.Generic;
using UnityEngine;

public class WindSkillEDamage : MonoBehaviour
{
    private float damageAmount;
    private readonly List<EnemyBase> enemiesHit = new List<EnemyBase>();

    public void SetDamage(float damage) { damageAmount = damage; }

    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy == null || enemy.IsDead || enemiesHit.Contains(enemy)) return;

        enemy.TakeDamage(damageAmount);
        enemiesHit.Add(enemy);

        Vector3 pushDir = enemy.transform.position - transform.position;
        pushDir.y = 0f;

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null) status.ApplyKnockback(pushDir, 3f, 0.2f);
    }
}