#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>Editor helper to create/rebuild the bestiary catalog from all EnemyBaseStats assets.</summary>
public static class MonsterBestiaryCatalogEditor
{
    private const string CatalogPath = "Assets/Stats/MonsterBestiaryCatalog.asset";
    private const string ResourcesCatalogPath = "Assets/Resources/MonsterBestiaryCatalog.asset";

    [MenuItem("Vision Of Light/Bestiary/Rebuild Catalog")]
    public static void RebuildCatalog()
    {
        MonsterBestiaryCatalog catalog = LoadOrCreateCatalog(CatalogPath);
        RebuildEntries(catalog);

        // Keep Resources copy in sync (runtime fallback via Resources.Load).
        MonsterBestiaryCatalog resourcesCatalog = LoadOrCreateCatalog(ResourcesCatalogPath);
        resourcesCatalog.entries = CloneEntries(catalog.entries);
        EditorUtility.SetDirty(resourcesCatalog);

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Bestiary] Catalog rebuilt with {catalog.entries.Length} entries.\n- {CatalogPath}\n- {ResourcesCatalogPath}");
        Selection.activeObject = catalog;
    }

    private static MonsterBestiaryCatalog LoadOrCreateCatalog(string path)
    {
        MonsterBestiaryCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterBestiaryCatalog>(path);
        if (catalog != null)
            return catalog;

        catalog = ScriptableObject.CreateInstance<MonsterBestiaryCatalog>();
        string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        AssetDatabase.CreateAsset(catalog, path);
        return catalog;
    }

    private static void RebuildEntries(MonsterBestiaryCatalog catalog)
    {
        string[] guids = AssetDatabase.FindAssets("t:EnemyBaseStats");
        List<EnemyBaseStats> statsList = new List<EnemyBaseStats>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyBaseStats stats = AssetDatabase.LoadAssetAtPath<EnemyBaseStats>(path);
            if (stats != null)
                statsList.Add(stats);
        }

        string[] preferredOrder =
        {
            "GoblinData", "SkeletonData", "ImpData", "BearData",
            "MiniGolemData", "OrcData", "GolemData"
        };

        statsList = statsList
            .OrderBy(s =>
            {
                int index = System.Array.IndexOf(preferredOrder, s.name);
                return index >= 0 ? index : 1000;
            })
            .ThenBy(s => s.EnemyName)
            .ToList();

        Dictionary<string, MonsterBestiaryEntry> previous = new Dictionary<string, MonsterBestiaryEntry>();
        if (catalog.entries != null)
        {
            foreach (MonsterBestiaryEntry entry in catalog.entries)
            {
                if (entry?.stats == null)
                    continue;
                previous[entry.stats.name] = entry;
            }
        }

        List<MonsterBestiaryEntry> rebuilt = new List<MonsterBestiaryEntry>();
        foreach (EnemyBaseStats stats in statsList)
        {
            if (previous.TryGetValue(stats.name, out MonsterBestiaryEntry existing))
            {
                existing.stats = stats;
                if (existing.icon == null)
                    existing.icon = stats.Icon;
                rebuilt.Add(existing);
            }
            else
            {
                rebuilt.Add(new MonsterBestiaryEntry
                {
                    stats = stats,
                    icon = stats.Icon,
                    description = string.Empty
                });
            }
        }

        catalog.entries = rebuilt.ToArray();
    }

    private static MonsterBestiaryEntry[] CloneEntries(MonsterBestiaryEntry[] source)
    {
        if (source == null)
            return new MonsterBestiaryEntry[0];

        MonsterBestiaryEntry[] clone = new MonsterBestiaryEntry[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            MonsterBestiaryEntry e = source[i];
            clone[i] = e == null
                ? null
                : new MonsterBestiaryEntry
                {
                    stats = e.stats,
                    icon = e.icon,
                    listIcon = e.listIcon,
                    description = e.description
                };
        }

        return clone;
    }
}
#endif
