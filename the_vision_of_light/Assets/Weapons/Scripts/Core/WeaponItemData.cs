using UnityEngine;
using VisionOfLight.Player;

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
/// Assets live in <c>Resources/Weapon/</c> (Wind Sword, Ice Sword, Fire Sword).
/// </summary>
/// <remarks>
/// Per-weapon field usage:
/// <list type="bullet">
///   <item><b>Wind</b> — generic Q via <see cref="skillQPrefab"/>; <see cref="skillQStrikePrefab"/> unused; no animator override.</item>
///   <item><b>Fire</b> — <see cref="skillQPrefab"/> = orbit manager; <see cref="skillQStrikePrefab"/> = Q_FireBoom; <see cref="skillQStrikeDamage"/> = boom percent.</item>
///   <item><b>Ice</b> — <see cref="skillQPrefab"/> = IceSwordQSystem manager; <see cref="skillQStrikePrefab"/> unused.</item>
/// </list>
/// Damage percent fields are applied in <c>PlayerCombat</c> against total attack (player + weapon base + upgrades).
/// </remarks>
[CreateAssetMenu(fileName = "New Weapon", menuName = "Game Data/Inventory/Weapon")]
public class WeaponItemData : ItemData
{
    #region Element
    public enum WeaponElement { None, Wind, Ice, Fire }

    [Header("Weapon Element")]
    [Tooltip("Drives HUD skill group and PlayerCombat skill branching.")]
    public WeaponElement weaponElement;
    #endregion

    #region Visuals
    [Header("Weapon Visuals")]
    [Tooltip("Model prefab parented to the player's hand when equipped.")]
    public GameObject weaponModelPrefab;

    /// <summary>Optional per-weapon animator. Wind leaves this null — base PlayerAnimation controller is used.</summary>
    public AnimatorOverrideController animatorOverride;
    #endregion

    #region Combat Stats
    [Header("Combat Stats")]
    [Tooltip("Flat attack added on top of player base attack.")]
    public int weaponBaseAttack = 50;

    [Tooltip("Normal combo (Attack 1/2/3). Percent of total attack.")]
    public int normalAttackDamage = 80;

    [Tooltip("Skill E projectile. Percent of total attack.")]
    public int skillEDamage = 250;

    [Tooltip("Skill Q — Wind tornado, Fire orbs, Ice balls. Percent of total attack.")]
    public int skillQDamage = 100;

    [Tooltip("Fire Sword Q_FireBoom only. Ice/Wind leave at 0.")]
    public int skillQStrikeDamage;
    #endregion

    #region Skills
    [Header("VFX & Skills")]
    [Tooltip("Spawned at eSpawnPoint on Skill E animation event.")]
    public GameObject skillEPrefab;

    [Tooltip("Wind/Ice: skill VFX. Fire: FireSwordQOrbitSystem manager.")]
    public GameObject skillQPrefab;

    [Tooltip("Fire Sword only — Q_FireBoom spawned at qSpawnPoint.")]
    public GameObject skillQStrikePrefab;

    [Header("Skill Settings")]
    public float skillECooldown = 8f;

    [Tooltip("Successful E casts required before Q becomes available.")]
    public int requiredE_For_Q = 2;
    #endregion

    #region Upgrades
    [Header("Upgrade System")]
    public WeaponUpgradeLevel[] upgradeLevels;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        type = ItemType.Weapon;
    }
    #endregion
}
