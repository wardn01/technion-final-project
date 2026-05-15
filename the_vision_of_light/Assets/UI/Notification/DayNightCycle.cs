using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Time Settings")]
    [Range(0, 24)] public float currentTime = 8f;
    public float timeMultiplier = 60f;

    [Header("Sun & Moon Settings")]
    public Light sunLight;
    public Transform sunMesh; 
    public Transform moonMesh; 
    [Range(0f, 2f)] public float maxSunIntensity = 1.2f;
    [Range(0f, 1f)] public float minSunIntensity = 0f;

    [Header("Sun Colors")]
    public Color sunriseColor = new Color(1f, 0.7f, 0.45f);
    public Color dayColor = Color.white;

    [Header("Ambient Lighting")]
    public Color dayAmbientColor = new Color(0.9f, 0.92f, 1f);
    public Color nightAmbientColor = new Color(0.02f, 0.02f, 0.1f);

    [Header("Fog")]
    public bool useFog = true;
    public Color dayFogColor = new Color(0.78f, 0.88f, 1f);
    public Color nightFogColor = new Color(0.01f, 0.01f, 0.05f);
    [Range(0f, 0.05f)] public float dayFogDensity = 0.0015f;
    [Range(0f, 0.05f)] public float nightFogDensity = 0.004f;

    [Header("Materials")]
    public Material skyDomeMaterial;
    public Material cloudsMaterial;
    public string strengthProperty = "_Strength";

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            currentTime += (Time.deltaTime * timeMultiplier) / 3600f;
            if (currentTime >= 24f) currentTime = 0f;
        }
        UpdateAtmosphere();
    }

    private void OnValidate() { UpdateAtmosphere(); }

    void UpdateAtmosphere()
    {
        if (sunLight == null) return;

        float sunRotation;

        if (currentTime >= 4f && currentTime <= 20f) 
        {
            float dayProgress = (currentTime - 4f) / 16f;
            sunRotation = Mathf.Lerp(0f, 180f, dayProgress);
        } 
        else 
        {
            float nightProgress = (currentTime > 20f) ? (currentTime - 20f) / 8f : (currentTime + 4f) / 8f;
            sunRotation = Mathf.Lerp(180f, 360f, nightProgress);
        }

        sunLight.transform.rotation = Quaternion.Euler(sunRotation, 15f, 0f);

        if (sunMesh != null) sunMesh.LookAt(Vector3.zero);
        if (moonMesh != null) moonMesh.LookAt(Vector3.zero);

        float sunHeight = Vector3.Dot(sunLight.transform.forward, Vector3.down);
        
        float t = Mathf.Clamp01(sunHeight);
        t = Mathf.SmoothStep(0f, 1f, t);

        float worldLightT = Mathf.Lerp(0.65f, 1f, t);

        sunLight.intensity = Mathf.Lerp(minSunIntensity, maxSunIntensity, t);
        
        if (t < 0.35f) {
            float sunriseT = t / 0.35f;
            sunLight.color = Color.Lerp(sunriseColor, dayColor, sunriseT);
        } else {
            sunLight.color = dayColor;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, worldLightT);

        if (useFog) {
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, t);
        }

        if (skyDomeMaterial != null) skyDomeMaterial.SetFloat(strengthProperty, Mathf.Lerp(0.01f, 1f, t));
        if (cloudsMaterial != null) cloudsMaterial.SetFloat(strengthProperty, Mathf.Lerp(0.1f, 2f, t));
    }
}