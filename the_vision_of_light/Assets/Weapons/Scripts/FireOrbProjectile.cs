using UnityEngine;

/// <summary>
/// Homing fire orb launched from <see cref="FireSwordQOrbitSystem"/>. Grows while flying toward its target.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FireOrbProjectile : MonoBehaviour
{
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

    private void HitTarget(EnemyBase enemy = null)
    {
        if (hasHit) return;
        hasHit = true;

        EnemyBase hitEnemy = enemy ?? target;
        if (hitEnemy != null && !hitEnemy.IsDead)
            hitEnemy.TakeDamage(damage);

        if (hitClip != null)
            AudioSource.PlayClipAtPoint(hitClip, transform.position, hitVolume);

        Destroy(gameObject, 0.15f);
    }
}
