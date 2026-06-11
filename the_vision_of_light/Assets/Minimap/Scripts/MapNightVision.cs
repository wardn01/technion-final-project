using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Forces bright overhead sun lighting while the minimap or full-map camera renders,
/// so map views stay readable at night.
/// </summary>
public class MapNightVision : MonoBehaviour
{
    private Quaternion savedSunRotation;
    private float savedSunIntensity;
    private Color savedSunColor;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
    }

    private static bool IsMapRenderCamera(Camera cam)
    {
        if (cam == null) return false;
        if (cam == MinimapFollow.RenderCamera) return true;
        if (FullMapController.FullMapRenderCamera != null && cam == FullMapController.FullMapRenderCamera)
            return true;

        return false;
    }

    private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        if (!IsMapRenderCamera(cam)
            || DayNightCycle.Instance == null
            || DayNightCycle.Instance.sunLight == null)
            return;

        Light sun = DayNightCycle.Instance.sunLight;

        savedSunRotation = sun.transform.rotation;
        savedSunIntensity = sun.intensity;
        savedSunColor = sun.color;

        sun.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        sun.intensity = 2f;
        sun.color = Color.white;
    }

    private void OnEndCamera(ScriptableRenderContext context, Camera cam)
    {
        if (!IsMapRenderCamera(cam)
            || DayNightCycle.Instance == null
            || DayNightCycle.Instance.sunLight == null)
            return;

        Light sun = DayNightCycle.Instance.sunLight;
        sun.transform.rotation = savedSunRotation;
        sun.intensity = savedSunIntensity;
        sun.color = savedSunColor;
    }
}
