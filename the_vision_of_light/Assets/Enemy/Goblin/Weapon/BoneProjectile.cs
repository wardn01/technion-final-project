using UnityEngine;

/// <summary>
/// Physics bone thrown by <see cref="Goblin"/>. Damages the player once on first hit.
/// </summary>
public class BoneProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 10f;

    private float damage;
    private bool canDamage = true;
    private bool hasDamagedPlayer;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(float dmgAmount)
    {
        damage = dmgAmount;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            canDamage = false;
            return;
        }

        if (!canDamage || hasDamagedPlayer) return;

        hasDamagedPlayer = true;
        canDamage = false;

        if (collision.gameObject.TryGetComponent(out PlayerHealth pHealth))
            pHealth.TakeDamage(damage);
    }
}
