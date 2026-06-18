using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The central data manager for the player's inventory system.
/// Handles the persistent storage of items, quantity management,
/// and instantiates UI pickup notifications when new items are acquired.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Global singleton instance for easy access from other scripts.
    /// Ensures only one inventory data system exists during runtime.
    /// </summary>
    public static InventoryManager Instance { get; private set; }

    #endregion

    #region UI References

    [Header("Pickup Notifications")]
    
    /// <summary>
    /// The UI prefab instantiated when an item is added to the inventory.
    /// </summary>
    public GameObject notificationPrefab;
    
    /// <summary>
    /// The parent transform (usually a layout group) where notifications will spawn.
    /// </summary>
    public Transform notificationParent;

    #endregion

    #region Inventory Data

    /// <summary>
    /// The core data structure storing all inventory items as keys and their current quantities as values.
    /// Utilizes a Dictionary for high-performance O(1) lookups.
    /// </summary>
    private Dictionary<ItemData, int> inventory = new Dictionary<ItemData, int>();

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the Singleton instance and ensures this manager persists across scene loads.
    /// Destroys any duplicate instances to maintain data integrity.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Inventory Management

    /// <summary>
    /// Adds a specified quantity of an item to the inventory.
    /// Safely handles new items, updates existing quantities, and triggers a UI pickup notification.
    /// </summary>
    /// <param name="item">The ItemData object to add to the inventory.</param>
    /// <param name="amount">The quantity to add (default is 1).</param>
    /// <param name="silent">When true, skips the pickup notification (e.g. save load).</param>
    public void AddItem(ItemData item, int amount = 1, bool silent = false)
    {
        if (item == null)
            return;

        // Update quantity or add new dictionary entry
        if (inventory.ContainsKey(item))
            inventory[item] += amount;
        else
            inventory.Add(item, amount);

        // Safely remove the item if the amount somehow falls to 0 or below
        if (inventory[item] <= 0)
        {
            inventory.Remove(item);
        }

        // Trigger UI notification only if a positive amount was actually added
        if (amount > 0 && !silent)
            ShowPickupNotification(item, amount);
    }

    private void ShowPickupNotification(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || notificationPrefab == null || notificationParent == null)
            return;

        for (int i = notificationParent.childCount - 1; i >= 0; i--)
        {
            PickupNotification existing = notificationParent.GetChild(i).GetComponent<PickupNotification>();
            if (existing != null && existing.TryStack(item, amount))
            {
                existing.transform.SetAsLastSibling();
                return;
            }
        }

        GameObject notif = Instantiate(notificationPrefab, notificationParent);
        notif.transform.SetAsLastSibling();

        PickupNotification notifScript = notif.GetComponent<PickupNotification>();
        if (notifScript != null)
            notifScript.Setup(item, amount);
    }

    private void ClearPickupNotifications()
    {
        if (notificationParent == null)
            return;

        for (int i = notificationParent.childCount - 1; i >= 0; i--)
            Destroy(notificationParent.GetChild(i).gameObject);
    }

    /// <summary>
    /// Decreases the quantity of a specific item in the inventory.
    /// Automatically removes the item from the dictionary if the quantity reaches zero or less.
    /// </summary>
    /// <param name="item">The ItemData object to remove.</param>
    /// <param name="amount">The quantity to subtract (default is 1).</param>
    public void RemoveItem(ItemData item, int amount = 1)
    {
        if (inventory.ContainsKey(item))
        {
            inventory[item] -= amount;

            // Cleanup the dictionary to prevent memory leaks or zero-quantity UI bugs
            if (inventory[item] <= 0)
            {
                inventory.Remove(item);
            }
        }
    }

    /// <summary>
    /// Completely empties the player's inventory.
    /// Typically used when loading a new clean save state or resetting the character.
    /// </summary>
    public void ClearInventory()
    {
        inventory.Clear();
        ClearPickupNotifications();
    }

    #endregion

    #region Inventory Queries

    /// <summary>
    /// Checks the inventory for a specific item and retrieves its current quantity.
    /// </summary>
    /// <param name="item">The ItemData object to query.</param>
    /// <returns>The quantity of the item owned by the player, or 0 if the item is not in the inventory.</returns>
    public int GetItemAmount(ItemData item)
    {
        if (inventory.ContainsKey(item))
            return inventory[item];

        return 0;
    }

    /// <summary>
    /// Retrieves the entire inventory data structure.
    /// </summary>
    /// <returns>The underlying Dictionary containing all current items and their quantities.</returns>
    public Dictionary<ItemData, int> GetInventory()
    {
        return inventory;
    }

    #endregion
}