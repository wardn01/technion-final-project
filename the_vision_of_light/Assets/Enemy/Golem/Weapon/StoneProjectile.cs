using UnityEngine;

/// <summary>
/// Stone thrown by <see cref="Golem"/>. Trigger hitbox + overlap check — same pattern as Goblin bone fix for CharacterController.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class StoneProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 12f;
    [SerializeField] private float hitCheckPadding = 0.45f;

    private float damage;
    private bool hasDamagedPlayer;
    private SphereCollider hitSphere;
    private EnemyAudioEmitter audioEmitter;

    private void Awake()
    {
        hitSphere = GetComponent<SphereCollider>();
        if (hitSphere != null)
            hitSphere.isTrigger = true;
    }

    private void Start()
    {
        audioEmitter?.PlayClip("StoneFly");
        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(float dmgAmount)
    {
        damage = dmgAmount;
    }

    public void BindAudio(EnemyAudioEmitter emitter)
    {
        audioEmitter = emitter;
    }

    private void FixedUpdate()
    {
        if (hasDamagedPlayer)
            return;

        CheckPlayerOverlap();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasDamagedPlayer || other == null)
            return;

        if (other.CompareTag("Enemy"))
            return;

        TryDamagePlayer(other.gameObject);
    }

    private void CheckPlayerOverlap()
    {
        if (hitSphere == null)
            return;

        float radius = hitSphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        radius += hitCheckPadding;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            if (hit == null || hit == hitSphere)
                continue;

            if (hit.CompareTag("Enemy"))
                continue;

            if (TryDamagePlayer(hit.gameObject))
                return;
        }
    }

    private bool TryDamagePlayer(GameObject hitObject)
    {
        PlayerHealth playerHealth = hitObject.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.isDead)
            return false;

        hasDamagedPlayer = true;
        playerHealth.TakeDamage(damage);
        audioEmitter?.PlayClipAt("StoneImpact", transform.position);
        Destroy(gameObject);
        return true;
    }
}
