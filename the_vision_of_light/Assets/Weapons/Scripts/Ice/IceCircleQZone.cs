using UnityEngine;

/// <summary>
/// Ice magic circle spawned by <see cref="IceSwordQSystem"/>.
/// Rises above the player, expands, then drops waves of ice balls from the Snow spawn volume.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class IceCircleQZone : MonoBehaviour
{
    #region Circle Settings
    [Header("Circle Lifetime")]
    public float lifeTime = 6f;

    [Header("Rise Phase")]
    public float riseDuration = 1.4f;
    public float riseHeight = 3.5f;

    [Header("Open Phase")]
    public float expandDuration = 0.8f;
    public float startScale = 0.2f;
    public float endScale = 1f;

    [Header("Ice Ball Waves")]
    [Tooltip("Optional spawn area marker. Auto-finds child named Snow if empty.")]
    public Transform ballSpawnArea;

    public float spawnInterval = 0.85f;
    public int ballsPerWave = 3;

    [Tooltip("Delay between each ice ball in the same wave.")]
    public float ballSpawnDelay = 0.25f;

    public float attackDelayAfterRise = 0.15f;
    #endregion

    #region Audio
    [Header("Audio")]
    [Tooltip("Played on open unless PlayerCombat animation event already played it.")]
    public AudioClip circleOpenClip;
    [Range(0f, 1f)] public float circleOpenVolume = 1f;
    public AudioClip circleLoopClip;
    [Range(0f, 1f)] public float circleLoopVolume = 0.45f;
    #endregion

    #region State
    private IceSwordQSystem owner;
    private BoxCollider spawnVolume;
    private Transform fallbackSpawnPoint;
    private float damage;
    private LayerMask enemyLayer;
    private float spawnTime;
    private float nextWaveTime;
    private float nextBallSpawnTime;
    private int ballsRemainingInWave;
    private Vector3 startPosition;
    private Vector3 baseScale;
    private bool isInitialized;
    private bool canAttack;
    private AudioSource audioSource;
    private bool isLoopPlaying;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (!isInitialized) return;

        float phaseT = GetPhaseProgress();
        UpdateRise(phaseT);
        UpdateExpansion(phaseT);

        if (!canAttack && Time.time >= nextWaveTime)
            canAttack = true;

        if (Time.time >= spawnTime + lifeTime)
        {
            owner?.NotifyCircleFinished(this);
            Cleanup();
            return;
        }

        if (!canAttack || owner == null) return;

        if (ballsRemainingInWave > 0)
        {
            if (Time.time < nextBallSpawnTime) return;

            owner.SpawnIceBall(GetBallSpawnPosition(), damage, enemyLayer);
            ballsRemainingInWave--;

            if (ballsRemainingInWave > 0)
                nextBallSpawnTime = Time.time + ballSpawnDelay;
            else
                nextWaveTime = Time.time + spawnInterval;

            return;
        }

        if (Time.time < nextWaveTime) return;

        ballsRemainingInWave = ballsPerWave;
        nextBallSpawnTime = Time.time;
    }
    #endregion

    #region Public API
    /// <param name="playOpenSound">Set false when cast sound was already triggered by animation event.</param>
    public void Initialize(IceSwordQSystem system, float ballDamage, LayerMask enemies, bool playOpenSound = true)
    {
        owner = system;
        damage = ballDamage;
        enemyLayer = enemies;
        spawnTime = Time.time;
        startPosition = transform.position;
        baseScale = transform.localScale;
        transform.localScale = baseScale * startScale;
        canAttack = false;
        ballsRemainingInWave = 0;
        nextWaveTime = spawnTime + riseDuration + attackDelayAfterRise;
        ResolveSpawnSources();
        isInitialized = true;

        if (playOpenSound)
            PlayCircleOpen();

        StartCircleLoop();
    }

    /// <summary>Resets wave timing when Q is pressed again while the manager is active.</summary>
    public void Refresh(float ballDamage)
    {
        damage = ballDamage;
        spawnTime = Time.time;
        startPosition = transform.position;
        canAttack = false;
        ballsRemainingInWave = 0;
        nextWaveTime = spawnTime + riseDuration + attackDelayAfterRise;
    }

    public void Cleanup()
    {
        StopCircleLoop();
        isInitialized = false;
        owner = null;
        Destroy(gameObject);
    }
    #endregion

    #region Audio
    private void PlayCircleOpen()
    {
        if (circleOpenClip == null) return;
        AudioSource.PlayClipAtPoint(circleOpenClip, transform.position, circleOpenVolume);
    }

    private void StartCircleLoop()
    {
        if (circleLoopClip == null || audioSource == null || isLoopPlaying) return;

        audioSource.clip = circleLoopClip;
        audioSource.volume = circleLoopVolume;
        audioSource.loop = true;
        audioSource.Play();
        isLoopPlaying = true;
    }

    private void StopCircleLoop()
    {
        if (audioSource == null || !isLoopPlaying) return;

        audioSource.loop = false;
        audioSource.Stop();
        isLoopPlaying = false;
    }
    #endregion

    #region Phases
    private float GetPhaseProgress()
    {
        if (riseDuration <= 0f) return 1f;
        return Mathf.Clamp01((Time.time - spawnTime) / riseDuration);
    }

    private void UpdateRise(float phaseT)
    {
        Vector3 pos = startPosition;
        pos.y += riseHeight * phaseT;
        transform.position = pos;
    }

    private void UpdateExpansion(float phaseT)
    {
        float expandT = expandDuration > 0f
            ? Mathf.Clamp01((Time.time - spawnTime) / expandDuration)
            : phaseT;

        float scaleFactor = Mathf.Lerp(startScale, endScale, expandT);
        transform.localScale = baseScale * scaleFactor;
    }
    #endregion

    #region Spawning
    private void ResolveSpawnSources()
    {
        if (ballSpawnArea == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == "Snow")
                {
                    ballSpawnArea = children[i];
                    break;
                }
            }
        }

        if (ballSpawnArea != null)
            spawnVolume = ballSpawnArea.GetComponent<BoxCollider>();

        if (spawnVolume != null)
        {
            spawnVolume.enabled = false;
            return;
        }

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            if (allChildren[i].name == "Portal-Flash2")
            {
                fallbackSpawnPoint = allChildren[i];
                return;
            }
        }
    }

    private Vector3 GetBallSpawnPosition()
    {
        if (spawnVolume != null)
            return GetRandomPointInBox(spawnVolume);

        if (ballSpawnArea != null)
            return ballSpawnArea.position;

        if (fallbackSpawnPoint != null)
            return fallbackSpawnPoint.position;

        return transform.position;
    }

    private static Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Vector3 halfSize = box.size * 0.5f;
        Vector3 localPoint = box.center + new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            Random.Range(-halfSize.y, halfSize.y),
            Random.Range(-halfSize.z, halfSize.z));

        return box.transform.TransformPoint(localPoint);
    }
    #endregion
}
