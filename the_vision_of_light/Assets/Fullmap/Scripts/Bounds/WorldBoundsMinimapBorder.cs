using UnityEngine;

/// <summary>
/// Minimap-only visuals for the playable area: a red border on the edge and black fog
/// covering everything outside. Supports circular (SphereCollider) or rectangular
/// (BoxCollider) bounds. Everything lives on the Minimap layer so it renders on the
/// minimap camera only, never in normal gameplay.
/// Auto-created by <see cref="WorldBoundsZone"/>; can also be added manually.
/// </summary>
[DisallowMultipleComponent]
public class WorldBoundsMinimapBorder : MonoBehaviour
{
    private const int CircleSegments = 96;

    [Tooltip("Leave empty to use the WorldBoundsZone on this object / parent / the active instance.")]
    public WorldBoundsZone boundsZone;

    [Header("Border Line")]
    [Tooltip("Border color on the minimap.")]
    public Color borderColor = Color.red;

    [Tooltip("Line width in world units (minimap is zoomed out — keep it thick).")]
    [Min(0.1f)]
    public float lineWidth = 6f;

    [Header("Outside Fog")]
    [Tooltip("Darken everything outside the playable area on the minimap.")]
    public bool darkenOutside = true;

    [Tooltip("Fog color outside the bounds. Alpha controls darkness.")]
    public Color outsideColor = new Color(0f, 0f, 0f, 0.8f);

    [Tooltip("How far (world units) the fog extends beyond the bounds on each side.")]
    [Min(100f)]
    public float outsideExtent = 1200f;

    [Tooltip("Fog mask resolution for circular bounds.")]
    public int circleMaskResolution = 1024;

    [Header("Placement")]
    [Tooltip("Height above the bounds center. Must stay above terrain and below the minimap camera.")]
    public float heightOffset = 150f;

    private LineRenderer line;
    private Transform[] fogQuads;
    private Transform circleFogQuad;
    private Texture2D circleMask;
    private int minimapLayer;

    private void Start()
    {
        ResolveBoundsZone();
        minimapLayer = ResolveMinimapLayer();
        gameObject.layer = minimapLayer;

        BuildLine();
        Refresh();
    }

    private void OnDestroy()
    {
        if (circleMask != null)
            Destroy(circleMask);
    }

    private void ResolveBoundsZone()
    {
        if (boundsZone != null)
            return;

        boundsZone = GetComponent<WorldBoundsZone>();
        if (boundsZone == null)
            boundsZone = GetComponentInParent<WorldBoundsZone>();
        if (boundsZone == null)
            boundsZone = WorldBoundsZone.Instance;
    }

    private static int ResolveMinimapLayer()
    {
        int layer = LayerMask.NameToLayer("Minimap");
        return layer >= 0 ? layer : 8; // Matches MapRenderSettings.MinimapLayer.
    }

    private static Material CreateOverlayMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private void BuildLine()
    {
        line = GetComponent<LineRenderer>();
        if (line == null)
            line = gameObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = lineWidth;
        line.numCornerVertices = 0;
        line.numCapVertices = 0;
        // Billboard toward the rendering camera — only the top-down minimap camera
        // renders this layer, so the ribbon always faces it.
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = CreateOverlayMaterial(borderColor);
        line.startColor = borderColor;
        line.endColor = borderColor;
    }

    private void Refresh()
    {
        if (boundsZone == null)
            return;

        if (boundsZone.TryGetCircle(out Vector3 circleCenter, out float circleRadius))
        {
            float y = circleCenter.y + heightOffset;
            BuildCircleLine(circleCenter, circleRadius, y);

            if (darkenOutside)
                BuildCircleFog(circleCenter, circleRadius, y - 2f);
        }
        else
        {
            Bounds b = boundsZone.PlayableBounds;
            float y = b.center.y + heightOffset;
            BuildRectLine(b, y);

            if (darkenOutside)
                BuildRectFog(b, y - 2f);
        }
    }

    private void BuildCircleLine(Vector3 center, float radius, float y)
    {
        line.positionCount = CircleSegments;
        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegments;
            line.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                y,
                center.z + Mathf.Sin(angle) * radius));
        }
    }

    private void BuildCircleFog(Vector3 center, float radius, float y)
    {
        float areaSize = (radius + outsideExtent) * 2f;

        if (circleMask == null)
        {
            circleMask = WorldBoundsMaskUtility.GenerateCircleMask(
                Mathf.Clamp(circleMaskResolution, 256, 4096),
                new Vector2(areaSize, areaSize),
                Vector2.zero,
                radius,
                outsideColor,
                Color.clear,
                0f);
        }

        if (circleFogQuad == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "MinimapFogCircle";
            quad.layer = minimapLayer;
            quad.transform.SetParent(transform, true);

            Collider col = quad.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            Material mat = CreateOverlayMaterial(Color.white);
            mat.mainTexture = circleMask;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            circleFogQuad = quad.transform;
        }

        circleFogQuad.position = new Vector3(center.x, y, center.z);
        circleFogQuad.localScale = new Vector3(areaSize, areaSize, 1f);
    }

    private void BuildRectLine(Bounds b, float y)
    {
        line.positionCount = 4;
        line.SetPosition(0, new Vector3(b.min.x, y, b.min.z));
        line.SetPosition(1, new Vector3(b.max.x, y, b.min.z));
        line.SetPosition(2, new Vector3(b.max.x, y, b.max.z));
        line.SetPosition(3, new Vector3(b.min.x, y, b.max.z));
    }

    private void BuildRectFog(Bounds b, float y)
    {
        if (fogQuads == null)
        {
            Material fogMaterial = CreateOverlayMaterial(outsideColor);
            fogQuads = new Transform[4];

            for (int i = 0; i < 4; i++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "MinimapFog" + i;
                quad.layer = minimapLayer;
                quad.transform.SetParent(transform, true);

                Collider col = quad.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);

                MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = fogMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                fogQuads[i] = quad.transform;
            }
        }

        float e = outsideExtent;
        float sizeX = b.size.x;
        float sizeZ = b.size.z;

        SetQuad(fogQuads[0], new Vector3(b.center.x, y, b.max.z + e * 0.5f), new Vector2(sizeX + e * 2f, e));
        SetQuad(fogQuads[1], new Vector3(b.center.x, y, b.min.z - e * 0.5f), new Vector2(sizeX + e * 2f, e));
        SetQuad(fogQuads[2], new Vector3(b.min.x - e * 0.5f, y, b.center.z), new Vector2(e, sizeZ));
        SetQuad(fogQuads[3], new Vector3(b.max.x + e * 0.5f, y, b.center.z), new Vector2(e, sizeZ));
    }

    private static void SetQuad(Transform quad, Vector3 position, Vector2 size)
    {
        quad.position = position;
        quad.localScale = new Vector3(size.x, size.y, 1f);
    }
}
