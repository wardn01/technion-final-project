using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private GameObject SpawnWhenFinish;

    public float speed = 20;
    public float distance = 30;
    private ParticleSystem mainParticle;

    private Vector3 initPosition;

    private void Start()
    {
        mainParticle = GetComponent<ParticleSystem>();

        if (mainParticle != null)
        {
            mainParticle.Play(true);
        }

        initPosition = transform.position;
    }

    private void Update()
    {
        if (mainParticle && mainParticle.isPlaying)
        {
            transform.position = Vector3.MoveTowards(transform.position, transform.position + transform.forward, speed * Time.deltaTime);

            if (Vector3.Distance(initPosition, transform.position) > distance)
            {
                mainParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                
                if (SpawnWhenFinish != null)
                {
                    Instantiate(SpawnWhenFinish, transform.position, Quaternion.identity);
                }
                
                Destroy(gameObject, 1f); 
            }
        }
    }
}