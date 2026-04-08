using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerStamina))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerMovementSound playerSounds;
    public PlayerCombat combatScript;

    [Header("References")]
    public CharacterController controller;
    public Transform cam;
    public Animator animator;
    public GameObject gliderObject;

    private PlayerStamina playerStamina;
    private PlayerHealth playerHealth;

    [Header("Movement Settings")]
    public float combatWalkSpeed = 3f;
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float gravity = -15f;

    [Header("Stamina Costs")]
    public float runStaminaCost = 15f;
    public float glideStaminaCost = 10f;
    public float jumpStaminaCost = 15f;
    public float swimStaminaCost = 8f;

    [Header("Fall Damage Settings")]
    public float safeFallHeight = 5f;
    public float damagePerMeter = 10f;
    private float highestYPosition;

    [Header("Jump & Fall Settings")]
    public float jumpHeight = 2.5f;
    public float jumpDelay = 0.15f;
    public float jumpCooldown = 2f;
    public LayerMask groundLayer;
    public float groundDistance;

    [Header("Gliding Settings")]
    public float glideFallSpeed = -5f;
    public float glideSpeed = 12f;
    public float minGlideHeight = 4f;
    private bool isGliding = false;

    [Header("Gliding Toggle Settings")]
    public float glideToggleDelay = 1f;
    private float nextToggleTime = 0f;

    [Header("Gliding Tilt Settings")]
    public float forwardTiltAngle = 15f;
    public float sideTiltAngle = 10f;
    public float tiltSmoothTime = 5f;

    [Header("Swimming Settings")]
    public float swimSpeed = 4f;
    public float swimStartDepth = 1.3f;
    public float swimFloatOffset = 1.2f;
    public float floatSmoothness = 10f;

    [Header("Roll Settings")]
    public float rollDuration = 0.7f;
    public float rollStaminaCost = 15f;
    [HideInInspector] public bool isRolling = false;

    [HideInInspector] public bool isSwimming = false;
    private bool inWaterBounds = false;
    private float waterSurfaceY = 0f;

    [Header("Physics Settings")]
    public float turnSmoothTime = 0.1f;

    float turnSmoothVelocity;
    Vector3 velocity;
    Vector3 airMomentum;
    bool wasGrounded;
    float currentAnimSpeed;
    Vector3 currentHorizontalMove;
    bool isJumping;
    float nextJumpTime = 0f;

    [HideInInspector] public bool isGrounded;
    private bool isLockedByAnimation = false;

    void Start()
    {
        playerSounds = GetComponentInChildren<PlayerMovementSound>();
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cam == null) cam = Camera.main.transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (gliderObject != null) gliderObject.SetActive(false);

        playerStamina = GetComponent<PlayerStamina>();
        playerHealth = GetComponent<PlayerHealth>();

        if (combatScript == null) combatScript = GetComponent<PlayerCombat>();

        highestYPosition = transform.position.y;
        ValidateSettings();
    }

    void Update()
    {
        CheckAnimationLocks();
        HandleGroundCheck();
        HandleWaterState();
        HandleJumping();

        if (!isLockedByAnimation && (combatScript == null || !combatScript.isAttacking))
        {
            HandleMovement();
            HandleJumping();
            HandleGliding();
        }
        else
        {
            currentHorizontalMove = Vector3.zero;
            currentAnimSpeed = 0f;
            if (isGrounded && velocity.y < 0) velocity.y = -2f;
        }

        ApplyMovement();
        UpdateAnimations();
        HandleWindSound();

    }

    void HandleMovement()
    {
    if (isRolling)
    {
        currentHorizontalMove = Vector3.zero; 
        return; 
    }   
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        float currentSpeed = walkSpeed;

        if (combatScript != null && combatScript.inCombatStance)
        {
            currentSpeed = combatWalkSpeed;
        }
        
        if (isSwimming)
    {
        if (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f)
        {
            currentSpeed = swimSpeed;

            if (playerStamina != null)
            {
                playerStamina.ConsumeStamina(swimStaminaCost * Time.deltaTime);

                if (!playerStamina.HasStamina())
                {
                    currentSpeed = swimSpeed * 0.5f;
                }
            }
        }
        else
        {
            currentSpeed = 0f;
        }
    }
        else if (isGliding)
        {
            currentSpeed = glideSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && playerStamina != null && playerStamina.HasStamina())
        {
            if (combatScript != null && combatScript.inCombatStance && !combatScript.isAttacking)
            {
                combatScript.ExitCombatStance(true); 
                if (animator != null) animator.CrossFade("Movement", 0.2f);
            }

            if (combatScript != null && !combatScript.isAttacking)
            {
                currentSpeed = runSpeed;
                if (isGrounded && (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f))
                {
                    playerStamina.ConsumeStamina(runStaminaCost * Time.deltaTime);
                }
            }
            else currentSpeed = walkSpeed;
        }

        Vector3 direction = new Vector3(x, 0f, z).normalized;
        Vector3 moveDirection = Vector3.zero;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float currentTurnSpeed = (isGliding) ? 0.5f : ((!isGrounded && !isSwimming) ? 0.3f : turnSmoothTime);
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, currentTurnSpeed);
            
            if (!isGliding) transform.rotation = Quaternion.Euler(0f, angle, 0f);
            else transform.rotation = Quaternion.Euler(transform.eulerAngles.x, angle, transform.eulerAngles.z);
            
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            if (isGrounded || isSwimming || isGliding) airMomentum = moveDirection;            
            currentAnimSpeed = currentSpeed;
        }
        else
        {
            if (isGrounded || isSwimming || isGliding) airMomentum = Vector3.zero;
            currentAnimSpeed = 0f;
        }

        if (!isGrounded && !isGliding && !isSwimming) moveDirection = airMomentum;
        currentHorizontalMove = moveDirection.normalized * currentSpeed;
    }

    void LateUpdate()
    {
        if (isGliding && !isSwimming)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            float targetX = (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f) ? forwardTiltAngle : 0f;
            float targetZ = x * -sideTiltAngle;
            Quaternion targetRotation = Quaternion.Euler(targetX, transform.eulerAngles.y, targetZ);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * tiltSmoothTime);
        }
        else if (!isGrounded && !isLockedByAnimation && !isSwimming)
        {
            Quaternion straightRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, straightRotation, Time.deltaTime * tiltSmoothTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water")) { inWaterBounds = true; waterSurfaceY = other.bounds.max.y; if (isGliding) StopGliding(); }
    }

    private void OnTriggerStay(Collider other) { if (other.CompareTag("Water")) waterSurfaceY = other.bounds.max.y; }

    private void OnTriggerExit(Collider other) { if (other.CompareTag("Water")) { inWaterBounds = false; isSwimming = false; } }

    void HandleWaterState()
    {
        if (inWaterBounds)
        {
            float currentDepth = waterSurfaceY - transform.position.y;
            if (currentDepth >= swimStartDepth && !isSwimming) { isSwimming = true; isGrounded = false; velocity.y = 0f; }
            else if (currentDepth < swimStartDepth && isGrounded && isSwimming) isSwimming = false;
        }
        else isSwimming = false;
    }

    void CheckAnimationLocks()
    {
        if (animator == null) return;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        isLockedByAnimation = !isSwimming && (stateInfo.IsName("HardLanding") || (stateInfo.IsName("Landing") && isGrounded));
    }

    void HandleGroundCheck()
    {
        RaycastHit hit;
        groundDistance = (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 100f, groundLayer)) ? hit.distance - 1f : 100f;
        isGrounded = controller.isGrounded;
        if (groundDistance <= 0.15f && velocity.y <= 0) isGrounded = true;
        if (isGliding && isGrounded) StopGliding();
        if (isGliding || isSwimming) highestYPosition = transform.position.y;
        else if (!isGrounded) { if (transform.position.y > highestYPosition) highestYPosition = transform.position.y; }
        else if (isGrounded && !wasGrounded)
        {
            float fallDistance = highestYPosition - transform.position.y;
            if (fallDistance > safeFallHeight && playerHealth != null)
                playerHealth.TakeDamage(Mathf.RoundToInt((fallDistance - safeFallHeight) * damagePerMeter));
            highestYPosition = transform.position.y;
        }
        wasGrounded = isGrounded;
        if (isGrounded && velocity.y < 0 && !isSwimming) { velocity.y = -2f; animator.ResetTrigger("Jump"); }
    }
