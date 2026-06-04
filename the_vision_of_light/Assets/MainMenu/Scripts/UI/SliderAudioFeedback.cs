using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Provides audio feedback for UI sliders, including continuous tick sounds during movement and a release sound.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderAudioFeedback : MonoBehaviour, IPointerUpHandler
{
    #region Audio Settings
    [Header("Audio Settings")]
    public AudioClip tickSound;
    public AudioClip releaseSound;
    
    [Range(0f, 1f)] 
    public float volume = 0.5f;
    
    [Tooltip("Minimum time between tick sounds to prevent audio spam.")]
    public float tickCooldown = 0.08f;
    #endregion

    #region State & References
    private AudioSource audioSource;
    private Slider slider;
    private float lastTickTime;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes references and subscribes to slider events.
    /// </summary>
    private void Start()
    {
        // Find the global UI audio player
        GameObject globalAudio = GameObject.Find("UIAudioPlayer");
        if (globalAudio != null)
        {
            audioSource = globalAudio.GetComponent<AudioSource>();
        }

        // Get and setup the slider component
        slider = GetComponent<Slider>();
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderMoved);
        }
    }

    /// <summary>
    /// Unsubscribes from events to prevent memory leaks when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderMoved);
        }
    }
    #endregion

    #region Audio Logic
    /// <summary>
    /// Plays a tick sound when the slider value changes, respecting the cooldown timer.
    /// Uses unscaledTime so UI sounds work even if the game is paused.
    /// </summary>
    /// <param name="value">The new slider value (unused but required by the listener).</param>
    private void OnSliderMoved(float value)
    {
        if (tickSound != null && audioSource != null && Time.unscaledTime - lastTickTime > tickCooldown)
        {
            audioSource.PlayOneShot(tickSound, volume);
            lastTickTime = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Plays a specific sound when the player releases the slider handle.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (releaseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(releaseSound, volume);
        }
    }
    #endregion
}