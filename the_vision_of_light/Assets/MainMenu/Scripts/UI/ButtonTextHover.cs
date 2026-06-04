using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handles interactive UI button effects including color tints, audio feedback, and icon animations on hover.
/// </summary>
public class MainMenuButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum ButtonType { Play, Settings, Exit, None }
    
    #region Configuration
    [Header("Select Button Type")]
    public ButtonType buttonType;

    [Header("Text Colors")]
    public Color normalTextColor = Color.white;
    public Color hoverTextColor = new Color(1f, 0.8f, 0f);

    [Header("Icon Colors")]
    public Color normalIconColor = Color.white;
    public Color hoverIconColor = new Color(0.5f, 0.8f, 1f);

    [Header("Animation Settings")]
    public float animationSpeed = 5f;
    public float animationAmount = 15f; 
    #endregion

    #region UI References
    [Header("UI References")]
    public TextMeshProUGUI buttonText;
    public Image buttonIcon;
    #endregion

    #region Audio Settings
    [Header("Audio Effects")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    [Range(0f, 1f)] public float hoverVolume = 0.3f;
    [Range(0f, 1f)] public float clickVolume = 1f;
    #endregion

    #region State Variables
    private AudioSource audioSource;
    private bool isHovered = false;
    private Quaternion originalIconRot;
    private Vector3 originalIconPos;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the audio source and caches the original transform values for animations.
    /// </summary>
    private void Start()
    {
        // Find the global UI audio player
        GameObject globalAudio = GameObject.Find("UIAudioPlayer");
        if (globalAudio != null)
        {
            audioSource = globalAudio.GetComponent<AudioSource>();
        }

        if (buttonText != null) buttonText.color = normalTextColor;
        
        if (buttonIcon != null)
        {
            buttonIcon.color = normalIconColor;
            originalIconRot = buttonIcon.rectTransform.localRotation;
            originalIconPos = buttonIcon.rectTransform.localPosition;
        }
    }

    /// <summary>
    /// Applies continuous mathematical animations to the button icon while hovered.
    /// </summary>
    private void Update()
    {
        if (isHovered && buttonIcon != null)
        {
            switch (buttonType)
            {
                case ButtonType.Play:
                    float zRot = Mathf.Sin(Time.time * animationSpeed * 2f) * animationAmount;
                    buttonIcon.rectTransform.localRotation = originalIconRot * Quaternion.Euler(0, 0, zRot);
                    break;

                case ButtonType.Settings:
                    buttonIcon.rectTransform.Rotate(0, 0, -animationSpeed * 30f * Time.deltaTime);
                    break;

                case ButtonType.Exit:
                    float xOffset = Mathf.Sin(Time.time * animationSpeed * 2f) * (animationAmount * 0.5f);
                    buttonIcon.rectTransform.localPosition = originalIconPos + new Vector3(xOffset, 0, 0);
                    break;
            }
        }
    }
    #endregion

    #region Pointer Events
    /// <summary>
    /// Triggered when the mouse enters the button area. Applies hover colors and plays sound.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (buttonText != null) buttonText.color = hoverTextColor;
        if (buttonIcon != null) buttonIcon.color = hoverIconColor;

        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    /// <summary>
    /// Triggered when the mouse leaves the button area. Resets colors, positions, and rotations.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (buttonText != null) buttonText.color = normalTextColor;
        
        if (buttonIcon != null)
        {
            buttonIcon.color = normalIconColor;
            buttonIcon.rectTransform.localRotation = originalIconRot;
            buttonIcon.rectTransform.localPosition = originalIconPos;
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