void HandleJumping()
{
    if (Input.GetButtonDown("Jump") && isGrounded && !isSwimming && Time.time >= nextJumpTime)
    {
        if (combatScript != null && combatScript.inCombatStance)
        {
            if (combatScript.CanInterrupt() && !isRolling && playerStamina != null && playerStamina.HasStamina(rollStaminaCost))
            {
                playerStamina.ConsumeStamina(rollStaminaCost);
                nextJumpTime = Time.time + rollDuration; 
                StartCoroutine(RollRoutine());
            }
        }
        else if (!isJumping)
        {
            if (playerStamina != null && playerStamina.HasStamina(jumpStaminaCost))
            {
                playerStamina.ConsumeStamina(jumpStaminaCost);
                nextJumpTime = Time.time + jumpCooldown;
                StartCoroutine(JumpRoutine());
            }
        }
    }
}

IEnumerator RollRoutine()
{
    isRolling = true;
    
    if (combatScript != null) 
    {
        combatScript.isAttacking = false;
        animator.ResetTrigger("Attack"); 
    }

    animator.SetBool("isRolling", true);

    yield return new WaitForSeconds(rollDuration);
    isRolling = false;
}

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        if (animator != null) { animator.ResetTrigger("Jump"); animator.SetTrigger("Jump"); }
        yield return new WaitForSeconds(jumpDelay);
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        isJumping = false;
    }

    void HandleGliding()
    {
        if (isSwimming || inWaterBounds || Time.time < nextToggleTime) return;
        if (Input.GetButtonDown("Jump"))
        {
            if (!isGliding)
            {
                if (!isGrounded && velocity.y < -1.5f && !isJumping && groundDistance >= minGlideHeight)
                {
                    isGliding = true;
                    if (gliderObject != null) gliderObject.SetActive(true);
                    if (animator != null) animator.SetBool("isGliding", true);
                    if (playerSounds != null) playerSounds.PlayOpenGliderSound();
                    nextToggleTime = Time.time + glideToggleDelay;
                }
            }
            else { StopGliding(); nextToggleTime = Time.time + glideToggleDelay; }
        }
        if (isGliding && playerStamina != null)
        {
            playerStamina.ConsumeStamina(glideStaminaCost * Time.deltaTime);
            if (!playerStamina.HasStamina()) StopGliding();
        }
    }

    void StopGliding()
    {
        isGliding = false;
        if (gliderObject != null) gliderObject.SetActive(false);
        if (animator != null) animator.SetBool("isGliding", false);
        if (playerSounds != null) playerSounds.PlayOpenGliderSound();
    }

    void HandleWindSound()
    {
        if (playerSounds == null) return;
        if (isGliding) playerSounds.PlayWindSound(true);
        else if (!isGrounded && velocity.y < -5f && !isSwimming && groundDistance > 4f) playerSounds.PlayWindSound(false);
        else playerSounds.StopWindSound();
    }

    void ApplyMovement()
    {
        if (isSwimming)
        {
            float targetY = waterSurfaceY - swimFloatOffset;
            velocity.y = Mathf.Lerp(velocity.y, (targetY - transform.position.y) * floatSmoothness, Time.deltaTime * floatSmoothness);
        }
        else if (isGliding) velocity.y = glideFallSpeed;
        else velocity.y += gravity * Time.deltaTime;

        if (controller != null && controller.enabled)
            controller.Move((currentHorizontalMove + velocity) * Time.deltaTime);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", currentAnimSpeed, 0.1f, Time.deltaTime);
        animator.SetFloat("VerticalVelocity", velocity.y);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("GroundDistance", groundDistance);
        animator.SetBool("isSwimming", isSwimming);
        animator.SetBool("isRolling", isRolling);

    }

    private void ValidateSettings()
    {
        walkSpeed = Mathf.Max(0.1f, walkSpeed);
        runSpeed = Mathf.Max(walkSpeed, runSpeed);
        swimSpeed = Mathf.Max(0.1f, swimSpeed);
        glideSpeed = Mathf.Max(0.1f, glideSpeed);
        gravity = Mathf.Min(-1f, gravity);
        jumpHeight = Mathf.Max(0.1f, jumpHeight);
        minGlideHeight = Mathf.Max(1f, minGlideHeight);
        safeFallHeight = Mathf.Max(0f, safeFallHeight);
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        airMomentum = Vector3.zero;
    }

    public void ResetFallDamage()
    {
        highestYPosition = transform.position.y;
    }
}