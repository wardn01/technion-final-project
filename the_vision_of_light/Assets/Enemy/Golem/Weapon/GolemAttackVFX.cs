using UnityEngine;

/// <summary>
/// Spawns <see cref="attackEffectPrefab"/> for <see cref="Golem"/> hits.
/// Assign a <see cref="Transform"/> spawn point per attack type in the Inspector.
/// </summary>
public class GolemAttackVFX : MonoBehaviour
{
    [SerializeField] private GameObject attackEffectPrefab;

    [Header("Spawn Points")]
    [Tooltip("Attack_1 / Attack_2 hit VFX.")]
    [SerializeField] private Transform meleeLightSpawn;
    [Tooltip("JumpAttack landing / impact VFX.")]
    [SerializeField] private Transform meleeHeavySpawn;
    [Tooltip("Stone release VFX (falls back to Golem throwPoint if empty).")]
    [SerializeField] private Transform stoneThrowSpawn;

    [Header("Light — Attack_1, Attack_2, stone")]
    [SerializeField] private float lightScale = 0.45f;
    [SerializeField] private float lightLifetime = 0.55f;

    [Header("Heavy — JumpAttack")]
    [SerializeField] private float heavyScale = 1.65f;
    [SerializeField] private float heavyLifetime = 1.35f;

    [Header("Fade In / Out")]
    [SerializeField] private float lightFadeIn = 0.12f;
    [SerializeField] private float lightFadeOut = 0.22f;
    [SerializeField] private float heavyFadeIn = 0.18f;
    [SerializeField] private float heavyFadeOut = 0.45f;

    public void PlayLightEffect()
    {
        SpawnAtPoint(meleeLightSpawn, lightScale, lightLifetime, lightFadeIn, lightFadeOut);
    }

    public void PlayHeavyEffect()
    {
        SpawnAtPoint(meleeHeavySpawn, heavyScale, heavyLifetime, heavyFadeIn, heavyFadeOut);
    }

    public void PlayStoneThrowEffect()
    {
        SpawnAtPoint(stoneThrowSpawn, lightScale, lightLifetime, lightFadeIn, lightFadeOut);
    }

    private void SpawnAtPoint(Transform spawnPoint, float scale, float lifetime, float fadeIn, float fadeOut)
    {
        if (attackEffectPrefab == null)
            return;

        if (spawnPoint == null)
        {
            Debug.LogWarning($"{nameof(GolemAttackVFX)}: missing spawn point on {name}.", this);
            return;
        }

        GameObject fx = Instantiate(attackEffectPrefab, spawnPoint.position, spawnPoint.rotation);
        TimedVFXFade fade = fx.AddComponent<TimedVFXFade>();
        fade.Configure(scale, lifetime, fadeIn, fadeOut);
    }
}
