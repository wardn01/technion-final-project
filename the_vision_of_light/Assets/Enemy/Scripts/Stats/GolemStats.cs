using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Golem boss stats — summon threshold at 30% HP drives the RageGolem HUD meter. Data: Golem/Data/GolemData.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "GolemData", menuName = "Game Data/Enemy/Boss/Golem Stats")]
    public class GolemStats : BossEnemyStats
    {
        #region Serialized Fields
        [Header("Ranged Combat (Stone Throw)")]
        [Tooltip("Rigidbody impulse applied to the stone projectile.")]
        [SerializeField] private float projectileSpeed = 26f;
        [Tooltip("Throw damage as % of scaled currentAttack.")]
        [SerializeField] private float throwDamage = 85f;
        #endregion

        #region Public Accessors
        public float ProjectileSpeed => projectileSpeed;
        public float ThrowDamage => throwDamage;
        #endregion
    }
}
