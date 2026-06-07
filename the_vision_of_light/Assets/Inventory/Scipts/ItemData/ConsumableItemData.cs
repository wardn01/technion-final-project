using UnityEngine;

/// <summary>
/// Defines a consumable inventory item.
/// Consumables can restore health instantly and/or over time,
/// and may have a cooldown before they can be used again.
/// </summary>
[CreateAssetMenu(fileName = "New Consumable", menuName = "Game Data/Inventory/Consumable")]
public class ConsumableItemData : ItemData
{
    #region Healing Settings

    [Header("Heal Settings")]

    /// <summary>
    /// Amount of health restored immediately upon use.
    /// </summary>
    public float instantHeal = 200f;

    /// <summary>
    /// Amount of health restored per healing tick.
    /// </summary>
    public float tickHealAmount = 100f;

    /// <summary>
    /// Time interval between healing ticks.
    /// </summary>
    public float tickInterval = 5f;

    /// <summary>
    /// Total number of healing ticks applied after use.
    /// </summary>
    public int totalTicks = 4;

    #endregion

    #region Cooldown Settings

    [Header("Cooldown Settings")]

    /// <summary>
    /// Cooldown duration in seconds before the consumable
    /// can be used again.
    /// </summary>
    public float cooldownTime = 40f;

    #endregion
}