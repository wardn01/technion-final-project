using UnityEngine;

/// <summary>
/// Forward-moving E-skill VFX carrier used on Wind, Fire, and Ice <c>e.prefab</c> objects.
/// Travels along local +Z, stops at <see cref="maxDistance"/>, then plays an optional impact effect.
/// Damage is handled by element-specific scripts (<see cref="WindSkillEDamage"/>, etc.), not this class.
/// </summary>
public class Projectile : MonoBehaviour
{
    #region Movement
    [Header("Movement")]
    public float speed = 20f;

    [Tooltip("Max travel distance from spawn before the projectile expires.")]
    public float maxDistance = 30f;
    #endregion

    #region VFX
    [Header("VFX")]
    [SerializeField] private GameObject impactEffect;
    #endregion

    #region State
    private ParticleSystem mainParticle;
    private Vector3 startPosition;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        mainParticle = GetComponent<ParticleSystem>();
        startPosition = transform.position;

        if (mainParticle != null)
            mainParticle.Play(true);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            ExplodeAndDestroy();
    }
    #endregion

    #region Public API
    /// <summary>Spawns optional impact VFX, stops particles, and destroys after a short delay.</summary>
    public void ExplodeAndDestroy()
    {
        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        if (mainParticle != null)
            mainParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        enabled = false;
        Destroy(gameObject, 1f);
    }
    #endregion
}
