using UnityEngine;
using VisionOfLight.Player;

namespace VisionOfLight.Enemy
{
    /// <summary>Stone projectile for <see cref="MiniGolem"/>. Destroys itself on the first player hit.</summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class MiniGolemStoneProjectile : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 12f;
        [SerializeField] private float playerHitPadding = 0.08f;
        [SerializeField] private float minAirTimeBeforeDamage = 0.08f;
        [SerializeField] private float groundProbeDistance = 10f;
        [SerializeField] private float groundBounce = 0.22f;
        [SerializeField] private float groundFriction = 0.82f;

        private const float FallbackCapsuleRadius = 0.3f;
        private const float FallbackCapsuleHeight = 2f;

        private float damage;
        private bool hasDamagedPlayer;
        private bool hasLaunched;
        private float launchTime;
        private Vector3 lastCheckPosition;
        private Transform playerTarget;
        private PlayerHealth playerHealth;
        private CharacterController playerController;
        private SphereCollider physicsSphere;
        private Rigidbody rb;
        private EnemyAudioEmitter audioEmitter;
        private int groundMask;

        private void Awake()
        {
            physicsSphere = GetComponent<SphereCollider>();
            rb = GetComponent<Rigidbody>();

            int groundLayer = LayerMask.NameToLayer("Ground");
            groundMask = groundLayer >= 0 ? (1 << groundLayer) : 0;
            groundMask |= 1 << LayerMask.NameToLayer("Default");
        }

        private void Start()
        {
            audioEmitter?.PlayClip("StoneFly");
            Destroy(gameObject, lifeTime);
        }

        /// <summary>Sets impact damage from the thrower's scaled attack.</summary>
        public void SetDamage(float dmgAmount)
        {
            damage = dmgAmount;
        }

        public void BindAudio(EnemyAudioEmitter emitter)
        {
            audioEmitter = emitter;
        }

        /// <summary>Locks the player transform for capsule hit detection.</summary>
        public void SetTarget(Transform target)
        {
            playerTarget = target;
            playerHealth = null;
            playerController = null;

            if (playerTarget == null)
                return;

            playerHealth = playerTarget.GetComponent<PlayerHealth>();
            playerController = playerHealth != null && playerHealth.characterController != null
                ? playerHealth.characterController
                : playerTarget.GetComponent<CharacterController>();
        }

        public void NotifyLaunched()
        {
            hasLaunched = true;
            launchTime = Time.time;
            lastCheckPosition = transform.position;

            if (playerHealth == null)
            {
                playerHealth = PlayerRegistry.Instance?.Health;
                if (playerHealth != null)
                {
                    playerTarget = playerHealth.transform;
                    playerController = playerHealth.characterController != null
                        ? playerHealth.characterController
                        : playerTarget.GetComponent<CharacterController>();
                }
            }

            if (physicsSphere != null)
                physicsSphere.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (!hasLaunched)
                return;

            Vector3 segmentStart = lastCheckPosition;
            KeepOnGround();
            HandlePlayerInteraction(segmentStart, transform.position);
            lastCheckPosition = transform.position;
        }

        private void Update()
        {
            if (!hasLaunched)
                return;

            HandlePlayerInteraction(lastCheckPosition, transform.position);
        }

        private void OnTriggerEnter(Collider other) => TryHandlePlayerCollider(other);

        private void TryHandlePlayerCollider(Collider other)
        {
            if (!hasLaunched || hasDamagedPlayer || Time.time < launchTime + minAirTimeBeforeDamage)
                return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.isDead)
                return;

            playerHealth = health;
            playerTarget = health.transform;
            playerController = health.characterController != null
                ? health.characterController
                : playerTarget.GetComponent<CharacterController>();

