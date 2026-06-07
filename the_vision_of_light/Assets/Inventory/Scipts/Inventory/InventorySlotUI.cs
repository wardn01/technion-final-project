using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// A reusable UI component representing a single inventory grid slot.
/// Utilizes delegate actions (Action) to remain decoupled from core managers,
/// making it highly efficient for Object Pooling and scalable grid layouts.
/// </summary>
[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    #region Components

    [Header("Player Data Reference")]
    
    /// <summary>
    /// Reference to the player's saved data profile.
    /// Required dynamically to fetch persistent weapon levels instead of static item amounts.
    /// </summary>
    public PlayerData playerData;

    /// <summary>
    /// The visual representation of the item assigned to this slot.
    /// </summary>
    public Image icon;
    
    /// <summary>
    /// Displays the item's stack quantity, or its upgrade level if the item is a weapon.
    /// </summary>
    public TextMeshProUGUI amountText;

    #endregion

    #region Slot Data

    /// <summary>
    /// The specific item data currently bound to this UI slot.
    /// </summary>
    private ItemData itemData;

    /// <summary>
    /// A delegate callback used to notify the subscriber (usually the InventoryUIManager) 
    /// when this slot is clicked, passing the attached item data safely.
    /// </summary>
    private Action<ItemData> onSlotClickedCallback;

    #endregion

    #region Slot Setup

    /// <summary>
    /// Configures the visual state and interaction logic of the slot.
    /// Formats text dynamically based on the item type (Weapon Level vs. Consumable Stack).
    /// </summary>
    /// <param name="item">The ItemData object to bind to this slot.</param>
    /// <param name="amount">The total quantity of the item owned by the player.</param>
    /// <param name="onClickAction">The method to execute when the player clicks this slot.</param>
    public void Setup(ItemData item, int amount, Action<ItemData> onClickAction)
    {
        itemData = item;
        onSlotClickedCallback = onClickAction;

        if (icon != null)
            icon.sprite = item.itemIcon;

        if (amountText != null)
        {
            if (item.type == ItemType.Weapon)
            {
                int lvl = playerData != null
                    ? playerData.GetWeaponLevel(item.itemName)
                    : 1;

                amountText.text = "Lv." + lvl;
            }
            else
            {
                amountText.text = amount.ToString();
            }
        }

        Button btn = GetComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnSlotClicked);
    }

    #endregion

    #region Button Events

    /// <summary>
    /// Broadcasts the click event to the subscribed manager.
    /// Using the Null-Conditional Operator (?.) ensures safety if no action is bound.
    /// </summary>
    private void OnSlotClicked()
    {
        onSlotClickedCallback?.Invoke(itemData);
    }

    #endregion
}