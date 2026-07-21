using UnityEngine;
using VisionOfLight.Player;

/// <summary>
/// Playable world area (Genshin-style). While the player is outside this trigger,
/// they are warped to the nearest unlocked teleport with the normal loading screen.
/// Place a large Box/Mesh Collider (Is Trigger) that covers the island / playable volume.
/// </summary>
[RequireComponent(typeof(Collider))]
[DefaultExecutionOrder(50)]
public class WorldBoundsZone : MonoBehaviour
{
    public static WorldBoundsZone Instance { get; private set; }

    private const float InsideToleranceSqr = 0.25f;

    [Header("Detection")]
    [Tooltip("How often to re-check player position (also reacts on TriggerExit).")]
    [Min(0.05f)]
    public float pollInterval = 0.2f;

    [Tooltip("Ignore OOB checks this long after a recovery teleport finishes.")]
    [Min(0f)]
    public float recoveryCooldown = 1.5f;

    [Header("Soft Warning (optional)")]
    [Tooltip("Warn when the player is still inside but this close to the edge (world meters). 0 = off.")]
    [Min(0f)]
    public float softWarningEdgeDistance = 12f;

    [Tooltip("Shown via NotificationManager when approaching the edge.")]
    public string softWarningMessage = "Leaving the exploration area...";

    [Tooltip("Minimum seconds between soft warnings.")]
    [Min(0.5f)]
    public float softWarningCooldown = 4f;

    [Header("Fallback")]
    [Tooltip("Used only if no teleport is unlocked yet.")]
    public Transform fallbackSpawn;

    [Header("Minimap")]
    [Tooltip("Auto-create the red border + outside fog on the minimap.")]
    public bool showOnMinimap = true;

    private Collider zoneCollider;
    private SphereCollider sphereCollider;
    private Transform player;
    private bool isRecovering;
    private float nextPollTime;
    private float recoverUntil;
    private float nextSoftWarningTime;
    private bool wasInside = true;

