using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-map playable-area overlay: everything OUTSIDE the playable bounds is darkened,
/// the inside stays clear, and a colored border traces the edge.
/// Supports a circular area (SphereCollider — drawn via a generated mask texture)
/// or a rectangular one (BoxCollider — drawn via fog + border strips).
/// </summary>
[DisallowMultipleComponent]
public class WorldBoundsMapOverlay : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("Leave empty to use WorldBoundsZone.Instance / FullMapController.Instance.")]
    public WorldBoundsZone boundsZone;
    public FullMapController mapController;

    [Header("Look")]
    [Tooltip("Color of the area OUTSIDE the playable bounds (fog). Alpha controls darkness.")]
    public Color outsideColor = new Color(0f, 0f, 0f, 0.85f);

    [Tooltip("Border color traced along the playable-area edge.")]
    public Color borderColor = Color.red;

    [Tooltip("Border thickness in map UI units.")]
    [Range(1f, 20f)]
    public float borderThickness = 4f;

    [Header("Circle Quality")]
    [Tooltip("Mask texture resolution for circular bounds.")]
    public int circleMaskResolution = 1024;

    private RectTransform overlayRect;

    // Circle mode
    private Image circleImage;
    private Texture2D circleMask;

    // Rect mode
    private readonly RectTransform[] fog = new RectTransform[4];
    private readonly Image[] fogImages = new Image[4];
    private readonly RectTransform[] edges = new RectTransform[4];
    private readonly Image[] edgeImages = new Image[4];
    private bool rectPartsBuilt;

    private void LateUpdate()
    {
        if (mapController == null)
            mapController = FullMapController.Instance;

        if (boundsZone == null)
            boundsZone = WorldBoundsZone.Instance;

        if (mapController == null || boundsZone == null || mapController.mapContent == null)
            return;

        if (mapController.fullMapScreen == null || !mapController.fullMapScreen.activeInHierarchy)
            return;

        if (mapController.fullMapCamera == null)
            return;

        EnsureRoot();

        if (boundsZone.TryGetCircle(out Vector3 circleCenter, out float circleRadius))
            RefreshCircle(circleCenter, circleRadius);
        else
            RefreshRect();
    }

    private void OnDestroy()
    {
        if (circleMask != null)
            Destroy(circleMask);
    }

    private void EnsureRoot()
    {
        if (overlayRect != null)
            return;

        Transform existing = mapController.mapContent.Find("WorldBoundsOverlay");
        if (existing != null)
            Destroy(existing.gameObject);

        overlayRect = CreateRect("WorldBoundsOverlay", mapController.mapContent);
        overlayRect.SetAsFirstSibling();
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    #region Circle mode
    private void RefreshCircle(Vector3 worldCenter, float worldRadius)
    {
        Rect mapRect = mapController.mapContent.rect;

        if (circleImage == null)
        {
            circleImage = overlayRect.gameObject.GetComponent<Image>();
            if (circleImage == null)
                circleImage = overlayRect.gameObject.AddComponent<Image>();
            circleImage.raycastTarget = false;
        }

        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = new Vector2(mapRect.width, mapRect.height);

        if (circleMask == null)
        {
            Vector2 uiCenter = mapController.WorldToMapAnchoredPosition(worldCenter);
            float uiRadius = worldRadius * (mapRect.width / mapController.worldSize);

            circleMask = WorldBoundsMaskUtility.GenerateCircleMask(
                Mathf.Clamp(circleMaskResolution, 256, 4096),
                new Vector2(mapRect.width, mapRect.height),
                uiCenter,
                uiRadius,
                outsideColor,
                borderColor,
                borderThickness);

            Sprite sprite = Sprite.Create(
                circleMask,
                new Rect(0f, 0f, circleMask.width, circleMask.height),
                new Vector2(0.5f, 0.5f),
                100f);

            circleImage.sprite = sprite;
            circleImage.color = Color.white;
        }

        overlayRect.SetAsFirstSibling();
    }
    #endregion

    #region Rect mode
    private void EnsureRectParts()
    {
        if (rectPartsBuilt)
            return;

        for (int i = 0; i < 4; i++)
        {
            fog[i] = CreateRect("Fog" + i, overlayRect);
            fogImages[i] = fog[i].gameObject.AddComponent<Image>();
            fogImages[i].raycastTarget = false;
            fogImages[i].color = outsideColor;
        }

        for (int i = 0; i < 4; i++)
        {
            edges[i] = CreateRect("Edge" + i, overlayRect);
            edgeImages[i] = edges[i].gameObject.AddComponent<Image>();
            edgeImages[i].raycastTarget = false;
            edgeImages[i].color = borderColor;
        }

        rectPartsBuilt = true;
    }

    private void RefreshRect()
    {
        EnsureRectParts();

        Bounds b = boundsZone.PlayableBounds;

        Vector2 uiMin = mapController.WorldToMapAnchoredPosition(new Vector3(b.min.x, b.center.y, b.min.z));
        Vector2 uiMax = mapController.WorldToMapAnchoredPosition(new Vector3(b.max.x, b.center.y, b.max.z));

        float left = Mathf.Min(uiMin.x, uiMax.x);
        float right = Mathf.Max(uiMin.x, uiMax.x);
        float bottom = Mathf.Min(uiMin.y, uiMax.y);
        float top = Mathf.Max(uiMin.y, uiMax.y);

        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;

        Rect mapRect = mapController.mapContent.rect;
        float mapHalfW = mapRect.width * 0.5f;
        float mapHalfH = mapRect.height * 0.5f;

        left = Mathf.Clamp(left, -mapHalfW, mapHalfW);
        right = Mathf.Clamp(right, -mapHalfW, mapHalfW);
        bottom = Mathf.Clamp(bottom, -mapHalfH, mapHalfH);
        top = Mathf.Clamp(top, -mapHalfH, mapHalfH);

        SetRect(fog[0],
            new Vector2(0f, (top + mapHalfH) * 0.5f),
            new Vector2(mapRect.width, Mathf.Max(0f, mapHalfH - top)));

        SetRect(fog[1],
            new Vector2(0f, (bottom - mapHalfH) * 0.5f),
            new Vector2(mapRect.width, Mathf.Max(0f, bottom + mapHalfH)));

        SetRect(fog[2],
            new Vector2((left - mapHalfW) * 0.5f, (top + bottom) * 0.5f),
            new Vector2(Mathf.Max(0f, left + mapHalfW), Mathf.Max(0f, top - bottom)));

        SetRect(fog[3],
            new Vector2((right + mapHalfW) * 0.5f, (top + bottom) * 0.5f),
            new Vector2(Mathf.Max(0f, mapHalfW - right), Mathf.Max(0f, top - bottom)));

        for (int i = 0; i < 4; i++)
            fogImages[i].color = outsideColor;

        float t = borderThickness;
        float cx = (left + right) * 0.5f;
        float cy = (top + bottom) * 0.5f;
        float w = right - left;
        float h = top - bottom;

        SetRect(edges[0], new Vector2(cx, top - t * 0.5f), new Vector2(w, t));
        SetRect(edges[1], new Vector2(cx, bottom + t * 0.5f), new Vector2(w, t));
        SetRect(edges[2], new Vector2(left + t * 0.5f, cy), new Vector2(t, h));
        SetRect(edges[3], new Vector2(right - t * 0.5f, cy), new Vector2(t, h));

        for (int i = 0; i < 4; i++)
            edgeImages[i].color = borderColor;

        overlayRect.SetAsFirstSibling();
    }

    private static void SetRect(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }
    #endregion
}
