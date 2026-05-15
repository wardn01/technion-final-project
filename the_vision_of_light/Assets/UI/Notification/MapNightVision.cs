using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class MapNightVision : MonoBehaviour
{
    private Quaternion savedSunRotation;
    private float savedSunIntensity;
    private Color savedSunColor;

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
    }

    void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        if ((cam.gameObject.name == "FullMapCamera" || cam.gameObject.name == "MinimapCamera") 
            && DayNightCycle.Instance != null 
            && DayNightCycle.Instance.sunLight != null)
        {
            Light sun = DayNightCycle.Instance.sunLight;
            
            savedSunRotation = sun.transform.rotation;
            savedSunIntensity = sun.intensity;
            savedSunColor = sun.color;

            sun.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            sun.intensity = 2f; 
            sun.color = Color.white;
        }
    }

    void OnEndCamera(ScriptableRenderContext context, Camera cam)
    {
        if ((cam.gameObject.name == "FullMapCamera" || cam.gameObject.name == "MinimapCamera") 
            && DayNightCycle.Instance != null 
            && DayNightCycle.Instance.sunLight != null)
        {
            Light sun = DayNightCycle.Instance.sunLight;
                        sun.transform.rotation = savedSunRotation;
            sun.intensity = savedSunIntensity;
            sun.color = savedSunColor;
        }
    }
}