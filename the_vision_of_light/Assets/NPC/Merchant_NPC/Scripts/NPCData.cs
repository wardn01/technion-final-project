using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New NPC Data", menuName = "Game Data/NPC Data")]
public class NPCData : ScriptableObject
{
    [Header("NPC Identity")]
    public string npcName = "Merchant";
    public Sprite npcIcon;
    
    [Header("Shop Inventory")]
    public List<ItemData> itemsToSell;
}