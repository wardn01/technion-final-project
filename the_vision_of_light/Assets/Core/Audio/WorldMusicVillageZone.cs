using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a safe village area for <see cref="WorldMusicManager"/>.
/// Use a large trigger collider that covers the village.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldMusicVillageZone : MonoBehaviour
{
    private static readonly HashSet<WorldMusicVillageZone> ActiveZones = new();

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null && !zoneCollider.isTrigger)
            Debug.LogWarning($"[WorldMusicVillageZone] {name}: Collider must have Is Trigger enabled.", this);
    }

    private void OnEnable()
    {
        ActiveZones.Add(this);
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveZones.Remove(this);
    }

    /// <summary>
    /// Position-based check also handles map teleports that skip OnTriggerExit.
    /// </summary>
    public static bool ContainsPlayer(Vector3 playerPosition)
    {
        ActiveZones.RemoveWhere(zone => zone == null);

        foreach (WorldMusicVillageZone zone in ActiveZones)
        {
            if (zone.Contains(playerPosition))
                return true;
        }

        return false;
    }

    private bool Contains(Vector3 position)
    {
        if (zoneCollider == null || !zoneCollider.enabled || !gameObject.activeInHierarchy)
            return false;

        Vector3 closest = zoneCollider.ClosestPoint(position);
        return (closest - position).sqrMagnitude <= 0.0025f;
    }
}
