using UnityEngine;

/// <summary>
/// Handles periodic "Special" idle animations for the character model.
/// Utilizes unscaled time so animations trigger even when the game is paused (e.g., inside UI menus).
/// </summary>
public class IdleRandomizer : MonoBehaviour
{
    #region Components

    /// <summary>Reference to the Animator component attached to the character.</summary>
    private Animator anim;

    #endregion

    #region Variables

    [Header("Settings")]
    /// <summary>The time interval in seconds between special idle animation triggers.</summary>
    public float specialIdleInterval = 10f;
    
    /// <summary>Internal timer tracking the time until the next animation trigger.</summary>
    private float timer;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        anim = GetComponent<Animator>();
        timer = specialIdleInterval;
    }

    private void Update()
    {
        if (anim == null)
            return;

        // Use unscaledDeltaTime to maintain functionality during game pause (Time.timeScale = 0)
        timer -= Time.unscaledDeltaTime;

        if (timer <= 0f)
        {
            // Verify if the animator is currently in the base "Idle" state
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idle"))
            {
                PlaySpecialAnimation();
            }

            // Reset the timer for the next cycle
            timer = specialIdleInterval;
        }
    }

    #endregion

    #region Animation Handling

    /// <summary>
    /// Triggers the animator's "PlaySpecial" parameter.
    /// Expected to transition the character to a unique animation state.
    /// </summary>
    private void PlaySpecialAnimation()
    {
        if (anim != null)
        {
            anim.SetTrigger("PlaySpecial");
        }
    }

    #endregion
}