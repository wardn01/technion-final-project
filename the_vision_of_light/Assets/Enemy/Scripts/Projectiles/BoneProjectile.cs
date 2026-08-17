using UnityEngine;
using VisionOfLight.Player;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Physics bone thrown by <see cref="Goblin"/>. Damages the player once on first hit.
    /// </summary>
    public class BoneProjectile : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private float lifeTime = 10f;

        #endregion

        #region Runtime State

        private float damage;
        private bool canDamage = true;
        private bool hasDamagedPlayer;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        #endregion

        #region Combat

        /// <summary>Sets impact damage from the thrower's scaled attack.</summary>
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

        #endregion
    }
}