            HandlePlayerInteraction(transform.position, transform.position);
        }

        private void HandlePlayerInteraction(Vector3 segmentStart, Vector3 segmentEnd)
        {
            if (hasDamagedPlayer || Time.time < launchTime + minAirTimeBeforeDamage)
                return;

            if (playerHealth == null || playerHealth.isDead || playerTarget == null)
                return;

            if (!TryGetPlayerCapsule(out Vector3 capsuleBottom, out Vector3 capsuleTop, out float playerRadius))
                return;

            bool hitThisFrame = IsHittingPlayer(segmentStart, segmentEnd, capsuleBottom, capsuleTop, playerRadius);
            bool overlappingNow = IsInsidePlayerCapsule(capsuleBottom, capsuleTop, playerRadius);

            if (!hitThisFrame && !overlappingNow)
                return;

            hasDamagedPlayer = true;
            playerHealth.TakeDamage(damage);
            audioEmitter?.PlayClipAt("StoneImpact", transform.position);
            Destroy(gameObject);
        }

        private bool TryGetPlayerCapsule(out Vector3 capsuleBottom, out Vector3 capsuleTop, out float playerRadius)
        {
            capsuleBottom = capsuleTop = Vector3.zero;
            playerRadius = FallbackCapsuleRadius;

            if (playerTarget == null)
                return false;

            if (playerController != null)
            {
                playerRadius = playerController.radius * GetHorizontalScale(playerTarget);
                Vector3 worldCenter = playerTarget.TransformPoint(playerController.center);
                float halfCylinder = Mathf.Max(0f, playerController.height * 0.5f - playerController.radius);
                Vector3 up = playerTarget.up;
                capsuleBottom = worldCenter - up * halfCylinder;
                capsuleTop = worldCenter + up * halfCylinder;
                return true;
            }

            playerRadius = FallbackCapsuleRadius;
            capsuleBottom = playerTarget.position + Vector3.up * playerRadius;
            capsuleTop = playerTarget.position + Vector3.up * (FallbackCapsuleHeight - playerRadius);
            return true;
        }

        private bool IsInsidePlayerCapsule(Vector3 capsuleBottom, Vector3 capsuleTop, float playerRadius)
        {
            float reach = GetWorldRadius() + playerRadius + playerHitPadding;
            return DistanceToCapsule(transform.position, capsuleBottom, capsuleTop, playerRadius) <= reach;
        }

        private bool IsHittingPlayer(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            Vector3 capsuleBottom,
            Vector3 capsuleTop,
            float playerRadius)
        {
            float reach = GetWorldRadius() + playerRadius + playerHitPadding;

            if (DistanceToCapsule(segmentEnd, capsuleBottom, capsuleTop, playerRadius) <= reach)
                return true;

            Vector3 segment = segmentEnd - segmentStart;
            float segmentLength = segment.magnitude;
            if (segmentLength <= 0.001f)
                return false;

            float step = Mathf.Max(0.04f, GetWorldRadius() * 0.25f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(segmentLength / step));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 sample = Vector3.Lerp(segmentStart, segmentEnd, t);
                if (DistanceToCapsule(sample, capsuleBottom, capsuleTop, playerRadius) <= reach)
                    return true;
            }

            return false;
        }

        private static Vector3 ClosestPointOnCapsuleAxis(Vector3 point, Vector3 capsuleBottom, Vector3 capsuleTop)
        {
            Vector3 axis = capsuleTop - capsuleBottom;
            float t = axis.sqrMagnitude > 0.0001f
                ? Mathf.Clamp01(Vector3.Dot(point - capsuleBottom, axis) / axis.sqrMagnitude)
                : 0f;

            return capsuleBottom + axis * t;
        }

        private static float DistanceToCapsule(Vector3 point, Vector3 capsuleBottom, Vector3 capsuleTop, float playerRadius)
        {
            Vector3 closestOnAxis = ClosestPointOnCapsuleAxis(point, capsuleBottom, capsuleTop);
            return Vector3.Distance(point, closestOnAxis) - playerRadius;
        }

        private void KeepOnGround()
        {
            if (rb == null || physicsSphere == null)
                return;

            float radius = GetWorldRadius();
            Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);

            if (!Physics.SphereCast(origin, radius * 0.85f, Vector3.down, out RaycastHit hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
                return;

            if (hit.collider.GetComponentInParent<PlayerHealth>() != null)
                return;

            float minY = hit.point.y + hit.normal.y * radius;
            if (transform.position.y >= minY)
                return;

            transform.position = new Vector3(transform.position.x, minY, transform.position.z);

            Vector3 velocity = rb.linearVelocity;
            if (velocity.y < 0f)
                velocity.y = -velocity.y * groundBounce;
            else
                velocity.y = 0f;

            velocity.x *= groundFriction;
            velocity.z *= groundFriction;
            rb.linearVelocity = velocity;
        }

        private float GetWorldRadius()
        {
            if (physicsSphere == null)
                return 0.1f;

            return physicsSphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        }

        private static float GetHorizontalScale(Transform target)
        {
            return Mathf.Max(target.lossyScale.x, target.lossyScale.z);
        }
    }
}
