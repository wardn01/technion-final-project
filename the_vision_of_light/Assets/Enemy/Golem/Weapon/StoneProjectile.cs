using UnityEngine;

/// <summary>
/// Stone projectile for <see cref="Golem"/>. Damages once on hit, sticks to the ground, then slowly sinks in.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class StoneProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 12f;
    [SerializeField] private float playerHitPadding = 0.08f;
    [SerializeField] private float minAirTimeBeforeDamage = 0.08f;
    [SerializeField] private float groundRayHeight = 25f;
    [SerializeField] private float groundStickDistance = 0.2f;
    [SerializeField] private float settleSpeed = 1.1f;
    [SerializeField] private float autoSettleDelay = 0.25f;
    [SerializeField] private float sinkSpeed = 0.07f;
    [SerializeField] private float maxSinkDepth = 0.38f;

    private const float FallbackCapsuleRadius = 0.3f;
    private const float FallbackCapsuleHeight = 2f;

    private float damage;
    private bool hasDamagedPlayer;
    private bool isAtRest;
    private bool hasLaunched;
    private float launchTime;
    private float lowSpeedTimer;
    private float sinkDepth;
    private Vector3 lastCheckPosition;
    private Vector3 restPosition;
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

    public void SetDamage(float dmgAmount)
    {
        damage = dmgAmount;
    }

    public void BindAudio(EnemyAudioEmitter emitter)
    {
        audioEmitter = emitter;
    }

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
        lowSpeedTimer = 0f;
        sinkDepth = 0f;
        isAtRest = false;
        hasDamagedPlayer = false;

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerTarget = playerHealth.transform;
                playerController = playerHealth.characterController != null
                    ? playerHealth.characterController
                    : playerTarget.GetComponent<CharacterController>();
            }
        }

        SetFlyingPhysics();
    }

    private void FixedUpdate()
    {
        if (!hasLaunched)
            return;

        if (isAtRest)
        {
            UpdateRestingStone();
            lastCheckPosition = transform.position;
            return;
        }

        Vector3 segmentStart = lastCheckPosition;
        HandlePlayerHit(segmentStart, transform.position);
        TrackAutoSettle();
        lastCheckPosition = transform.position;
    }

    private void TryHandlePlayerCollider(Collider other)
    {
        if (!hasLaunched || isAtRest || Time.time < launchTime + minAirTimeBeforeDamage)
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.isDead)
            return;

        playerHealth = health;
        playerTarget = health.transform;
        playerController = health.characterController != null
            ? health.characterController
            : playerTarget.GetComponent<CharacterController>();

        HandlePlayerHit(lastCheckPosition, transform.position);
    }

    private void HandlePlayerHit(Vector3 segmentStart, Vector3 segmentEnd)
    {
        if (isAtRest || hasDamagedPlayer || Time.time < launchTime + minAirTimeBeforeDamage)
            return;

        if (playerHealth == null || playerHealth.isDead || playerTarget == null)
            return;

        if (!TryGetPlayerCapsule(out Vector3 capsuleBottom, out Vector3 capsuleTop, out float playerRadius))
            return;

        if (!IsHittingPlayer(segmentStart, segmentEnd, capsuleBottom, capsuleTop, playerRadius))
            return;

        SeparateFromPlayerHorizontally(capsuleBottom, capsuleTop, playerRadius);
        hasDamagedPlayer = true;
        playerHealth.TakeDamage(damage);
        audioEmitter?.PlayClipAt("StoneImpact", transform.position);
        SettleStone();
    }

    private void TrackAutoSettle()
    {
        if (rb == null)
            return;

        float speed = rb.linearVelocity.magnitude;
        bool nearGround = IsNearGround();

        if (nearGround && speed <= settleSpeed)
        {
            SettleStone();
            return;
        }

        if (speed <= settleSpeed * 0.4f)
            lowSpeedTimer += Time.fixedDeltaTime;
        else
            lowSpeedTimer = 0f;

        if (lowSpeedTimer >= autoSettleDelay)
            SettleStone();
    }

    private bool IsNearGround()
    {
        if (!TryFindGroundPosition(transform.position.x, transform.position.z, out Vector3 groundedPosition))
            return false;

        return transform.position.y - groundedPosition.y <= GetWorldRadius() + groundStickDistance;
    }

    private void SetFlyingPhysics()
    {
        if (physicsSphere != null)
        {
            physicsSphere.enabled = true;
            physicsSphere.isTrigger = false;
        }

        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;
    }

    private void SettleStone()
    {
        if (isAtRest)
            return;

        isAtRest = true;
        lowSpeedTimer = 0f;
        sinkDepth = 0f;

        if (physicsSphere != null)
        {
            physicsSphere.enabled = true;
            physicsSphere.isTrigger = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (TryFindGroundPosition(transform.position.x, transform.position.z, out Vector3 groundedPosition))
            restPosition = groundedPosition;
        else
            restPosition = transform.position;

        ApplyRestTransform();
    }

    private void UpdateRestingStone()
    {
        sinkDepth = Mathf.Min(maxSinkDepth, sinkDepth + sinkSpeed * Time.fixedDeltaTime);
        ApplyRestTransform();

        if (sinkDepth >= maxSinkDepth && physicsSphere != null && physicsSphere.enabled)
            physicsSphere.enabled = false;
    }

    private void ApplyRestTransform()
    {
        Vector3 pos = restPosition;
        pos.y -= sinkDepth;

        transform.position = pos;

        if (rb != null)
            rb.position = pos;
    }

    private bool TryFindGroundPosition(float worldX, float worldZ, out Vector3 groundedPosition)
    {
        groundedPosition = transform.position;
        float radius = GetWorldRadius();
        Vector3 origin = new Vector3(worldX, groundRayHeight, worldZ);

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            groundRayHeight + 50f,
            groundMask,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.collider.GetComponentInParent<PlayerHealth>() != null)
                continue;

            groundedPosition = new Vector3(worldX, hit.point.y + radius, worldZ);
            return true;
        }

        return false;
    }

    private void SeparateFromPlayerHorizontally(Vector3 capsuleBottom, Vector3 capsuleTop, float playerRadius)
    {
        Vector3 closestOnAxis = ClosestPointOnCapsuleAxis(transform.position, capsuleBottom, capsuleTop);
        Vector3 offset = transform.position - closestOnAxis;
        offset.y = 0f;

        float dist = offset.magnitude;
        float minDist = playerRadius + GetWorldRadius() + playerHitPadding;
        Vector3 pushDir = dist > 0.001f ? offset / dist : GetHorizontalAwayFromPlayer();

        Vector3 separated = closestOnAxis + pushDir * minDist;
        transform.position = new Vector3(separated.x, transform.position.y, separated.z);

        if (rb != null)
            rb.position = transform.position;
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

    private Vector3 GetHorizontalAwayFromPlayer()
    {
        Vector3 away = transform.position - playerTarget.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f)
            away = playerTarget.forward;

        return away.normalized;
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
