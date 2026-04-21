using System.Collections.Generic;
using UnityEngine;

public class IceSkillEDamage : MonoBehaviour
{
    private float damageAmount;
    private readonly List<EnemyHealth> enemiesHit = new List<EnemyHealth>();

    public void SetDamage(float damage)
    {
        damageAmount = damage;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy == null || enemy.isDead || enemiesHit.Contains(enemy))
            return;

        enemy.TakeDamage(damageAmount);
        enemiesHit.Add(enemy);

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null)
            status.ApplyFreeze(1f);
    }
}