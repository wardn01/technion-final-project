using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Identity, map icon, welcome lines, and shop inventory for an NPC.
/// Used by <see cref="StoryNPC"/> and <see cref="ShopkeeperNPC"/>.
/// </summary>
[CreateAssetMenu(fileName = "New NPC Data", menuName = "Game Data/NPC/NPC Data")]
public class NPCData : ScriptableObject
{
    [Header("NPC Identity")]
    public string npcName = "Merchant";

    [Tooltip("Icon for minimap and shopkeeper overhead UI.")]
    public Sprite npcIcon;

    [Header("Shop Dialogue")]
    [TextArea(3, 10)]
    [Tooltip("Default lines when no quest-specific dialogue matches.")]
    public string[] welcomeDialogue;

    [Header("Shop Inventory")]
    [Tooltip("Items sold when this NPC opens the shop (shopkeepers only).")]
    public List<ItemData> itemsToSell;
}
