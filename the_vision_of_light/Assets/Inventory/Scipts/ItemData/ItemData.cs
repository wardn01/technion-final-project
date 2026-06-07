using UnityEngine;

/// <summary>
/// Defines the available inventory item categories.
/// Used for inventory filtering, item behavior,
/// and gameplay systems.
/// </summary>
public enum ItemType
{
    Material,
    Consumable,
    Weapon,
    Currency
}

/// <summary>
/// Base ScriptableObject for all inventory items.
/// Stores common item information such as name,
/// description, icon, value, and item type.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Game Data/Inventory/Item")]
public class ItemData : ScriptableObject
{
    #region Item Information

    [Header("Item Info")]

    /// <summary>
    /// Display name of the item.
    /// </summary>
    public string itemName;

    /// <summary>
    /// Description shown in the inventory details panel.
    /// </summary>
    [TextArea(2, 4)]
    public string description;

    /// <summary>
    /// Icon displayed in the inventory UI.
    /// </summary>
    public Sprite itemIcon;

    #endregion

    #region Item Properties

    [Header("Item Details")]

    /// <summary>
    /// Monetary value or worth of the item.
    /// </summary>
    public int value;

    /// <summary>
    /// Category of the item.
    /// Determines how the item behaves in the inventory.
    /// </summary>
    public ItemType type;

    #endregion
}