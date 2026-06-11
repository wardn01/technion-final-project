using UnityEngine;

/// <summary>
/// Separate culling rules for minimap vs full-map render cameras.
/// Minimap shows world-space Minimap-layer sprites; full map uses its own UI icons instead.
/// </summary>
public static class MapRenderSettings
{
    private const int FullMapSceneMask = 130743;
    private const int MinimapLayer = 1 << 8;
    private const int EnemyLayer = 1 << 15;
    private const int PlayerBodyLayer = 1 << 13;

    /// <summary>Terrain + player arrow; no world-space minimap sprites (full map uses UI icons).</summary>
    public static int FullMapCullingMask =>
        FullMapSceneMask & ~EnemyLayer & ~PlayerBodyLayer;

    /// <summary>Same as full map plus Minimap layer for NPC/teleport world icons.</summary>
    public static int MinimapCullingMask =>
        (FullMapSceneMask | MinimapLayer) & ~EnemyLayer & ~PlayerBodyLayer;

    public static void ApplyToMinimap(Camera camera)
    {
        if (camera != null)
            camera.cullingMask = MinimapCullingMask;
    }

    public static void ApplyToFullMap(Camera camera)
    {
        if (camera != null)
            camera.cullingMask = FullMapCullingMask;
    }

    /// <summary>Moves PlayerMapIcon to PlayerIcon layer so it stays visible when Player layer is culled.</summary>
    public static void EnsurePlayerMapIconLayer(Transform player)
    {
        if (player == null) return;

        Transform icon = player.Find("PlayerMapIcon");
        if (icon == null) return;

        int playerIconLayer = LayerMask.NameToLayer("PlayerIcon");
        if (playerIconLayer >= 0)
            icon.gameObject.layer = playerIconLayer;
    }
}
