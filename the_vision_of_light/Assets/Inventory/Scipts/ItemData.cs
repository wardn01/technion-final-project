using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game Data/Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite itemIcon;

    [Header("Item Details")]
    public int value;
    public ItemType type;
}

public enum ItemType
{
    Material,
    Consumable,
    Weapon,
    Currency
}