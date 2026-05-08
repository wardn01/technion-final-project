using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private PlayerMovement movementScript;
    private RuntimeAnimatorController baseAnimatorController;

    [Header("Active Weapon Data")]
    public WeaponItemData activeWeaponData;
    private GameObject currentWeaponModel;

    [Header("Player Hand & Spawn Points")]
    public Transform weaponHandPosition;
    public Transform eSpawnPoint;
    public Transform qSpawnPoint;

    [Header("Input Buffer Settings")]
    public float bufferDuration = 0.1f;
    private bool isBufferActive;
    private float bufferTimer;

    [Header("Player State")]
    public bool isAttacking;
    public bool inCombatStance;

    [Header("Combat Settings")]
    public float combatStanceDuration = 5f;
    private float combatTimer;
    public float attackRange = 2.5f; 
    public float attackAngle = 100f;
    public LayerMask enemyLayer;

    [Header("Live Skill State")]
    public float skillETimer = 0f;
    public int currentE_Count = 0;

    private void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponentInParent<CharacterController>();
        movementScript = GetComponentInParent<PlayerMovement>();
        if (movementScript == null) movementScript = GetComponent<PlayerMovement>();

        if (anim != null)
        {
            baseAnimatorController = anim.runtimeAnimatorController;
        }

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
    
    public void EquipWeapon(WeaponItemData newWeaponData)
    {
        if (newWeaponData == null) return;

        if (currentWeaponModel != null) Destroy(currentWeaponModel);

        activeWeaponData = newWeaponData;

        skillETimer = 0f;
        currentE_Count = 0;

        if (activeWeaponData.animatorOverride != null)
            anim.runtimeAnimatorController = activeWeaponData.animatorOverride;
        else
            anim.runtimeAnimatorController = baseAnimatorController;

        if (activeWeaponData.weaponModelPrefab != null && weaponHandPosition != null)
        {
            currentWeaponModel = Instantiate(activeWeaponData.weaponModelPrefab, weaponHandPosition);
            currentWeaponModel.SetActive(inCombatStance);
        }
        
        Debug.Log("Equipped weapon: " + activeWeaponData.itemName);
    }

    public void UnequipCurrentWeapon()
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
            currentWeaponModel = null;
        }

        activeWeaponData = null;

        if (anim != null)
        {
            AnimatorStateInfo combatState = anim.GetCurrentAnimatorStateInfo(1);

            if (!combatState.IsName("Empty"))
            {
                anim.Play("Movement", 0); 
                anim.Play("Empty", 1);
            }

            anim.runtimeAnimatorController = baseAnimatorController;
            anim.SetBool("isAttacking", false);
            anim.SetBool("isRolling", false);
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Skill_E");
            anim.ResetTrigger("Skill_Q");
        }

        inCombatStance = false;
        isAttacking = false;
    }

    public bool IsSafeToEquip()
    {
        AnimatorStateInfo combatState = anim.GetCurrentAnimatorStateInfo(1);
        bool isAttackingState = anim.GetBool("isAttacking");
        bool isInCombatAnimation = !combatState.IsName("Empty");

        return !isAttackingState && !isInCombatAnimation;
    }
    
    private void HandleCooldowns()
    {
        if (skillETimer > 0f)
        {
            skillETimer -= Time.deltaTime;
        }
    }

    private void HandleCombatTimer()
    {
        if (inCombatStance && !isAttacking)
        {
            combatTimer -= Time.deltaTime;
            if (combatTimer <= 0f) ExitCombatStance();
        }
    }

    public void ExitCombatStance(bool isRunning = false)
    {
        inCombatStance = false;
        HideWeapon();
        if (!isRunning) anim.SetTrigger("SheathWeapon");
        else anim.Play("Empty", 1, 0f); 
    }

    public void ShowWeapon()
    {
        if (currentWeaponModel != null)
            currentWeaponModel.SetActive(true);

        inCombatStance = true;
        combatTimer = combatStanceDuration;
    }

    public void HideWeapon()
    {
        if (currentWeaponModel != null)
            currentWeaponModel.SetActive(false);
    }

    public bool CanInterrupt()
    {
        if (anim == null) return true;
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(1);
        if (anim.IsInTransition(1)) return false; 
        
        if (state.IsName("Skill_E") || state.IsName("Skill_Q")) 
            return state.normalizedTime >= 0.95f;
        else if (state.IsName("Rolling") || state.IsName("Attack_1") || state.IsName("Attack_2") || state.IsName("Attack_3")) 
            return state.normalizedTime >= 0.60f;
            
        return true; 
    }

    private void HandleInput()
    {   
        if (EventSystem.current.IsPointerOverGameObject()) return;

        bool grounded = movementScript != null && movementScript.isGrounded;
        if (!grounded) return;

        if (!CanInterrupt() || activeWeaponData == null) return;

        if (Input.GetMouseButtonDown(0)) 
        { 
            ForceCancelRoll(); 
            RequestAttack(); 
        }

        if (Input.GetKeyDown(KeyCode.Q) && currentE_Count >= activeWeaponData.requiredE_For_Q)
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
            skillETimer = activeWeaponData.skillECooldown;
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

        if (CanInterrupt()) ExecuteAttack();
    }

    private void CheckAttackState()
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(1);

        if (stateInfo.IsName("Empty") || stateInfo.IsName("Combat_Movement") || stateInfo.IsName("Rolling"))
            isAttacking = false;
        else
            isAttacking = true;

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

    private void RequestAttack()
    {
        isBufferActive = true;
        bufferTimer = bufferDuration;
        if (!isAttacking) ExecuteAttack();
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
        if (anim == null || controller == null) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(1);

        bool isAlwaysMovingSkill = stateInfo.IsName("Skill_Q") || stateInfo.IsName("Rolling");
        bool isNormalAttack = stateInfo.IsName("Attack_1") || stateInfo.IsName("Attack_2") || stateInfo.IsName("Attack_3");

        if (isAlwaysMovingSkill || isNormalAttack)
        {
            Vector3 finalMovement = anim.deltaPosition;
            finalMovement.y = -20f * Time.deltaTime;
            controller.Move(finalMovement);
        }
    }

    public void DealNormalDamage()
    {
        if (activeWeaponData == null) return;

        // 🔥 تم التحويل لـ float
        float playerBonus = PlayerData.Instance != null ? PlayerData.Instance.GetTotalAttack() : 0f;
        float totalDamage = activeWeaponData.normalAttackDamage + playerBonus;

        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase != null && !enemyBase.IsDead)
            {
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                directionToEnemy.y = 0;

                float angle = Vector3.Angle(transform.forward, directionToEnemy);

                if (angle <= attackAngle)
                {
                    enemyBase.TakeDamage(totalDamage);
                    Debug.Log($"[Player] Hit {enemy.name} for {totalDamage} damage! (Weapon: {activeWeaponData.normalAttackDamage} + Base: {playerBonus})");
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Vector3 rightDir = Quaternion.Euler(0, attackAngle, 0) * transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -attackAngle, 0) * transform.forward;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, rightDir * attackRange);
        Gizmos.DrawRay(transform.position, leftDir * attackRange);
    }
    
    public void OnSkillE()
    {
        if (activeWeaponData != null && activeWeaponData.skillEPrefab != null && eSpawnPoint != null)
        {
            GameObject skill = Instantiate(activeWeaponData.skillEPrefab, eSpawnPoint.position, eSpawnPoint.rotation);
            
            float playerBonus = PlayerData.Instance != null ? PlayerData.Instance.GetTotalAttack() : 0f;
            float totalDamage = activeWeaponData.skillEDamage + playerBonus;

            WindSkillEDamage windScript = skill.GetComponent<WindSkillEDamage>();
            if (windScript != null) windScript.SetDamage(totalDamage);

            IceSkillEDamage iceScript = skill.GetComponent<IceSkillEDamage>();
            if (iceScript != null) iceScript.SetDamage(totalDamage);
        }
    }
    
    public void OnSkillQ()
    {
        if (activeWeaponData != null && activeWeaponData.skillQPrefab != null && qSpawnPoint != null)
        {
            GameObject skill = Instantiate(activeWeaponData.skillQPrefab, qSpawnPoint.position, qSpawnPoint.rotation);
            
            float playerBonus = PlayerData.Instance != null ? PlayerData.Instance.GetTotalAttack() : 0f;
            float totalDamage = activeWeaponData.skillQDamage + playerBonus;

            IceSkillQDamage iceScript = skill.GetComponent<IceSkillQDamage>();
            if (iceScript != null)
            {
                skill.transform.parent = qSpawnPoint;
                iceScript.SetDamage(totalDamage);
            }

            WindSkillQDamage windScript = skill.GetComponent<WindSkillQDamage>();
            if (windScript != null) windScript.SetDamage(totalDamage);
        }
    }

    public void PlayNormalAttackSound()
    {
        if (currentWeaponModel != null) 
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayNormalAttackSound(); 
    }
    
    public void PlaySkillESound()
    {
        if (currentWeaponModel != null) 
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlaySkillESound(); 
    }
    
    public void PlaySkillQSound()
    {
        if (currentWeaponModel != null) 
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlaySkillQSound(); 
    }
    
    public void PlayRollSound()
    {
        if (currentWeaponModel != null) 
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayRollSound(); 
    }
    
    public void PlayCombatWalkSound()
    {
        if (currentWeaponModel != null) 
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayCombatWalkSound(); 
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