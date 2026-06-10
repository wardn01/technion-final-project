using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fire Sword [E] skill hitbox. Damages each enemy once and applies a burning DoT
/// through <see cref="EnemyStatusEffects"/>.
/// </summary>
public class FireSkillEDamage : MonoBehaviour
{
    #region Burn Settings
    [Header("Burn Settings")]
    public float burnDuration = 3f;
    public float burnTickInterval = 0.5f;

    [Range(0f, 1f)]
    public float burnDamagePercent = 0.2f;
    #endregion

    #region State
    private float damageAmount;
    private readonly List<EnemyBase> enemiesHit = new List<EnemyBase>();
    #endregion

    #region Public API
    /// <summary>Called by <c>PlayerCombat.OnSkillE</c> after the prefab is spawned.</summary>
    public void SetDamage(float damage) { damageAmount = damage; }
    #endregion

    #region Hit Detection
    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy == null || enemy.IsDead || enemiesHit.Contains(enemy)) return;

        enemy.TakeDamage(damageAmount);
        enemiesHit.Add(enemy);

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null)
        {
            float burnTickDamage = damageAmount * burnDamagePercent;
            status.ApplyBurn(burnDuration, burnTickDamage, burnTickInterval);
        }
    }
    #endregion
}
