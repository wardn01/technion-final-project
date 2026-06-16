using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Ice Sword [Q] manager on <c>q.prefab</c>.
/// Spawns <see cref="IceCircleQZone"/> above the player; the circle drops homing ice balls.
/// Owned by <c>PlayerCombat</c> — call <see cref="Cleanup"/> when switching weapons.
/// </summary>
public class IceSwordQSystem : MonoBehaviour
{
    #region Prefabs
    [Header("Ice Q Prefabs")]
    public GameObject circleVfxPrefab;
    public GameObject iceBallPrefab;
    #endregion

    #region Layout
    [Header("Circle Placement")]
    [Tooltip("Height above the player when Q opens, before the circle rises.")]
    public float startHeightAboveHead = 2.5f;

    [Tooltip("How far the circle rises upward after appearing.")]
    public float circleRiseHeight = 3.5f;

    [Tooltip("Seconds to rise from head height to final height.")]
    public float circleRiseDuration = 1.4f;
    #endregion

    #region Ice Ball Combat
    [Header("Ice Ball Combat")]
    public float targetRange = 24f;
    public float diveSpeed = 18f;
    public float fallSpeed = 10f;
    public float hitDistance = 1.1f;
    public float projectileLifeTime = 5f;

    [Tooltip("Movement speed multiplier removed from the enemy (0.35 = 35% slower).")]
    [Range(0f, 1f)] public float slowPercentage = 0.35f;

    [Tooltip("How long the slow debuff lasts after an ice ball hit.")]
    public float slowDuration = 3f;
    #endregion

    #region State
    private Transform player;
    private IceCircleQZone activeCircle;
    private bool isInitialized;
    #endregion

    #region Public API
    public bool IsActive => isInitialized;

    /// <summary>First Q press — parents to player and spawns the ice circle.</summary>
    public void Initialize(Transform playerTransform, float ballDamage, LayerMask enemies)
    {
        player = playerTransform;
        transform.SetParent(player);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        isInitialized = true;
        SpawnCircle(ballDamage, enemies);
    }

    /// <summary>Subsequent Q press while the system is still active — refreshes circle damage timing.</summary>
    public void Activate(float ballDamage, LayerMask enemies)
    {
        if (!isInitialized || player == null) return;

        if (activeCircle != null)
            activeCircle.Refresh(ballDamage);
        else
            SpawnCircle(ballDamage, enemies);
    }

    /// <summary>Called by <see cref="IceCircleQZone"/> when spawning a wave of ice balls.</summary>
    public void SpawnIceBall(Vector3 spawnPosition, float ballDamage, LayerMask enemies)
    {
        if (iceBallPrefab == null) return;

        EnemyBase target = FindNearestEnemy(enemies);
        GameObject ballObject = Instantiate(iceBallPrefab, spawnPosition, Quaternion.identity);

        IceBallProjectile projectile = ballObject.GetComponent<IceBallProjectile>();
        if (projectile == null)
            projectile = ballObject.AddComponent<IceBallProjectile>();

        projectile.Launch(
            target,
            ballDamage,
            diveSpeed,
            fallSpeed,
            hitDistance,
            projectileLifeTime,
            slowPercentage,
            slowDuration);
    }

    /// <summary>Circle lifetime ended — clears the active reference.</summary>
    public void NotifyCircleFinished(IceCircleQZone circle)
    {
        if (activeCircle == circle)
            activeCircle = null;
    }

    /// <summary>Stops VFX and destroys the manager. Called on weapon swap.</summary>
    public void Cleanup()
    {
        if (activeCircle != null)
        {
            activeCircle.Cleanup();
            activeCircle = null;
        }

        isInitialized = false;
        player = null;
        Destroy(gameObject);
    }
    #endregion

    #region Circle Spawn
    private void SpawnCircle(float ballDamage, LayerMask enemies)
    {
        if (circleVfxPrefab == null || player == null) return;

        if (activeCircle != null)
        {
            activeCircle.Cleanup();
            activeCircle = null;
        }

        Vector3 spawnPosition = player.position + Vector3.up * startHeightAboveHead;
        GameObject circleObject = Instantiate(circleVfxPrefab, spawnPosition, Quaternion.identity);

        IceCircleQZone circle = circleObject.GetComponent<IceCircleQZone>();
        if (circle == null)
            circle = circleObject.AddComponent<IceCircleQZone>();

        circle.riseHeight = circleRiseHeight;
        circle.riseDuration = circleRiseDuration;

        activeCircle = circle;
        // Open sound already played by PlayerCombat.PlaySkillQSound animation event.
        circle.Initialize(this, ballDamage, enemies, playOpenSound: false);
    }

    private EnemyBase FindNearestEnemy(LayerMask enemies)
    {
        Vector3 searchOrigin = activeCircle != null
            ? activeCircle.transform.position
            : player != null ? player.position : Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(searchOrigin, targetRange, enemies);
        EnemyBase nearest = null;
        float bestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>() ?? hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(searchOrigin, enemy.transform.position);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            nearest = enemy;
        }

        return nearest;
    }
    #endregion
}
