using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private PlayerMovement movementScript;

    [Header("Weapon Settings")]
    public GameObject weaponModel;
    public float combatStanceDuration = 5f;
    private float combatTimer;

    [Header("Damage Settings (New)")]
    public float normalAttackDamage = 15f;
    public float skillEDamage = 35f;
    public float skillQDamage = 80f;
    public float attackRange = 2f; 
    public LayerMask enemyLayer;   

    [Header("Input Buffer Settings")]
    public float bufferDuration = 0.1f;
    private bool isBufferActive;
    private float bufferTimer;

    [Header("Player State")]
    public bool isAttacking;
    public bool inCombatStance;

    [Header("Skill Cooldowns")]
    public float skillECooldown = 1f;
    public float skillETimer;

    [Header("Combo System")]
    public int requiredE_For_Q = 2;
    public int currentE_Count;

    [Header("Air Slash VFX")]
    public GameObject airSlashPrefab;
    public Transform slashSpawnPoint;

    [Header("Skill Q VFX")]
    public GameObject skillQPrefab;
    public Transform qSpawnPoint;

    private void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponentInParent<CharacterController>();
        movementScript = GetComponentInParent<PlayerMovement>();
        if (movementScript == null) movementScript = GetComponent<PlayerMovement>();

        HideWeapon();
    }

    private void Update()
    {
        CheckAttackState();
        HandleInput();
        UpdateBuffer();
        HandleCombatTimer();
        HandleCooldowns();

        if (inCombatStance && movementScript != null && !movementScript.isGrounded)
        {
            if (movementScript.groundDistance > 1f)
            {
                ExitCombatStance(true);
            }
        }
    }

    private void HandleCooldowns()
    {
        if (skillETimer > 0f)
            skillETimer -= Time.deltaTime;
    }

    private void HandleCombatTimer()
    {
        if (inCombatStance && !isAttacking)
        {
            combatTimer -= Time.deltaTime;
            if (combatTimer <= 0f)
                ExitCombatStance();
        }
    }

public void ExitCombatStance(bool isRunning = false)
    {
        inCombatStance = false;
        HideWeapon();
        
        if (!isRunning)
        {
            anim.SetTrigger("SheathWeapon");
        }
        else
        {
            anim.Play("Empty", 1, 0f); 
        }
    }

    public void ShowWeapon()
    {
        if (weaponModel != null)
            weaponModel.SetActive(true);

        inCombatStance = true;
        combatTimer = combatStanceDuration;
    }

    public void HideWeapon()
    {
        if (weaponModel != null)
            weaponModel.SetActive(false);
    }
public bool CanInterrupt()
{
    if (anim == null) return true;

    AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(1);
    
    if (anim.IsInTransition(1)) return false; 

    if (state.IsName("Skill_E") || state.IsName("Skill_Q"))
    {
        return state.normalizedTime >= 0.95f;
    }
    else if (state.IsName("Rolling") || state.IsName("Attack_1") || state.IsName("Attack_2") || state.IsName("Attack_3"))
    {
        return state.normalizedTime >= 0.60f;
    }
    
    return true; 
}

private void HandleInput()
{   
    if (EventSystem.current.IsPointerOverGameObject()) return;

    bool grounded = movementScript != null && movementScript.isGrounded;
    if (!grounded) return;

    if (!CanInterrupt()) return;

    if (Input.GetMouseButtonDown(0)) 
    {
        ForceCancelRoll();
        RequestAttack();
    }

    if (Input.GetKeyDown(KeyCode.Q) && currentE_Count >= requiredE_For_Q)
    {
        ForceCancelRoll();
        anim.SetTrigger("Skill_Q");
        isAttacking = true;
        ShowWeapon();
        currentE_Count = 0;
        ClearBuffer();
    }

    if (Input.GetKeyDown(KeyCode.E) && skillETimer <= 0f)
    {
        ForceCancelRoll();
        anim.SetTrigger("Skill_E");
        isAttacking = true;
        ShowWeapon();
        skillETimer = skillECooldown;
        currentE_Count++;
        ClearBuffer();
    }
}
private void UpdateBuffer()
{
    if (!isBufferActive) return;

    bufferTimer -= Time.deltaTime;
    if (bufferTimer <= 0f) 
    { 
        isBufferActive = false; 
        return; 
    }

    if (CanInterrupt())
    {
        ExecuteAttack();
    }
}

private void CheckAttackState()
{
    AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(1);
    
    if (stateInfo.IsName("Empty") || stateInfo.IsName("Combat_Movement") || stateInfo.IsName("Rolling"))
    {
        isAttacking = false;
    }
    else
    {
        isAttacking = true; 
    }

    anim.SetBool("isAttacking", isAttacking);
}
private void ForceCancelRoll()
{
    if (movementScript.isRolling)
    {
        movementScript.isRolling = false;
        anim.SetBool("isRolling", false);
    }
}
    private void SpawnAirSlash()
    {
        if (airSlashPrefab != null && slashSpawnPoint != null)
        {
            GameObject slash = Instantiate(airSlashPrefab, slashSpawnPoint.position, slashSpawnPoint.rotation);
        }
    }

    private void RequestAttack()
    {
        isBufferActive = true;
        bufferTimer = bufferDuration;

        if (!isAttacking)
            ExecuteAttack();
    }


    private void ExecuteAttack()
    {
        anim.SetTrigger("Attack");
        isAttacking = true;
        ShowWeapon();
        ClearBuffer();
    }

    private void ClearBuffer()
    {
        isBufferActive = false;
        bufferTimer = 0f;
    }


 private void OnAnimatorMove()
    {
        if (anim == null || controller == null)
            return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(1);
        
        if (stateInfo.IsName("Skill_Q") || stateInfo.IsName("Rolling"))
        {
            Vector3 finalMovement = anim.deltaPosition;
            finalMovement.y = -20f * Time.deltaTime;
            controller.Move(finalMovement);
        }
    }

    public void DealNormalDamage()
    {
        Debug.Log("Normal Attack Hit! Damage: " + normalAttackDamage);
    }

  public void OnSkillE()
    {
        SpawnAirSlash();
        
    }

    public void OnSkillQ()
    {
        if (skillQPrefab != null && qSpawnPoint != null)
        {
            GameObject qSlash = Instantiate(skillQPrefab, qSpawnPoint.position, qSpawnPoint.rotation);
        }
        
    }

    public void PlayNormalAttackSound()
    {
        if (weaponModel != null) 
            weaponModel.GetComponent<WindSwordSounds>()?.PlayNormalAttackSound();
    }

    public void PlaySkillESound()
    {
        if (weaponModel != null) 
            weaponModel.GetComponent<WindSwordSounds>()?.PlaySkillESound();
    }

    public void PlaySkillQSound()
    {
        if (weaponModel != null) 
            weaponModel.GetComponent<WindSwordSounds>()?.PlaySkillQSound();
    }

    public void PlayRollSound()
    {
        if (weaponModel != null) 
            weaponModel.GetComponent<WindSwordSounds>()?.PlayRollSound();
    }

    public void PlayCombatWalkSound()
    {
        if (weaponModel != null) 
            weaponModel.GetComponent<WindSwordSounds>()?.PlayCombatWalkSound();
    }

   public void CancelAttack()
    {
        isAttacking = false;
        
        ClearBuffer();

        if (anim != null)
        {
            anim.ResetTrigger("Attack1"); 
            anim.ResetTrigger("Attack2"); 
            anim.ResetTrigger("Attack");
        }
    }

}