using UnityEngine;

public class Goblin : NormalEnemy
{
    protected override void PerformAttack()
    {
        if (anim != null)
        {
            int randomAttack = Random.Range(1, 3); 
            anim.SetTrigger("Attack" + randomAttack);
            
            Debug.Log(randomAttack);
        }
    }
}