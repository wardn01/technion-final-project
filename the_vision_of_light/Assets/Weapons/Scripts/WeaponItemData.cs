using UnityEngine;

/// <summary>
/// Materials required to upgrade a weapon to the next level.
/// </summary>
[System.Serializable]
public struct WeaponUpgradeLevel
{
    public ItemRequirement[] materials;
    public int damageBoost;
}

/// <summary>
/// ScriptableObject for equippable weapons. Extends <see cref="ItemData"/> with combat stats,
/// skill prefabs, optional <see cref="animatorOverride"/>, and an upgrade path.
/// Wind Sword intentionally leaves <see cref="animatorOverride"/> empty and uses the player's base controller.
/// </summary>
[CreateAssetMenu(fileName = "New Weapon", menuName = "Game Data/Inventory/Weapon")]
public class WeaponItemData : ItemData
{
    #region Element
    public enum WeaponElement { None, Wind, Ice, Fire }

    [Header("Weapon Element")]
    public WeaponElement weaponElement;
    #endregion

    #region Visuals
    [Header("Weapon Visuals")]
    public GameObject weaponModelPrefab;

    /// <summary>Optional per-weapon animator. Leave null to keep the player's base combat controller.</summary>
    public AnimatorOverrideController animatorOverride;
    #endregion

    #region Combat Stats
    [Header("Combat Stats")]
    public int weaponBaseAttack = 50;

    [Tooltip("Normal combo (Attack 1/2/3). Percent of total attack.")]
    public int normalAttackDamage = 80;

    [Tooltip("Skill E. Percent of total attack.")]
    public int skillEDamage = 250;

    [Tooltip("Skill Q — Wind/Ice Q, Fire Sword orbiting fireballs. Percent of total attack.")]
    public int skillQDamage = 100;

    [Tooltip("Fire Sword only — forward Q_FireBoom. Percent of total attack. If 0, uses Skill Q Damage.")]
    public int skillQStrikeDamage;
    #endregion

    #region Skills
    [Header("VFX & Skills")]
    public GameObject skillEPrefab;
    public GameObject skillQPrefab;

    [Tooltip("Optional secondary Q VFX spawned at qSpawnPoint (e.g. Fire Sword forward boom).")]
    public GameObject skillQStrikePrefab;

    [Header("Skill Settings")]
    public float skillECooldown = 8f;
    public int requiredE_For_Q = 2;
    #endregion

    #region Upgrades
    [Header("Upgrade System")]
    public WeaponUpgradeLevel[] upgradeLevels;
    #endregion

    private void OnEnable()
    {
        type = ItemType.Weapon;
    }
}
