using UnityEngine;

public class MapZoom : MonoBehaviour
{
    [Header("Map Settings")]
    public RectTransform mapContent;
    
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;

    void Update()
    {
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
}