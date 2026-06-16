using System.Collections.Generic;
using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Fire Sword [Q] — spawns orbiting fireballs behind the player. While enemies are nearby,
/// one orb auto-launches on an interval. Pressing Q again refreshes all three orbs.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FireSwordQOrbitSystem : MonoBehaviour
{
    #region Orbit Settings
    [Header("Orbit VFX")]
    public GameObject orbVfxPrefab;

    [Header("Orbit Layout")]
    public int orbCount = 3;
    public float orbScale = 0.3f;
    public float backOffset = 0.55f;
    public float orbitRadius = 0.45f;
    public float orbitHeight = 1.35f;
    public float orbitSpeed = 140f;

    [Tooltip("Tilts the orbit plane around the player. -90 = vertical circle flat on the back.")]
    public float orbitPlaneYaw = -90f;
    #endregion

    #region Launch Settings
    [Header("Launch")]
    public float autoFireInterval = 4f;
    public float launchSpeed = 20f;
    public float maxTargetRange = 22f;
    public float hitDistance = 1.1f;
    public float projectileLifeTime = 4f;

    [Header("Projectile Growth")]
    public float launchScaleMultiplier = 2.5f;
    public float launchGrowDuration = 1.2f;
    #endregion

    #region Audio
    [Header("Audio — Orbs Orbiting")]
    public AudioClip orbitLoopClip;
    [Range(0f, 1f)] public float orbitLoopVolume = 0.45f;

    [Header("Audio — Orb Launch")]
    public AudioClip launchClip;
    [Range(0f, 1f)] public float launchVolume = 0.8f;

    [Header("Audio — Hit Enemy")]
    public AudioClip hitClip;
    [Range(0f, 1f)] public float hitVolume = 1f;
    #endregion

    #region State
    private readonly List<OrbState> orbs = new List<OrbState>();
    private Transform player;
    private float damage;
    private LayerMask enemyLayer;
    private float orbitPhase;
    private float nextAutoFireTime;
    private bool isInitialized;
    private AudioSource audioSource;
    private bool isOrbitLoopPlaying;
    #endregion

    #region Types
    private class OrbState
    {
        public GameObject vfx;
        public float angleOffset;
        public bool isOrbiting = true;
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.loop = false;
    }

    private void OnDestroy()
    {
        StopOrbitLoop();
    }
    #endregion

    #region Public API
    public bool IsActive => isInitialized && orbs.Count > 0;

    /// <summary>Called by <see cref="PlayerCombat"/> when Q is first activated.</summary>
    public void Initialize(Transform playerTransform, float skillDamage, LayerMask enemies)
    {
        player = playerTransform;
        damage = skillDamage;
        enemyLayer = enemies;

        transform.SetParent(player);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        isInitialized = true;
        nextAutoFireTime = Time.time + autoFireInterval;
        SpawnOrbs();
        StartOrbitLoop();
    }

    /// <summary>Called by <see cref="PlayerCombat"/> when Q is pressed again while orbs are active.</summary>
    public void RefreshOrbs(float skillDamage)
    {
        damage = skillDamage;
        DestroyOrbitingVfx();
        nextAutoFireTime = Time.time + autoFireInterval;
        SpawnOrbs();
        StartOrbitLoop();
    }

    public void SetDamage(float skillDamage) => damage = skillDamage;

    /// <summary>Launches one orbiting fireball at the nearest enemy. Returns true if an orb was fired.</summary>
    public bool TryLaunchOrb()
    {
        if (!isInitialized || player == null) return false;

        OrbState orb = GetFirstOrbiting();
        if (orb == null) return false;

        EnemyBase target = FindNearestEnemy();
        if (target == null) return false;

        orb.isOrbiting = false;
        PlayLaunchSound();

        GameObject projectile = orb.vfx;
        projectile.transform.SetParent(null, true);

        FireOrbProjectile projectileScript = projectile.GetComponent<FireOrbProjectile>();
        if (projectileScript == null)
            projectileScript = projectile.AddComponent<FireOrbProjectile>();

        float startScale = orbScale;
        float endScale = orbScale * launchScaleMultiplier;
        projectileScript.Launch(
            target, damage, launchSpeed, hitDistance, projectileLifeTime,
            startScale, endScale, launchGrowDuration, hitClip, hitVolume);

        orbs.Remove(orb);

        if (orbs.Count == 0)
        {
            StopOrbitLoop();
            Destroy(gameObject);
        }

        return true;
    }

    public void Cleanup()
    {
        StopOrbitLoop();
        DestroyOrbitingVfx();
        orbs.Clear();
        isInitialized = false;
        Destroy(gameObject);
    }
    #endregion

    #region Audio Playback
    private void PlayLaunchSound()
    {
        if (launchClip == null || audioSource == null) return;
        audioSource.PlayOneShot(launchClip, launchVolume);
    }

    private void StartOrbitLoop()
    {
        if (orbitLoopClip == null || audioSource == null || isOrbitLoopPlaying) return;

        audioSource.clip = orbitLoopClip;
        audioSource.volume = orbitLoopVolume;
        audioSource.loop = true;
        audioSource.Play();
        isOrbitLoopPlaying = true;
    }

    private void StopOrbitLoop()
    {
        if (audioSource == null || !isOrbitLoopPlaying) return;

        audioSource.loop = false;
        audioSource.Stop();
        isOrbitLoopPlaying = false;
    }
    #endregion

    #region Update
    private void Update()
    {
        if (!isInitialized || player == null) return;

        orbitPhase += orbitSpeed * Time.deltaTime;

        for (int i = orbs.Count - 1; i >= 0; i--)
        {
            OrbState orb = orbs[i];
            if (!orb.isOrbiting || orb.vfx == null)
            {
                orbs.RemoveAt(i);
                continue;
            }

            UpdateOrbPosition(orb);
        }

        if (Time.time >= nextAutoFireTime)
        {
            if (FindNearestEnemy() != null && TryLaunchOrb())
                nextAutoFireTime = Time.time + autoFireInterval;
            else
                nextAutoFireTime = Time.time + 0.25f;
        }
    }
    #endregion

    #region Orbit Logic
    private void SpawnOrbs()
    {
        if (orbVfxPrefab == null || player == null) return;

        float angleStep = 360f / Mathf.Max(1, orbCount);

        for (int i = 0; i < orbCount; i++)
        {
            float angleOffset = i * angleStep;
            GameObject vfx = Instantiate(orbVfxPrefab, transform);
            vfx.transform.localScale = Vector3.one * orbScale;

            OrbState orb = new OrbState
            {
                vfx = vfx,
                angleOffset = angleOffset,
                isOrbiting = true
            };

            orbs.Add(orb);
            UpdateOrbPosition(orb);
        }
    }

    private void UpdateOrbPosition(OrbState orb)
    {
        float angle = (orbitPhase + orb.angleOffset) * Mathf.Deg2Rad;

        // Base loop: vertical plane behind the spine (up/down + depth), then yaw-tilt to sit flat on the back.
        Vector3 orbitOffset = new Vector3(
            0f,
            Mathf.Sin(angle) * orbitRadius,
            Mathf.Cos(angle) * orbitRadius);

        orbitOffset = Quaternion.Euler(0f, orbitPlaneYaw, 0f) * orbitOffset;

        Vector3 localOffset = new Vector3(0f, orbitHeight, -backOffset) + orbitOffset;
        orb.vfx.transform.position = player.TransformPoint(localOffset);
        orb.vfx.transform.rotation = player.rotation;
    }

    private OrbState GetFirstOrbiting()
    {
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i].isOrbiting && orbs[i].vfx != null)
                return orbs[i];
        }

        return null;
    }

    private void DestroyOrbitingVfx()
    {
        for (int i = orbs.Count - 1; i >= 0; i--)
        {
            OrbState orb = orbs[i];
            if (orb.vfx != null)
                Destroy(orb.vfx);
        }

        orbs.Clear();
    }
    #endregion

    #region Targeting
    private EnemyBase FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(player.position, maxTargetRange, enemyLayer);
        EnemyBase nearest = null;
        float bestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>() ?? hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(player.position, enemy.transform.position);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            nearest = enemy;
        }

        return nearest;
    }
    #endregion
}
