using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-50)]
public class PlayerCombat : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private PlayerMovement movementScript;
    private RuntimeAnimatorController baseAnimatorController;

    [Header("Player Data")]
    public PlayerData playerData;

    [Header("Active Weapon Data")]
    public WeaponItemData activeWeaponData;

    private GameObject currentWeaponModel;
    private FireSwordQOrbitSystem activeFireQSystem;
    private IceSwordQSystem activeIceQSystem;

    [Header("Player Hand & Spawn Points")]
    public Transform weaponHandPosition;
    public Transform eSpawnPoint;
    public Transform qSpawnPoint;

    private Transform defaultESpawnPoint;
    private Transform defaultQSpawnPoint;

    private readonly Dictionary<WeaponItemData.WeaponElement, ElementSpawnPoints> spawnPointsByElement =
        new Dictionary<WeaponItemData.WeaponElement, ElementSpawnPoints>();

    private struct ElementSpawnPoints
    {
        public Transform e;
        public Transform q;
    }

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

    public class WeaponState
    {
        public float skillETimer = 0f;
        public int currentE_Count = 0;
    }

    private Dictionary<WeaponItemData, WeaponState> weaponStates =
        new Dictionary<WeaponItemData, WeaponState>();

    private void Awake()
    {
        defaultESpawnPoint = eSpawnPoint;
        defaultQSpawnPoint = qSpawnPoint;
        CacheElementSpawnPoints();
    }

    private void Start()
    {
        anim = GetComponent<Animator>();

        controller = GetComponentInParent<CharacterController>();
        movementScript = GetComponentInParent<PlayerMovement>();

        if (movementScript == null)
        {
            movementScript = GetComponent<PlayerMovement>();
        }

        if (anim != null)
        {
            baseAnimatorController = anim.runtimeAnimatorController;
        }

        HideWeapon();
    }

    /// <summary>Used by <see cref="CameraZoom"/> — combo/skill lunges only, not roll.</summary>
    public bool RequiresCombatCameraFollow()
    {
        if (anim == null) return false;
        return IsCombatCameraFollowState(anim.GetCurrentAnimatorStateInfo(1))
            || (anim.IsInTransition(1) && IsCombatCameraFollowState(anim.GetNextAnimatorStateInfo(1)));
    }

    public bool IsRolling()
    {
        if (anim == null) return false;
        if (movementScript != null && movementScript.isRolling) return true;
        return anim.GetCurrentAnimatorStateInfo(1).IsName("Rolling");
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

    public WeaponState GetCurrentWeaponState()
    {
        if (activeWeaponData == null) return null;

        if (!weaponStates.ContainsKey(activeWeaponData))
        {
            weaponStates.Add(activeWeaponData, new WeaponState());
        }

        return weaponStates[activeWeaponData];
    }

    public void EquipWeapon(WeaponItemData newWeaponData)
    {
        if (newWeaponData == null) return;

        // Keep Fire/Ice Q VFX running — only drop PlayerCombat tracking.
        ReleaseActiveQSystemReferences();

        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }

        activeWeaponData = newWeaponData;

        if (activeWeaponData.animatorOverride != null)
        {
            anim.runtimeAnimatorController = activeWeaponData.animatorOverride;
        }
        else
        {
            anim.runtimeAnimatorController = baseAnimatorController;
        }

        if (activeWeaponData.weaponModelPrefab != null && weaponHandPosition != null)
        {
            currentWeaponModel = Instantiate(activeWeaponData.weaponModelPrefab, weaponHandPosition);
            currentWeaponModel.SetActive(inCombatStance);
        }

        ApplySpawnPointsForWeapon(activeWeaponData);
    }

    private void CacheElementSpawnPoints()
    {
        RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Wind, "Wind_E_SpawnPoint", "Wind_Q_SpawnPoint");
        RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Ice, "Ice_E_SpawnPoint", "Ice_Q_SpawnPoint");
        RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Fire, "Fire_E_SpawnPoint", "Fire_Q_SpawnPoint");

        if (!spawnPointsByElement.ContainsKey(WeaponItemData.WeaponElement.Wind))
        {
            RegisterElementSpawnPoints(WeaponItemData.WeaponElement.Wind, "E_SpawnPoint", "Q_SpawnPoint");
        }
    }

    private void RegisterElementSpawnPoints(
        WeaponItemData.WeaponElement element,
        string ePointName,
        string qPointName)
    {
        Transform ePoint = FindPlayerSpawnPoint(ePointName);
        Transform qPoint = FindPlayerSpawnPoint(qPointName);

        if (ePoint == null && qPoint == null) return;

        spawnPointsByElement[element] = new ElementSpawnPoints { e = ePoint, q = qPoint };
    }

    private Transform FindPlayerSpawnPoint(string pointName)
    {
        Transform searchRoot = transform;
        while (searchRoot.parent != null && searchRoot.name != "Player")
        {
            searchRoot = searchRoot.parent;
        }

        foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == pointName) return child;
        }

        return null;
    }

    private void ApplySpawnPointsForWeapon(WeaponItemData weapon)
    {
        if (weapon == null
            || !spawnPointsByElement.TryGetValue(weapon.weaponElement, out ElementSpawnPoints points))
        {
            eSpawnPoint = defaultESpawnPoint;
            qSpawnPoint = defaultQSpawnPoint;
            return;
        }

        eSpawnPoint = points.e != null ? points.e : defaultESpawnPoint;
        qSpawnPoint = points.q != null ? points.q : defaultQSpawnPoint;
    }

    public void UnequipCurrentWeapon()
    {
        ClearActiveQSystems();

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
        if (anim == null) return true;

        AnimatorStateInfo combatState = anim.GetCurrentAnimatorStateInfo(1);

        if (combatState.IsName("Empty") || combatState.IsName("Combat_Movement"))
        {
            return true;
        }

        return false;
    }

    private void HandleCooldowns()
    {
        foreach (var state in weaponStates.Values)
        {
            if (state.skillETimer > 0f)
            {
                state.skillETimer -= Time.deltaTime;
            }
        }
    }

    private void HandleCombatTimer()
    {
        if (inCombatStance && !isAttacking)
        {
            combatTimer -= Time.deltaTime;

            if (combatTimer <= 0f)
            {
                ExitCombatStance();
            }
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
        if (currentWeaponModel != null)
        {
            currentWeaponModel.SetActive(true);
        }

        inCombatStance = true;
        combatTimer = combatStanceDuration;
    }

    public void HideWeapon()
    {
        if (currentWeaponModel != null)
        {
            currentWeaponModel.SetActive(false);
        }
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

        if (Player_InputManager.Instance != null && Player_InputManager.Instance.isInputLocked) return;

        bool grounded = movementScript != null && movementScript.isGrounded;

        if (!grounded) return;

        if (!CanInterrupt() || activeWeaponData == null) return;

        if (Player_InputManager.Instance != null && Player_InputManager.Instance.AttackPressed)
        {
            ForceCancelRoll();
            RequestAttack();
        }

        WeaponState state = GetCurrentWeaponState();

        KeyCode burstKey = KeyCode.Q;
        KeyCode skillKey = KeyCode.E;

        if (KeybindManager.Instance != null && KeybindManager.Instance.keys.TryGetValue("Burst", out KeyCode burstKeyCode))
        {
            burstKey = burstKeyCode;
        }
        else if (PlayerPrefs.HasKey("Key_Burst"))
        {
            burstKey = (KeyCode)PlayerPrefs.GetInt("Key_Burst");
        }

        if (KeybindManager.Instance != null && KeybindManager.Instance.keys.TryGetValue("Skill", out KeyCode skillKeyCode))
        {
            skillKey = skillKeyCode;
        }
        else if (PlayerPrefs.HasKey("Key_Skill"))
        {
            skillKey = (KeyCode)PlayerPrefs.GetInt("Key_Skill");
        }

        if (Input.GetKeyDown(burstKey) && state.currentE_Count >= activeWeaponData.requiredE_For_Q)
        {
            ForceCancelRoll();
            anim.SetTrigger("Skill_Q");
            isAttacking = true;
            ShowWeapon();
            state.currentE_Count = 0;
            ClearBuffer();
        }

        if (Input.GetKeyDown(skillKey) && state.skillETimer <= 0f)
        {
            ForceCancelRoll();
            anim.SetTrigger("Skill_E");
            isAttacking = true;
            ShowWeapon();
            state.skillETimer = activeWeaponData.skillECooldown;
            state.currentE_Count++;
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

    private void RequestAttack()
    {
        isBufferActive = true;
        bufferTimer = bufferDuration;

        if (!isAttacking)
        {
            ExecuteAttack();
        }
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
        if (!IsRootMotionAnimatorState()) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(1);
        bool isRolling = stateInfo.IsName("Rolling");

        Vector3 finalMovement = anim.deltaPosition;
        finalMovement.y = -20f * Time.deltaTime;

        Transform moveRoot = controller.transform;
        // Roll facing is set in PlayerMovement before the animation plays.
        if (!isRolling)
            moveRoot.rotation *= anim.deltaRotation;

        controller.Move(finalMovement);
    }

    private static bool IsCombatCameraFollowState(AnimatorStateInfo stateInfo)
    {
        return stateInfo.IsName("Attack_1") || stateInfo.IsName("Attack_2") || stateInfo.IsName("Attack_3")
            || stateInfo.IsName("Skill_E") || stateInfo.IsName("Skill_Q");
    }

    private bool IsRootMotionAnimatorState()
    {
        if (anim == null) return false;

        if (anim.IsInTransition(1))
        {
            AnimatorStateInfo nextState = anim.GetNextAnimatorStateInfo(1);
            if (IsRootMotionStateName(nextState)) return true;
        }

        return IsRootMotionStateName(anim.GetCurrentAnimatorStateInfo(1));
    }

    private static bool IsRootMotionStateName(AnimatorStateInfo stateInfo)
    {
        return stateInfo.IsName("Attack_1") || stateInfo.IsName("Attack_2") || stateInfo.IsName("Attack_3")
            || stateInfo.IsName("Skill_E") || stateInfo.IsName("Skill_Q") || stateInfo.IsName("Rolling");
    }

    private int GetWeaponUpgradeBoost(WeaponItemData weapon)
    {
        if (playerData == null || weapon == null) return 0;

        int currentLvl = playerData.GetWeaponLevel(weapon.itemName);
        int totalBoost = 0;

        for (int i = 0; i < currentLvl - 1; i++)
        {
            if (weapon.upgradeLevels != null && i < weapon.upgradeLevels.Length)
            {
                totalBoost += weapon.upgradeLevels[i].damageBoost;
            }
        }

        return totalBoost;
    }

    public void DealNormalDamage()
    {
        if (activeWeaponData == null) return;

        float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
        int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
        float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
        float finalDamage = totalAttack * (activeWeaponData.normalAttackDamage / 100f);

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
                    enemyBase.TakeDamage(finalDamage);
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

            float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
            int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
            float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
            float finalDamage = totalAttack * (activeWeaponData.skillEDamage / 100f);

            WindSkillEDamage windScript = skill.GetComponent<WindSkillEDamage>();
            if (windScript != null)
            {
                windScript.SetDamage(finalDamage);
            }

            IceSkillEDamage iceScript = skill.GetComponent<IceSkillEDamage>();
            if (iceScript != null)
            {
                iceScript.SetDamage(finalDamage);
            }

            FireSkillEDamage fireScript = skill.GetComponent<FireSkillEDamage>();
            if (fireScript != null)
            {
                fireScript.SetDamage(finalDamage);
            }
        }
    }

    public void OnSkillQ()
    {
        if (activeWeaponData == null) return;

        float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
        int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
        float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
        float finalDamage = totalAttack * (activeWeaponData.skillQDamage / 100f);

        if (activeWeaponData.weaponElement == WeaponItemData.WeaponElement.Fire)
        {
            int boomPercent = activeWeaponData.skillQStrikeDamage > 0
                ? activeWeaponData.skillQStrikeDamage
                : activeWeaponData.skillQDamage;

            float boomDamage = CalculateSkillDamage(boomPercent);
            float orbDamage = CalculateSkillDamage(activeWeaponData.skillQDamage);
            HandleFireSkillQ(boomDamage, orbDamage);
            return;
        }

        if (activeWeaponData.weaponElement == WeaponItemData.WeaponElement.Ice)
        {
            HandleIceSkillQ(CalculateSkillDamage(activeWeaponData.skillQDamage));
            return;
        }

        if (activeWeaponData.skillQPrefab == null || qSpawnPoint == null) return;

        GameObject skill = Instantiate(
            activeWeaponData.skillQPrefab,
            qSpawnPoint.position,
            qSpawnPoint.rotation);

        WindSkillQDamage windScript = skill.GetComponent<WindSkillQDamage>();
        if (windScript != null)
            windScript.SetDamage(finalDamage);
    }

    private float CalculateSkillDamage(int damagePercent)
    {
        if (activeWeaponData == null) return 0f;

        float playerBaseAttack = playerData != null ? playerData.GetTotalAttack() : 0f;
        int weaponBoost = GetWeaponUpgradeBoost(activeWeaponData);
        float totalAttack = playerBaseAttack + activeWeaponData.weaponBaseAttack + weaponBoost;
        return totalAttack * (damagePercent / 100f);
    }

    private void HandleIceSkillQ(float ballDamage)
    {
        if (activeIceQSystem == null)
            activeIceQSystem = GetComponentInChildren<IceSwordQSystem>();

        if (activeIceQSystem != null && activeIceQSystem.IsActive)
        {
            activeIceQSystem.Activate(ballDamage, enemyLayer);
            return;
        }

        activeIceQSystem = null;

        if (activeWeaponData.skillQPrefab == null) return;

        GameObject skill = Instantiate(activeWeaponData.skillQPrefab, transform.position, Quaternion.identity);
        IceSwordQSystem iceSystem = skill.GetComponent<IceSwordQSystem>();
        if (iceSystem == null) return;

        activeIceQSystem = iceSystem;
        iceSystem.Initialize(transform, ballDamage, enemyLayer);
    }

    private void HandleFireSkillQ(float boomDamage, float orbDamage)
    {
        SpawnFireSkillQBoom(boomDamage);

        if (activeFireQSystem == null)
            activeFireQSystem = GetComponentInChildren<FireSwordQOrbitSystem>();

        if (activeFireQSystem != null && activeFireQSystem.IsActive)
        {
            activeFireQSystem.RefreshOrbs(orbDamage);
            return;
        }

        activeFireQSystem = null;

        if (activeWeaponData.skillQPrefab == null) return;

        GameObject skill = Instantiate(activeWeaponData.skillQPrefab, transform.position, Quaternion.identity);
        FireSwordQOrbitSystem fireOrbit = skill.GetComponent<FireSwordQOrbitSystem>();
        if (fireOrbit == null) return;

        activeFireQSystem = fireOrbit;
        fireOrbit.Initialize(transform, orbDamage, enemyLayer);
    }

    private void SpawnFireSkillQBoom(float finalDamage)
    {
        if (activeWeaponData.skillQStrikePrefab == null || qSpawnPoint == null) return;

        GameObject boom = Instantiate(
            activeWeaponData.skillQStrikePrefab,
            qSpawnPoint.position,
            qSpawnPoint.rotation);

        FireSkillQBoom boomScript = boom.GetComponent<FireSkillQBoom>();
        if (boomScript != null)
            boomScript.SetDamage(finalDamage);
    }

    private void ReleaseActiveQSystemReferences()
    {
        activeFireQSystem = null;
        activeIceQSystem = null;
    }

    private void ClearActiveQSystems()
    {
        ClearFireQOrbs();
        ClearIceQSystem();
    }

    private void ClearIceQSystem()
    {
        if (activeIceQSystem == null) return;

        activeIceQSystem.Cleanup();
        activeIceQSystem = null;
    }

    private void ClearFireQOrbs()
    {
        if (activeFireQSystem == null) return;

        activeFireQSystem.Cleanup();
        activeFireQSystem = null;
    }

    public void PlayNormalAttackSound()
    {
        if (currentWeaponModel != null)
        {
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayNormalAttackSound();
        }
    }

    public void PlaySkillESound()
    {
        if (activeWeaponData == null) return;

        Vector3 position = eSpawnPoint != null ? eSpawnPoint.position : transform.position;
        WeaponSkillAudio.PlayFromPrefab(activeWeaponData.skillEPrefab, position);
    }

    public void PlaySkillQSound()
    {
        if (activeWeaponData == null) return;

        Vector3 position = qSpawnPoint != null ? qSpawnPoint.position : transform.position;
        if (WeaponSkillAudio.PlayFromPrefab(activeWeaponData.skillQPrefab, position))
            return;

        PlayIceQCastSound(activeWeaponData.skillQPrefab, position);
    }

    private static void PlayIceQCastSound(GameObject qPrefab, Vector3 position)
    {
        IceSwordQSystem iceQ = qPrefab != null ? qPrefab.GetComponent<IceSwordQSystem>() : null;
        if (iceQ == null || iceQ.circleVfxPrefab == null) return;

        IceCircleQZone circle = iceQ.circleVfxPrefab.GetComponent<IceCircleQZone>();
        if (circle == null || circle.circleOpenClip == null) return;

        AudioSource.PlayClipAtPoint(circle.circleOpenClip, position, circle.circleOpenVolume);
    }

    public void PlayRollSound()
    {
        if (currentWeaponModel != null)
        {
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayRollSound();
        }
    }

    public void PlayCombatWalkSound()
    {
        if (currentWeaponModel != null)
        {
            currentWeaponModel.GetComponent<WeaponCombatSounds>()?.PlayCombatWalkSound();
        }
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