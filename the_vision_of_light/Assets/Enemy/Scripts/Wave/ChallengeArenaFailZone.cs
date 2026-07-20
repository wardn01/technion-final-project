using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Larger arena trigger around a <see cref="ChallengeStone"/>.
    /// If the player exits while the trial is running, the challenge fails.
    /// Uses both trigger exit and position polling so teleports are detected.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChallengeArenaFailZone : MonoBehaviour
    {
        private const float InsideToleranceSqr = 0.04f;

        [Tooltip("Stone that owns this trial. Auto-finds a parent ChallengeStone if empty.")]
        [SerializeField] private ChallengeStone challengeStone;

        [Tooltip("Seconds after the trial starts before leaving this zone counts as a fail.")]
        [SerializeField] private float failGraceSeconds = 0.35f;

        private Collider zoneCollider;
        private Transform player;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();

            if (challengeStone == null)
                challengeStone = GetComponentInParent<ChallengeStone>();
        }

        private void Start()
        {
            player = SharedInteractPromptUtility.GetPlayerTransform();
        }

        private void Update()
        {
            if (challengeStone == null || !challengeStone.IsChallengeActive || player == null || zoneCollider == null)
                return;

            if (IsPlayerInside(player.position))
                return;

            challengeStone.NotifyPlayerLeftArena(failGraceSeconds);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") || challengeStone == null)
                return;

            if (zoneCollider != null && !zoneCollider.isTrigger)
                return;

            challengeStone.NotifyPlayerLeftArena(failGraceSeconds);
        }

        private bool IsPlayerInside(Vector3 worldPosition)
        {
            Vector3 closest = zoneCollider.ClosestPoint(worldPosition);
            return (closest - worldPosition).sqrMagnitude < InsideToleranceSqr;
        }
    }
}
