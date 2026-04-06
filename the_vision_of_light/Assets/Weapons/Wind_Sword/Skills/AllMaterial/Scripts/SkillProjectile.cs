using UnityEngine;

public class SkillProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 20f;
    public float lifeTime = 2.5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}