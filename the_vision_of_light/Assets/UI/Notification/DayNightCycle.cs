using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Time Settings")]
    [Range(0, 24)]
    public float currentTime = 8f;

    public float timeMultiplier = 60f;

    [Header("Sun Settings")]
    public Light sunLight;

    [Range(0f, 2f)]
    public float maxSunIntensity = 1.2f;

    [Range(0f, 1f)]
    public float minSunIntensity = 0.22f;

    [Header("Sun Colors")]
    public Color sunriseColor = new Color(1f, 0.7f, 0.45f);
    public Color dayColor = Color.white;

    [Header("Ambient Lighting")]
    public Color dayAmbientColor = new Color(0.9f, 0.92f, 1f);
    public Color nightAmbientColor = new Color(0.22f, 0.24f, 0.32f);

    [Header("Fog")]
    public bool useFog = true;

    public Color dayFogColor = new Color(0.78f, 0.88f, 1f);
    public Color nightFogColor = new Color(0.12f, 0.14f, 0.2f);

    [Range(0f, 0.05f)]
    public float dayFogDensity = 0.0015f;

    [Range(0f, 0.05f)]
    public float nightFogDensity = 0.004f;

    [Header("Sky & Clouds Materials")]
    public Material skyDomeMaterial;
    public Material cloudsMaterial;

    public string strengthProperty = "_Strength";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            currentTime += (Time.deltaTime * timeMultiplier) / 3600f;

            if (currentTime >= 24f)
                currentTime = 0f;
        }

        UpdateAtmosphere();
    }

    private void OnValidate()
    {
        UpdateAtmosphere();
    }

    void UpdateAtmosphere()
    {
        if (sunLight == null)
            return;

        float sunRotation = (currentTime / 24f) * 360f - 90f;

        sunLight.transform.rotation = Quaternion.Euler(sunRotation, 15f, 0f);

        float sunHeight = Vector3.Dot(sunLight.transform.forward, Vector3.down);

        float t = Mathf.Clamp01(sunHeight);
        t = Mathf.SmoothStep(0f, 1f, t);

        float worldLightT = Mathf.Lerp(0.65f, 1f, t);

        sunLight.intensity = Mathf.Lerp(0.45f, maxSunIntensity, t);

        if (t < 0.35f)
        {
            float sunriseT = t / 0.35f;
            sunLight.color = Color.Lerp(sunriseColor, dayColor, sunriseT);
        }
        else
        {
            sunLight.color = dayColor;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;

        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, worldLightT);

        RenderSettings.fog = useFog;

        if (useFog)
        {
            RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, t);

            RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, t);
        }

        float skyStrength = Mathf.Lerp(0.08f, 1f, t);

        if (skyDomeMaterial != null)
        {
            skyDomeMaterial.SetFloat(strengthProperty, skyStrength);
        }

        float cloudStrength = Mathf.Lerp(0.2f, 2f, t);

        if (cloudsMaterial != null)
        {
            cloudsMaterial.SetFloat(strengthProperty, cloudStrength);
        }
    }
}