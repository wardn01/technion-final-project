using UnityEngine;

/// <summary>
/// Spawns <see cref="attackEffectPrefab"/> for <see cref="Golem"/> hits.
/// The prefab handles its own particles / lifetime — no runtime fade scaling.
/// </summary>
public class GolemAttackVFX : MonoBehaviour
{
    [SerializeField] private GameObject attackEffectPrefab;

    [Header("Spawn Points")]
    [Tooltip("Attack_1 / Attack_2 hit VFX.")]
    [SerializeField] private Transform meleeLightSpawn;
    [Tooltip("JumpAttack landing / impact VFX.")]
    [SerializeField] private Transform meleeHeavySpawn;

    [Header("Scale")]
    [SerializeField] private float lightScale = 1f;
    [SerializeField] private float heavyScale = 1.5f;

    [Header("Cleanup")]
    [Tooltip("Fallback destroy if the prefab leaves an empty root behind.")]
    [SerializeField] private float lightCleanupDelay = 2f;
    [SerializeField] private float heavyCleanupDelay = 3f;

    /// <summary>Animation event — spawns light melee VFX at the configured spawn point.</summary>
    public void PlayLightEffect()
    {
        SpawnAtPoint(meleeLightSpawn, lightScale, lightCleanupDelay);
    }

    /// <summary>Animation event — spawns JumpAttack impact VFX at the configured spawn point.</summary>
    public void PlayHeavyEffect()
    {
        SpawnAtPoint(meleeHeavySpawn, heavyScale, heavyCleanupDelay);
    }

    private void SpawnAtPoint(Transform spawnPoint, float scale, float cleanupDelay)
    {
        if (attackEffectPrefab == null)
            return;

        if (spawnPoint == null)
        {
            Debug.LogWarning($"{nameof(GolemAttackVFX)}: missing spawn point on {name}.", this);
            return;
        }

        GameObject fx = Instantiate(attackEffectPrefab, spawnPoint.position, spawnPoint.rotation);
        fx.transform.localScale = Vector3.one * scale;
        StripGameplay(fx);
        Destroy(fx, cleanupDelay);
    }

    private static void StripGameplay(GameObject fx)
    {
        foreach (MonoBehaviour behaviour in fx.GetComponents<MonoBehaviour>())
            behaviour.enabled = false;

        foreach (Collider collider in fx.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Rigidbody body in fx.GetComponentsInChildren<Rigidbody>(true))
            Destroy(body);
    }
}
