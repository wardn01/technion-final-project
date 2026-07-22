using System.Collections.Generic;
using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Wind Sword [E] skill hitbox on <c>e.prefab</c>.
/// Damages each enemy once and applies a short knockback through <see cref="EnemyStatusEffects"/>.
/// </summary>
public class WindSkillEDamage : MonoBehaviour
{
    #region Knockback
    [Header("Knockback")]
    public float knockbackDistance = 3f;
    public float knockbackDuration = 0.2f;
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

        enemy.TakeDamage(damageAmount, true, WeaponItemData.WeaponElement.Wind);
        enemiesHit.Add(enemy);

        Vector3 pushDir = enemy.transform.position - transform.position;
        pushDir.y = 0f;

        EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
        if (status != null)
            status.ApplyKnockback(pushDir, knockbackDistance, knockbackDuration);
    }
    #endregion
}
