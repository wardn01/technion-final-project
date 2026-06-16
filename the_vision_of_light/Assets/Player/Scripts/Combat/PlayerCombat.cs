using System.Collections.Generic;
using UnityEngine;
using VisionOfLight.Enemy;

namespace VisionOfLight.Player
{
    /// <summary>
    /// Player combat controller — weapon state, attack buffering, combat stance, and per-weapon skill cooldowns.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public partial class PlayerCombat : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Player Data")]
        public PlayerData playerData;

        [Header("Active Weapon Data")]
        public WeaponItemData activeWeaponData;

        [Header("Player Hand & Spawn Points")]
        public Transform weaponHandPosition;
        public Transform eSpawnPoint;
        public Transform qSpawnPoint;

        [Header("Input Buffer Settings")]
        [Tooltip("Seconds an attack input remains buffered while waiting for animation interrupt.")]
        public float bufferDuration = 0.1f;

        [Header("Player State")]
        public bool isAttacking;
        public bool inCombatStance;

        [Header("Combat Settings")]
        [Tooltip("Seconds before combat stance auto-sheaths when not attacking.")]
        public float combatStanceDuration = 5f;

        public float attackRange = 2.5f;
        public float attackAngle = 100f;

        public LayerMask enemyLayer;
        #endregion

        #region Runtime State
        private Animator anim;
        private CharacterController controller;
        private PlayerMovement movementScript;
        private RuntimeAnimatorController baseAnimatorController;

        private GameObject currentWeaponModel;
        private FireSwordQOrbitSystem activeFireQSystem;
        private IceSwordQSystem activeIceQSystem;

        private Transform defaultESpawnPoint;
        private Transform defaultQSpawnPoint;

        private readonly Dictionary<WeaponItemData.WeaponElement, ElementSpawnPoints> spawnPointsByElement =
            new Dictionary<WeaponItemData.WeaponElement, ElementSpawnPoints>();

        private struct ElementSpawnPoints
        {
            public Transform e;
            public Transform q;
        }

        private bool isBufferActive;
        private float bufferTimer;

        private float combatTimer;

        public class WeaponState
        {
            public float skillETimer = 0f;
            public int currentE_Count = 0;
        }

        private Dictionary<WeaponItemData, WeaponState> weaponStates =
            new Dictionary<WeaponItemData, WeaponState>();
        #endregion

        #region Unity Lifecycle
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
        #endregion

        #region Public API
        /// <summary>Used by <see cref="CameraZoom"/> — combo/skill lunges only, not roll.</summary>
        public bool RequiresCombatCameraFollow()
        {
            if (anim == null) return false;
            return IsCombatCameraFollowState(anim.GetCurrentAnimatorStateInfo(1))
                || (anim.IsInTransition(1) && IsCombatCameraFollowState(anim.GetNextAnimatorStateInfo(1)));
        }

        /// <summary>Returns true when the player is in a roll animation or movement roll state.</summary>
        public bool IsRolling()
        {
            if (anim == null) return false;
            if (movementScript != null && movementScript.isRolling) return true;
            return anim.GetCurrentAnimatorStateInfo(1).IsName("Rolling");
        }

        /// <summary>Returns per-weapon skill cooldown and E-charge state for <see cref="activeWeaponData"/>.</summary>
        public WeaponState GetCurrentWeaponState()
        {
            if (activeWeaponData == null) return null;

            if (!weaponStates.ContainsKey(activeWeaponData))
            {
                weaponStates.Add(activeWeaponData, new WeaponState());
            }

            return weaponStates[activeWeaponData];
        }

        /// <summary>Returns true when the combat layer is idle enough to swap weapons.</summary>
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

        /// <summary>Clears attack state, input buffer, and animation triggers.</summary>
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
        #endregion

        #region Private Helpers
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
        #endregion
    }
}