    /// <summary>World-space AABB of the playable collider (for map overlay).</summary>
    public Bounds PlayableBounds => zoneCollider != null ? zoneCollider.bounds : new Bounds(transform.position, Vector3.one);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[WorldBoundsZone] Multiple instances — keeping the first one.", this);
            return;
        }

        Instance = this;
        zoneCollider = GetComponent<Collider>();
        sphereCollider = zoneCollider as SphereCollider;
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }

    /// <summary>True when the playable area is a circle (SphereCollider) — checked on the XZ plane only.</summary>
    public bool TryGetCircle(out Vector3 center, out float radius)
    {
        if (sphereCollider == null)
        {
            center = transform.position;
            radius = 0f;
            return false;
        }

        center = sphereCollider.transform.TransformPoint(sphereCollider.center);
        Vector3 s = sphereCollider.transform.lossyScale;
        radius = sphereCollider.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        player = ResolvePlayer();
        EnsureMinimapBorder();
    }

    private void EnsureMinimapBorder()
    {
        if (!showOnMinimap)
            return;

        if (GetComponentInChildren<WorldBoundsMinimapBorder>(true) != null)
            return;

        GameObject child = new GameObject("MinimapBorder");
        child.transform.SetParent(transform, false);
        child.AddComponent<WorldBoundsMinimapBorder>();
    }

    private void Update()
    {
        if (isRecovering || Time.unscaledTime < recoverUntil)
            return;

        if (TeleportManager.Instance != null && TeleportManager.Instance.IsTraveling)
            return;

        if (player == null)
        {
            player = ResolvePlayer();
            if (player == null)
                return;
        }

        PlayerHealth health = PlayerRegistry.Instance != null ? PlayerRegistry.Instance.Health : null;
        if (health != null && health.isDead)
            return;

        if (Time.unscaledTime < nextPollTime)
            return;

        nextPollTime = Time.unscaledTime + pollInterval;

        bool inside = IsInside(player.position);
        if (inside)
        {
            wasInside = true;
            TrySoftWarning(player.position);
            return;
        }

        if (!wasInside)
            return;

        wasInside = false;
        BeginRecovery();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isRecovering || Time.unscaledTime < recoverUntil)
            return;

        if (TeleportManager.Instance != null && TeleportManager.Instance.IsTraveling)
            return;

        // Sphere trigger can fire on vertical exits (jump near the top face) — trust the XZ check.
        if (IsInside(other.transform.position))
            return;

        wasInside = false;
        BeginRecovery();
    }

    private void BeginRecovery()
    {
        if (isRecovering)
            return;

        if (player == null)
            player = ResolvePlayer();

        if (player == null)
            return;

        Vector3 from = player.position;
        Vector3 spawnPos;
        Quaternion spawnRot;

        if (!TeleportUnlockRegistry.TryGetNearestUnlockedSpawn(from, out spawnPos, out spawnRot))
        {
            if (fallbackSpawn == null)
            {
                Debug.LogWarning("[WorldBoundsZone] Player left bounds but no unlocked teleport / fallback spawn.", this);
                return;
            }

            spawnPos = fallbackSpawn.position;
            spawnRot = fallbackSpawn.rotation;
        }

        isRecovering = true;

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowWarning("Returned to the nearest teleport.");

        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.TravelWithLoadingScreen(spawnPos, spawnRot, FinishRecovery);
        }
        else
        {
            ApplyFallbackWarp(spawnPos, spawnRot);
            FinishRecovery();
        }
    }

    private void FinishRecovery()
    {
        isRecovering = false;
        recoverUntil = Time.unscaledTime + recoveryCooldown;
        wasInside = true;
        nextPollTime = recoverUntil;
    }

    private void ApplyFallbackWarp(Vector3 position, Quaternion rotation)
    {
        if (player == null)
            return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        player.SetPositionAndRotation(position, rotation);

        if (cc != null)
            cc.enabled = true;

        player.SendMessage("ResetVelocity", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("ResetFallDamage", SendMessageOptions.DontRequireReceiver);
    }

    private void TrySoftWarning(Vector3 playerPosition)
    {
        if (softWarningEdgeDistance <= 0f || zoneCollider == null)
            return;

        if (Time.unscaledTime < nextSoftWarningTime)
            return;

        float distToEdge;

        if (TryGetCircle(out Vector3 circleCenter, out float circleRadius))
        {
            float dist = Vector2.Distance(
                new Vector2(playerPosition.x, playerPosition.z),
                new Vector2(circleCenter.x, circleCenter.z));
            distToEdge = circleRadius - dist;
        }
        else
        {
            // Box: probe outward from center through the player to the surface.
            Vector3 center = zoneCollider.bounds.center;
            Vector3 flatPlayer = new Vector3(playerPosition.x, center.y, playerPosition.z);
            Vector3 outward = flatPlayer - center;
            if (outward.sqrMagnitude < 0.01f)
                return;

            Vector3 probe = flatPlayer + outward.normalized * (softWarningEdgeDistance + 5f);
            Vector3 onSurface = zoneCollider.ClosestPoint(probe);
            distToEdge = Vector3.Distance(
                new Vector3(flatPlayer.x, 0f, flatPlayer.z),
                new Vector3(onSurface.x, 0f, onSurface.z));
        }

        if (distToEdge > softWarningEdgeDistance)
            return;

        nextSoftWarningTime = Time.unscaledTime + softWarningCooldown;

        if (NotificationManager.Instance != null && !string.IsNullOrWhiteSpace(softWarningMessage))
            NotificationManager.Instance.ShowWarning(softWarningMessage);
    }

    private bool IsInside(Vector3 worldPosition)
    {
        if (zoneCollider == null)
            return true;

        // Circle mode: XZ distance only, so jumping/gliding height never counts as "outside".
        if (TryGetCircle(out Vector3 circleCenter, out float circleRadius))
        {
            float dx = worldPosition.x - circleCenter.x;
            float dz = worldPosition.z - circleCenter.z;
            return dx * dx + dz * dz <= circleRadius * circleRadius;
        }

        Vector3 closest = zoneCollider.ClosestPoint(worldPosition);
        return (closest - worldPosition).sqrMagnitude < InsideToleranceSqr;
    }

    private static Transform ResolvePlayer()
    {
        if (PlayerRegistry.Instance != null)
            return PlayerRegistry.Instance.transform;

        return SharedInteractPromptUtility.GetPlayerTransform();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider col = zoneCollider != null ? zoneCollider : GetComponent<Collider>();
        if (col == null)
            return;

        if (col is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            Vector3 s = sphere.transform.lossyScale;
            float radius = sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.9f);
            Gizmos.DrawWireSphere(center, radius);
            return;
        }

        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.25f);
        Bounds b = col.bounds;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
