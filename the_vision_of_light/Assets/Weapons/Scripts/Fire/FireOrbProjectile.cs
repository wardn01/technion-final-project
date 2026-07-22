using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Homing fire orb launched from <see cref="FireSwordQOrbitSystem"/>.
/// Grows while flying toward its target and damages on impact.
/// Added at runtime to <c>Q_Fireball.prefab</c> — not attached in the prefab asset.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FireOrbProjectile : MonoBehaviour
{
    #region State
    private EnemyBase target;
    private float damage;
    private float speed;
    private float hitDistance;
    private float lifeTime;
    private float startScale;
    private float endScale;
    private float growDuration;
    private bool hasHit;
    private float spawnTime;
    private AudioClip hitClip;
    private float hitVolume;
    #endregion

    #region Public API
    /// <summary>Called by <see cref="FireSwordQOrbitSystem.TryLaunchOrb"/> when an orb is fired.</summary>
    public void Launch(
        EnemyBase enemy,
        float skillDamage,
        float moveSpeed,
        float reachDistance,
        float maxLifeTime,
        float initialScale,
        float targetScale,
        float scaleGrowDuration,
        AudioClip impactClip,
        float impactVolume)
    {
        target = enemy;
        damage = skillDamage;
        speed = moveSpeed;
        hitDistance = reachDistance;
        lifeTime = maxLifeTime;
        startScale = initialScale;
        endScale = targetScale;
        growDuration = Mathf.Max(0.1f, scaleGrowDuration);
        hitClip = impactClip;
        hitVolume = impactVolume;
        spawnTime = Time.time;

        transform.localScale = Vector3.one * startScale;

        SphereCollider collider = GetComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.35f;
    }
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (hasHit) return;

        float elapsed = Time.time - spawnTime;
        float growT = Mathf.Clamp01(elapsed / growDuration);
        float currentScale = Mathf.Lerp(startScale, endScale, growT);
        transform.localScale = Vector3.one * currentScale;

        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 aimPoint = target.transform.position + Vector3.up * 1f;
        Vector3 direction = (aimPoint - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);

        if (Vector3.Distance(transform.position, aimPoint) <= hitDistance)
            HitTarget();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>() ?? other.GetComponentInParent<EnemyBase>();
        if (enemy == null || enemy.IsDead) return;
        if (target != null && enemy != target) return;

        HitTarget(enemy);
    }
    #endregion

    #region Hit Detection
    private void HitTarget(EnemyBase enemy = null)
    {
        if (hasHit) return;
        hasHit = true;

        EnemyBase hitEnemy = enemy ?? target;
        if (hitEnemy != null && !hitEnemy.IsDead)
            hitEnemy.TakeDamage(damage, true, WeaponItemData.WeaponElement.Fire);

        if (hitClip != null)
            AudioSource.PlayClipAtPoint(hitClip, transform.position, hitVolume);

        Destroy(gameObject, 0.15f);
    }
    #endregion
}
