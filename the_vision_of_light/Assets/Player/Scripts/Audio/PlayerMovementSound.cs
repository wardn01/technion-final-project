using UnityEngine;
using UnityEngine.Serialization;

namespace VisionOfLight.Player
{
    /// <summary>
    /// Animation-event-driven movement sounds — footsteps, jump, glider, swimming, landing, and wind loops.
    /// </summary>
    public class PlayerMovementSound : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Components")]
        public AudioSource audioSource;
        public AudioSource windAudioSource;
        public Animator animator;

        [Header("Sound Clips")]
        public AudioClip[] footstepSounds;
        public AudioClip[] jumpSound;
        public AudioClip[] openGliderSound;
        public AudioClip[] swimmingSounds;
        public AudioClip[] landingSound;
        public AudioClip windLoopSound;

        [Header("Sound Volume Settings")]
        public float FastRunVolume = 0.7f;
        public float runVolume = 0.4f;
        public float jumpVolume = 0.1f;
        public float openGliderVolume = 0.7f;
        public float swimmingVolume = 0.4f;
        [FormerlySerializedAs("idelSwimmingVolume")]
        public float idleSwimmingVolume = 0.1f;
        public float landingVolume = 0.2f;
        public float glideWindVolume = 0.1f; 
        public float fallWindVolume = 0.3f;
        #endregion

        #region Runtime State
        private float targetWindVolume = 0f;
        private float windFadeSpeed = 2f;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (windAudioSource == null) windAudioSource = GetComponent<AudioSource>();

            AudioMixerHub.Route(audioSource, AudioMixerHub.Bus.SFX);
            AudioMixerHub.Route(windAudioSource, AudioMixerHub.Bus.SFX);

            if (windAudioSource != null)
            {
                windAudioSource.loop = true;
                windAudioSource.playOnAwake = false;
            }
        }

        void Update()
        {
            UpdateWindVolume();
        }
        #endregion

        #region Wind
        private void UpdateWindVolume()
        {
            if (windAudioSource == null) return;

            if (windAudioSource.isPlaying)
            {
                windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, targetWindVolume, windFadeSpeed * Time.deltaTime);

                if (windAudioSource.volume < 0.01f && targetWindVolume < 0.01f)
                {
                    windAudioSource.Stop();
                    windAudioSource.volume = 0f;
                }
            }
        }

        /// <summary>Starts or adjusts the wind loop — gliding uses <see cref="glideWindVolume"/>, falling uses <see cref="fallWindVolume"/>.</summary>
        public void PlayWindSound(bool isGlidingState)
        {
            if (windAudioSource == null || windLoopSound == null) return;

            windAudioSource.clip = windLoopSound;

            targetWindVolume = isGlidingState ? glideWindVolume : fallWindVolume;

            if (!windAudioSource.isPlaying)
            {
                windAudioSource.volume = 0f;
                windAudioSource.Play();
            }
        }

        /// <summary>Fades out the wind loop.</summary>
        public void StopWindSound()
        {
            targetWindVolume = 0f;
        }
        #endregion

        #region Animation Events
        /// <summary>Plays a footstep clip scaled by movement speed and combat stance.</summary>
        public void PlayFootstepSound()
        {
            if (audioSource == null || footstepSounds == null || footstepSounds.Length == 0) return;

            PlayerCombat combat = GetComponentInParent<PlayerCombat>();
            float speed = animator.GetFloat("Speed");

            if (combat != null && combat.inCombatStance)
            {
                int randomIndex = Random.Range(0, footstepSounds.Length);
                audioSource.pitch = Random.Range(0.75f, 0.95f);
                audioSource.PlayOneShot(footstepSounds[randomIndex], runVolume);
                return;
            }

            if (speed < 0.1f) return;

            int normalIndex = Random.Range(0, footstepSounds.Length);
            float volume = 0f;
            float pitch = 1f;

            if (speed > 6.5f)
            {
                volume = FastRunVolume;
                pitch = Random.Range(0.9f, 1.1f);
            }
            else
            {
                volume = runVolume;
                pitch = Random.Range(0.8f, 0.95f);
            }

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(footstepSounds[normalIndex], volume);
        }

        /// <summary>Plays a random jump clip.</summary>
        public void PlayJumpSound()
        {
            if (audioSource == null || jumpSound == null || jumpSound.Length == 0) return;

            int randomIndex = Random.Range(0, jumpSound.Length);
            audioSource.pitch = Random.Range(1f, 1.2f);
            audioSource.PlayOneShot(jumpSound[randomIndex], jumpVolume);
        }

        /// <summary>Plays a random glider open/close clip.</summary>
        public void PlayOpenGliderSound()
        {
            if (audioSource == null || openGliderSound == null || openGliderSound.Length == 0) return;

            int randomIndex = Random.Range(0, openGliderSound.Length);
            audioSource.pitch = Random.Range(0.85f, 1.1f);
            audioSource.PlayOneShot(openGliderSound[randomIndex], openGliderVolume);
        }

        /// <summary>Plays a swimming stroke clip scaled by swim speed.</summary>
        public void PlaySwimmingSound()
        {
            if (audioSource == null || swimmingSounds == null || swimmingSounds.Length == 0) return;

            float currentSpeed = animator != null ? animator.GetFloat("Speed") : 0f;
            int randomIndex = Random.Range(0, swimmingSounds.Length);

            audioSource.pitch = currentSpeed > 3f ? Random.Range(0.85f, 1.15f) : Random.Range(0.5f, 0.7f);
            
            float volume = currentSpeed > 3f ? swimmingVolume : idleSwimmingVolume;

            audioSource.PlayOneShot(swimmingSounds[randomIndex], volume);
        }

        /// <summary>Plays a normal landing clip with higher pitch.</summary>
        public void PlayLandingSound()
        {
            if (audioSource == null || landingSound == null || landingSound.Length == 0) return;

            int randomIndex = Random.Range(0, landingSound.Length);
            audioSource.pitch = Random.Range(1.2f, 1.5f);
            audioSource.PlayOneShot(landingSound[randomIndex], landingVolume);
        }

        /// <summary>Plays a hard landing clip with lower pitch.</summary>
        public void PlayHardLandingSound()
        {
            if (audioSource == null || landingSound == null || landingSound.Length == 0) return;

            int randomIndex = Random.Range(0, landingSound.Length);
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(landingSound[randomIndex], landingVolume);
        }
        #endregion
    }
}
