using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles zooming mechanics for the interactive map using both mouse scroll wheel and UI slider inputs.
/// Automatically clamps zoom levels to maintain map readability and synchronizes input methods.
/// </summary>
public class MapZoom : MonoBehaviour
{
    #region Settings
    [Header("Settings")]
    public RectTransform mapContent;
    public Slider zoomSlider; 
    
    [Header("Zoom Limits")]
    public float zoomSpeed = 0.5f; 
    public float minZoom = 0.5f;   
    public float maxZoom = 2.5f;   
    #endregion

    private float currentZoom = 1.5f;

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the default zoom level based on the current scale and registers the UI slider event listener.
    /// </summary>
    private void Start()
    {
        if (mapContent != null) currentZoom = mapContent.localScale.x;

        if (zoomSlider != null)
        {
            zoomSlider.minValue = minZoom;
            zoomSlider.maxValue = maxZoom;
            zoomSlider.value = currentZoom;
            
            // Listen for slider changes dynamically
            zoomSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    /// <summary>
    /// Captures mouse scroll input to adjust the zoom level dynamically during gameplay.
    /// </summary>
    private void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed, minZoom, maxZoom);
            ApplyZoom(currentZoom);
        }
    }
    #endregion

    #region Zoom Logic
    /// <summary>
    /// Callback triggered whenever the UI slider value is modified by the player.
    /// </summary>
    /// <param name="value">The new zoom value from the slider.</param>
    private void OnSliderChanged(float value)
    {
        currentZoom = value;
        ApplyZoom(currentZoom);
    }

    /// <summary>
    /// Applies the calculated zoom scale to the map content and synchronizes the slider UI.
    /// </summary>
    /// <param name="zoomLevel">The target scale factor to apply.</param>
    private void ApplyZoom(float zoomLevel)
    {
        if (mapContent == null) return;
        
        // Apply uniform scaling
        mapContent.localScale = new Vector3(zoomLevel, zoomLevel, 1f);
        
        // Update slider value while preventing infinite event loops
        if (zoomSlider != null && zoomSlider.value != zoomLevel)
        {
            zoomSlider.value = zoomLevel;
        }
    }
    #endregion
}