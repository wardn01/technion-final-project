using UnityEngine;

public class BoneProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifeTime = 10f;
    private float damage;
    
    private bool canDamage = true;
    private bool hasDamagedPlayer = false;

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
        }

        if (collision.gameObject.CompareTag("Player") && canDamage && !hasDamagedPlayer)
        {
            hasDamagedPlayer = true; 
            canDamage = false; 
            
            Debug.Log("Bone projectile hit player in air! Damage: " + damage);
            
            PlayerHealth pHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damage);
            }
        }
    }
}