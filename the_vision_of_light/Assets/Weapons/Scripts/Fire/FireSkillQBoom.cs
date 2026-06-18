using System.Collections.Generic;
using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Fire Sword [Q] forward boom. Spawns in front of the player, travels forward,
/// damages every enemy the wave passes through once, then disappears.
/// </summary>
[DisallowMultipleComponent]
public class FireSkillQBoom : MonoBehaviour
{
    #region Movement
    [Header("Movement")]
    public float speed = 12f;

    [Tooltip("How long the boom keeps moving before it disappears. Travel distance = speed × lifeTime.")]
    public float lifeTime = 3f;
    #endregion

    #region Hitbox
    [Header("Hitbox")]
    public Vector3 hitboxCenter = new Vector3(0f, 0.5f, 1.5f);
    public Vector3 hitboxSize = new Vector3(3f, 2f, 4f);
    #endregion

    #region Audio
    [Header("Audio")]
    public AudioClip strikeClip;
    [Range(0f, 1f)] public float strikeVolume = 1f;
    #endregion

    #region State
    private float damageAmount;
    private readonly HashSet<EnemyBase> enemiesHit = new HashSet<EnemyBase>();
    private AudioSource audioSource;
    private BoxCollider hitbox;
    #endregion

    #region Public API
    /// <summary>Called by <c>PlayerCombat</c> when the Q strike prefab is spawned.</summary>
    public void SetDamage(float damage) => damageAmount = damage;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
        EnsureHitbox();
        EnsureRigidbody();
        EnsureAudioSource();
    }

    private void Start()
    {
        if (strikeClip != null && audioSource != null)
            audioSource.PlayOneShot(strikeClip, strikeVolume);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
    #endregion

    #region Hit Detection
    private void OnTriggerEnter(Collider other) => DamageEnemy(other);

    private void DamageEnemy(Collider other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy == null || enemy.IsDead || !enemiesHit.Add(enemy)) return;

        enemy.TakeDamage(damageAmount);
    }
    #endregion

    #region Setup
    private void EnsureHitbox()
    {
        hitbox = GetComponent<BoxCollider>();
        if (hitbox == null) hitbox = gameObject.AddComponent<BoxCollider>();

        hitbox.isTrigger = true;
        hitbox.center = hitboxCenter;
        hitbox.size = hitboxSize;
    }

    private void EnsureRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void EnsureAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }
    #endregion
}
