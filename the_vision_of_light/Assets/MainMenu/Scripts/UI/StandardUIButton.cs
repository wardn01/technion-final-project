using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Provides standard audio and color feedback for general UI buttons on hover and click events.
/// </summary>
public class StandardUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    #region Audio Settings
    [Header("Audio Effects")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    
    [Range(0f, 1f)] 
    public float hoverVolume = 0.25f;
    
    [Range(0f, 1f)] 
    public float clickVolume = 1f;
    #endregion

    #region UI References & Colors
    [Header("Hover Colors (Optional)")]
    public TextMeshProUGUI buttonText;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.8f, 0f);
    #endregion

    #region State Variables
    private AudioSource audioSource;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the audio source by finding the global UI audio player and sets the default text color.
    /// </summary>
    private void Start()
    {
        GameObject globalAudio = GameObject.Find("UIAudioPlayer");
        if (globalAudio != null)
        {
            audioSource = globalAudio.GetComponent<AudioSource>();
        }

        if (buttonText != null) 
        {
            buttonText.color = normalColor;
        }
    }
    #endregion

    #region Pointer Events
    /// <summary>
    /// Triggered when the pointer enters the button area. Plays hover sound and changes text color.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null && audioSource != null) 
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
        
        if (buttonText != null) 
        {
            buttonText.color = hoverColor;
        }
    }

    /// <summary>
    /// Triggered when the pointer exits the button area. Reverts the text color to normal.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null) 
        {
            buttonText.color = normalColor;
        }
    }

    /// <summary>
    /// Triggered when the button is clicked. Plays the click sound effect.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null && audioSource != null) 
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }
    #endregion
}