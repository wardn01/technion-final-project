using UnityEngine;

/// <summary>
/// Allows the player to zoom in and out of the map using the mouse scroll wheel.
/// </summary>
public class MapZoom : MonoBehaviour
{
    #region Settings
    [Header("Map Settings")]
    public RectTransform mapContent;
    
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;
    #endregion

    #region Input Handling
    private void Update()
    {
        if (mapContent == null) return;

        float scroll = Input.mouseScrollDelta.y;

        if (scroll != 0)
        {
            Vector3 newScale = mapContent.localScale + Vector3.one * scroll * zoomSpeed;

            newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
            newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
            newScale.z = 1f;

            mapContent.localScale = newScale;
        }
    }
    #endregion
}