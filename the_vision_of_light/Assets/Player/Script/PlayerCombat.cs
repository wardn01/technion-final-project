using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCombat : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private PlayerMovement movementScript;
    private RuntimeAnimatorController baseAnimatorController;

    [System.Serializable]
    public class WeaponProfile
    {
        public string weaponName;
        public GameObject weaponModel;
        public AnimatorOverrideController overrideAnimator;
        
        [Header("Damage Stats")]
        public float normalAttackDamage;
        public float skillEDamage;
        public float skillQDamage;

        [Header("VFX Prefabs")]
        public GameObject airSlashPrefab; 
        public GameObject skillQPrefab;

        [Header("Spawn Points")]
        public Transform eSpawnPoint;
        public Transform qSpawnPoint;

        [Header("Skill Settings (Per Weapon)")]
        public float skillECooldown = 8f;
        public int requiredE_For_Q = 2;

        [Header("Live State (Don't Edit)")]
        public float skillETimer = 0f;
        public int currentE_Count = 0;

        [Header("UI Settings")]
        public GameObject weaponUIRoot;
    }

    [Header("Weapons Inventory")]
    public WeaponProfile[] availableWeapons;
    public int currentWeaponIndex = 0;
    private WeaponProfile activeWeapon;

    
[Header("Switch Settings")]
public float switchCooldown = 2f;
    private float nextSwitchTime = 0f;
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
    public float attackRange = 2f; 
    public LayerMask enemyLayer;

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

        EquipWeapon(currentWeaponIndex);
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
public void EquipWeapon(int index)
    {
        if (index < 0 || index >= availableWeapons.Length) return;

        if (activeWeapon != null && activeWeapon.weaponModel != null)
        {
            activeWeapon.weaponModel.SetActive(false);
        }

        currentWeaponIndex = index;
        activeWeapon = availableWeapons[currentWeaponIndex];

        if (activeWeapon.overrideAnimator != null)
        {
            anim.runtimeAnimatorController = activeWeapon.overrideAnimator;
        }
        else
        {
            anim.runtimeAnimatorController = baseAnimatorController;
        }

        if (inCombatStance)
        {
            anim.Play("Combat_Movement", 1, 0f);
            ShowWeapon();
        }
        else
        {
            anim.Play("Empty", 1, 0f);
            HideWeapon();
        }
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("Skill_E");
        anim.ResetTrigger("Skill_Q");
        
        isAttacking = false;
        ClearBuffer();

        Debug.Log("Equipped Weapon: " + activeWeapon.weaponName);
    }   private void HandleCooldowns()
    {

        for (int i = 0; i < availableWeapons.Length; i++)
        {
            if (availableWeapons[i].skillETimer > 0f)
            {
                availableWeapons[i].skillETimer -= Time.deltaTime;
            }
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
        if (activeWeapon != null && activeWeapon.weaponModel != null)
            activeWeapon.weaponModel.SetActive(true);

        inCombatStance = true;
        combatTimer = combatStanceDuration;
    }

    public void HideWeapon()
    {
        if (activeWeapon != null && activeWeapon.weaponModel != null)
            activeWeapon.weaponModel.SetActive(false);
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

if (Time.time >= nextSwitchTime)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentWeaponIndex != 0) 
        {
            EquipWeapon(0);
            nextSwitchTime = Time.time + switchCooldown;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2) && currentWeaponIndex != 1) 
        {
            EquipWeapon(1);
            nextSwitchTime = Time.time + switchCooldown;
        }
    }
        if (!CanInterrupt() || activeWeapon == null) return;

        // Basic Attack
        if (Input.GetMouseButtonDown(0)) 
        { 
            ForceCancelRoll(); 
            RequestAttack(); 
        }

        if (Input.GetKeyDown(KeyCode.Q) && activeWeapon.currentE_Count >= activeWeapon.requiredE_For_Q)
        {
            ForceCancelRoll();
            anim.SetTrigger("Skill_Q");
            isAttacking = true;
            ShowWeapon();
            activeWeapon.currentE_Count = 0;
            ClearBuffer();
        }

        if (Input.GetKeyDown(KeyCode.E) && activeWeapon.skillETimer <= 0f)
        {
            ForceCancelRoll();
            anim.SetTrigger("Skill_E");
            isAttacking = true;
            ShowWeapon();
            activeWeapon.skillETimer = activeWeapon.skillECooldown;
            activeWeapon.currentE_Count++;
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

        if (isAlwaysMovingSkill || (isNormalAttack && currentWeaponIndex == 1))
        {
            Vector3 finalMovement = anim.deltaPosition;
            finalMovement.y = -20f * Time.deltaTime; 
            controller.Move(finalMovement);
        }
    }

    // --- Combat Events (Called via Animation) ---

    public void DealNormalDamage() 
    { 
        Debug.Log("Hit! Damage: " + (activeWeapon != null ? activeWeapon.normalAttackDamage : 0)); 
    }
    
    public void OnSkillE() 
    { 
        if (activeWeapon != null && activeWeapon.airSlashPrefab != null && activeWeapon.eSpawnPoint != null)
        {
            Instantiate(activeWeapon.airSlashPrefab, activeWeapon.eSpawnPoint.position, activeWeapon.eSpawnPoint.rotation);
        }
    }
    
    public void OnSkillQ()
    {
        if (activeWeapon != null && activeWeapon.skillQPrefab != null && activeWeapon.qSpawnPoint != null)
        {
            Instantiate(activeWeapon.skillQPrefab, activeWeapon.qSpawnPoint.position, activeWeapon.qSpawnPoint.rotation);
        }
    }

    // --- Audio Events ---
  // --- Audio Events ---
    public void PlayNormalAttackSound() 
    { 
        if (activeWeapon != null && activeWeapon.weaponModel != null) 
            activeWeapon.weaponModel.GetComponent<WeaponCombatSounds>()?.PlayNormalAttackSound(); 
    }
    
    public void PlaySkillESound() 
    { 
        if (activeWeapon != null && activeWeapon.weaponModel != null) 
            activeWeapon.weaponModel.GetComponent<WeaponCombatSounds>()?.PlaySkillESound(); 
    }
    
    public void PlaySkillQSound() 
    { 
        if (activeWeapon != null && activeWeapon.weaponModel != null) 
            activeWeapon.weaponModel.GetComponent<WeaponCombatSounds>()?.PlaySkillQSound(); 
    }
    
    public void PlayRollSound() 
    { 
        if (activeWeapon != null && activeWeapon.weaponModel != null) 
            activeWeapon.weaponModel.GetComponent<WeaponCombatSounds>()?.PlayRollSound(); 
    }
    
    public void PlayCombatWalkSound() 
    { 
        if (activeWeapon != null && activeWeapon.weaponModel != null) 
            activeWeapon.weaponModel.GetComponent<WeaponCombatSounds>()?.PlayCombatWalkSound(); 
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