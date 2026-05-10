using UnityEngine;

public class IdleRandomizer : MonoBehaviour
{
    private Animator anim;
    public float specialIdleInterval = 10f;
    private float timer;

    void Start()
    {
        anim = GetComponent<Animator>();
        timer = specialIdleInterval;
    }

    void Update()
    {
        timer -= Time.unscaledDeltaTime;

        if (timer <= 0f)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idel"))
            {
                PlaySpecialAnimation();
            }
            
            timer = specialIdleInterval;
        }
    }

    void PlaySpecialAnimation()
    {
        if (anim != null)
        {
            anim.SetTrigger("PlaySpecial");
        }
    }
}