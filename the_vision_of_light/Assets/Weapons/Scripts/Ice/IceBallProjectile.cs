using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Ice Q projectile spawned from <see cref="IceCircleQZone"/>.
/// Dives toward a target enemy and applies a timed slow via <see cref="EnemyStatusEffects"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class IceBallProjectile : MonoBehaviour
{
    #region Audio
    [Header("Audio")]
    public AudioClip spawnClip;
    [Range(0f, 1f)] public float spawnVolume = 0.85f;
    public AudioClip hitClip;
    [Range(0f, 1f)] public float hitVolume = 1f;
    #endregion

    #region State
    private EnemyBase target;
    private float damage;
    private float diveSpeed;
    private float fallSpeed;
    private float hitDistance;
    private float lifeTime;
    private float slowPercentage;
    private float slowDuration;
    private float spawnTime;
    private bool hasHit;
    private AudioSource audioSource;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        if (hasHit) return;

        if (Time.time - spawnTime >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || target.IsDead)
        {
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
            return;
        }

        Vector3 aimPoint = target.transform.position + Vector3.up * 1f;
        Vector3 toTarget = aimPoint - transform.position;
        Vector3 direction = toTarget.normalized;
        float moveSpeed = direction.y < -0.1f ? Mathf.Max(diveSpeed, fallSpeed) : diveSpeed;
        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);

        if (toTarget.magnitude <= hitDistance)
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

    #region Public API
    /// <summary>Called by <see cref="IceSwordQSystem.SpawnIceBall"/> after the ball is instantiated.</summary>
    public void Launch(
        EnemyBase enemy,
        float skillDamage,
        float moveSpeed,
        float dropSpeed,
        float reachDistance,
        float maxLifeTime,
        float slowPercent,
        float slowSeconds)
    {
        target = enemy;
        damage = skillDamage;
        diveSpeed = moveSpeed;
        fallSpeed = dropSpeed;
        hitDistance = reachDistance;
        lifeTime = maxLifeTime;
        slowPercentage = slowPercent;
        slowDuration = slowSeconds;
        spawnTime = Time.time;

        EnsureCollider();

        if (spawnClip != null && audioSource != null)
            audioSource.PlayOneShot(spawnClip, spawnVolume);
    }
    #endregion

    #region Hit Detection
    private void HitTarget(EnemyBase enemy = null)
    {
        if (hasHit) return;
        hasHit = true;

        EnemyBase hitEnemy = enemy ?? target;
        if (hitEnemy != null && !hitEnemy.IsDead)
        {
            hitEnemy.TakeDamage(damage);

            EnemyStatusEffects status = hitEnemy.GetComponent<EnemyStatusEffects>();
            if (status != null && slowPercentage > 0f)
                status.ApplySlow(slowPercentage, slowDuration);
        }

        if (hitClip != null)
            AudioSource.PlayClipAtPoint(hitClip, transform.position, hitVolume);

        Destroy(gameObject, 0.15f);
    }
    #endregion

    #region Setup
    private void EnsureCollider()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
    #endregion
}
