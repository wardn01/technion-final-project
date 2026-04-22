using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Combat Settings")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public float attackAnimationTime = 1.2f;
    public float hitAnimationTime = 0.8f;

    private float lastAttackTime;
    private bool isPaused = false;
    private NavMeshAgent agent;
    private Animator anim;
    private Coroutine pauseCoroutine;
    private PlayerHealth targetHealth;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject target = GameObject.FindGameObjectWithTag("Player");
            if (target != null)
                player = target.transform;
        }

        if (player != null)
        {
            targetHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (player == null || agent == null || anim == null)
            return;

        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        if (targetHealth != null && targetHealth.currentHealth <= 0)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetFloat("Speed", 0f);
            return;
        }

        if (isPaused)
            return;

        agent.SetDestination(player.position);
        anim.SetFloat("Speed", agent.velocity.magnitude);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            FaceTarget();

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                anim.SetTrigger("Attack");
                lastAttackTime = Time.time;
                TriggerPause(attackAnimationTime);
            }
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);

        if (flatDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(flatDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    public void OnHit()
    {
        TriggerPause(hitAnimationTime);
    }

    public void DealDamage()
    {
        if (player == null || isPaused == false)
            return;

        if (targetHealth != null && targetHealth.currentHealth <= 0)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange + 0.5f)
        {
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(attackDamage);
            }
        }
    }

    private void TriggerPause(float duration)
    {
        if (pauseCoroutine != null)
            StopCoroutine(pauseCoroutine);
            
        pauseCoroutine = StartCoroutine(PauseRoutine(duration));
    }

    private IEnumerator PauseRoutine(float duration)
    {
        isPaused = true;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
        }

        yield return new WaitForSeconds(duration);

        isPaused = false;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }
}