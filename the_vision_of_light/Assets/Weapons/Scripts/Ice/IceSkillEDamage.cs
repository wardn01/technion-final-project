using System.Collections.Generic;
using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Ice Sword [E] skill hitbox. Damages each enemy once and briefly freezes them
/// through <see cref="EnemyStatusEffects"/>.
/// </summary>
public class IceSkillEDamage : MonoBehaviour
{
    #region Freeze Settings
    [Header("Freeze Settings")]
    public float freezeDuration = 1f;
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
        if (status != null && freezeDuration > 0f)
            status.ApplyFreeze(freezeDuration);
    }
    #endregion
}
