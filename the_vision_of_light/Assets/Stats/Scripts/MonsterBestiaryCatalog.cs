using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>One bestiary species: stats asset plus optional UI extras.</summary>
[System.Serializable]
public class MonsterBestiaryEntry
{
    [Tooltip("Enemy stats ScriptableObject. Asset name is the stable monsterId (e.g. OrcData).")]
    public EnemyBaseStats stats;

    [Tooltip("Large portrait for the right Info panel. If empty, uses EnemyBaseStats icon.")]
    public Sprite icon;

    [Tooltip("Small thumbnail for the left scroll list (e.g. *-MiniIcon). Falls back to icon.")]
    public Sprite listIcon;

    [TextArea(2, 4)]
    [Tooltip("Optional flavor text shown when the entry is unlocked.")]
    public string description;
}

/// <summary>
/// Ordered catalog of monsters for the bestiary screen.
/// Create via Assets → Create → Game Data → Monster Bestiary Catalog
/// (or Vision Of Light → Bestiary → Rebuild Catalog).
/// </summary>
[CreateAssetMenu(fileName = "MonsterBestiaryCatalog", menuName = "Game Data/Monster Bestiary Catalog")]
public class MonsterBestiaryCatalog : ScriptableObject
{
    public MonsterBestiaryEntry[] entries;
}
