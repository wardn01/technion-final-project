using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("E Skill Settings")]
    public float speed = 20f;
    public float maxDistance = 30f;
    
    [SerializeField]
    private GameObject impactEffect;

    private ParticleSystem mainParticle;
    private Vector3 startPosition;

    private void Start()
    {
        mainParticle = GetComponent<ParticleSystem>();
        startPosition = transform.position;

        if (mainParticle != null)
        {
            mainParticle.Play(true);
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            ExplodeAndDestroy();
        }
    }

    public void ExplodeAndDestroy()
    {
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }

        if (mainParticle != null)
        {
            mainParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        this.enabled = false; 
        
        Destroy(gameObject, 1f); 
    }
}