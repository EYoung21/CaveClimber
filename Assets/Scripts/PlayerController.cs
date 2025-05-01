using UnityEngine;
using System.Collections; // Needed for IEnumerator
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 10f;
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f; // Set a default value that's not zero
    
    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public LayerMask enemyLayer; // Set this to your Enemy layer in inspector
    public float attackCooldown = 0.5f;
    public Sprite[] attackAnimation; // Assign attack sprites
    
    // These will be ignored but are kept to avoid errors in the scene
    [HideInInspector] public SpriteRenderer icePickSprite;
    [HideInInspector] public Transform icePickPivot;
    [HideInInspector] public Transform icePickTip;
    [HideInInspector] public float icePickLength;
    [HideInInspector] public float swingForce;
    [HideInInspector] public Transform groundCheck;
    [HideInInspector] public LayerMask climbableLayer;
    
    [Header("Animation")]
    public Sprite[] jumpAnimation;
    public Sprite[] runAnimation;
    public Sprite[] fallAnimaiton;
    public Sprite[] idleAnimation;
    public float animationFPS;

    [Header("Debug")]
    public bool showDebugLogs = false; // Defaulting debug logs off now

    // Screen wrapping
    private float screenHalfWidth;
    private float playerHalfWidth;

    SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private bool isGrounded;
    private string lastDirection;
    private float movement = 0f;

    int currentFrame;
    float animationTimer;
    private float debugTimer = 0f;
    
    private CameraFollow cameraFollow; // Reference to the camera follow script
    private bool hasLandedOnce = false; // Track if player has landed at least once
    
    // Attack state
    private bool isAttacking = false;
    private float lastAttackTime = -1f;
    
    private Camera mainCamera;
    private GameManager gameManager;
    
    [Header("Power-up Settings")]
    public float defaultJumpForce = 10f; // Store the default jump force
    public TextMeshProUGUI jumpBoostTimerText; // Reference to the timer UI
    
    // Power-up state
    private bool jumpBoostActive = false;
    private float jumpBoostTimeRemaining = 0f;
    private float currentJumpBoostMultiplier = 1f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Store the default jump force value
        defaultJumpForce = jumpForce;
        
        // Hide the jump boost timer initially
        if (jumpBoostTimerText != null)
        {
            jumpBoostTimerText.gameObject.SetActive(false);
        }
        
        // Find the CameraFollow script in the scene
        cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogError("Player could not find CameraFollow script on the main camera!");
        }
        
        // Get reference to the main camera and game manager
        mainCamera = Camera.main;
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("Could not find GameManager instance!");
        }

        currentFrame = 0;
        animationTimer = 1f / animationFPS;
        lastDirection = "right";
        
        // Setup for screen wrapping
        screenHalfWidth = Camera.main.aspect * Camera.main.orthographicSize;
        playerHalfWidth = GetComponent<Renderer>().bounds.extents.x;
        
        // Make sure we have a valid ground check radius
        if (groundCheckRadius <= 0)
        {
            groundCheckRadius = 0.2f;
            if(showDebugLogs) Debug.LogWarning("Ground check radius was 0, set to default 0.2");
        }
        
        // Log initial state
        if(showDebugLogs) Debug.Log($"Player initialized. Ground Layer: {LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2)))}");
        if(showDebugLogs) Debug.Log($"Ground Check Radius: {groundCheckRadius}");
        
        // Ensure player is not on the Ground layer
        if (gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Debug.LogError("Player is on the Ground layer! This will cause issues with ground detection.");
        }
        
        // Setup platform collision handling
        SetupCollisionDetection();
    }
    
    private void SetupCollisionDetection()
    {
        // Set up physics 2D to ignore collisions between player and platforms when moving horizontally
        if (rb != null && playerCollider != null)
        {
            // Get the player's collision detection mode and make sure it's continuous
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            // Log setup for debugging
            if (showDebugLogs)
            {
                Debug.Log($"Player collision setup - Layer: {gameObject.layer}, Ground Layer: {Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2))}");
            }
        }
    }
    
    private void CheckGrounded()
    {
        // Alternative ground check method - use raycast down from slightly above the bottom
        float raycastDistance = groundCheckRadius * 2;
        Vector2 rayStart = new Vector2(transform.position.x, transform.position.y - playerCollider.bounds.extents.y + 0.1f);
        
        // Try multiple raycasts - one in center, one slightly left, one slightly right
        bool hitGround = Physics2D.Raycast(rayStart, Vector2.down, raycastDistance, groundLayer) ||
                         Physics2D.Raycast(rayStart + new Vector2(-0.2f, 0), Vector2.down, raycastDistance, groundLayer) ||
                         Physics2D.Raycast(rayStart + new Vector2(0.2f, 0), Vector2.down, raycastDistance, groundLayer);
        
        // Set grounded state
        isGrounded = hitGround;
        
        // Draw debug rays
        if (showDebugLogs)
        {
            Debug.DrawRay(rayStart, Vector2.down * raycastDistance, hitGround ? Color.green : Color.red);
            Debug.DrawRay(rayStart + new Vector2(-0.2f, 0), Vector2.down * raycastDistance, hitGround ? Color.green : Color.red);
            Debug.DrawRay(rayStart + new Vector2(0.2f, 0), Vector2.down * raycastDistance, hitGround ? Color.green : Color.red);
        }
        
        // Additional debug info for ground check
        if (showDebugLogs && Input.GetButtonDown("Jump"))
        {
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, raycastDistance, groundLayer);
            if (hit.collider != null)
            {
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name} at distance {hit.distance}");
            }
            else
            {
                Debug.Log("Raycast didn't hit anything");
        }
    }
    }
    
    // Re-introduced Update for reliable input polling
    private void Update()
    {
        // Handle jump boost timer
        if (jumpBoostActive)
        {
            jumpBoostTimeRemaining -= Time.deltaTime;
            
            // Update timer UI
            if (jumpBoostTimerText != null)
            {
                jumpBoostTimerText.text = Mathf.Ceil(jumpBoostTimeRemaining).ToString();
            }
            
            // Check if boost has expired
            if (jumpBoostTimeRemaining <= 0)
            {
                DeactivateJumpBoost();
            }
        }
        
        // Attack Input Check (moved back from FixedUpdate)
        // Added debug log inside the check
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            if(showDebugLogs) Debug.Log("Attack input detected in Update!");
            StartCoroutine(Attack());
        }
        
        // Other per-frame logic could go here if needed, but keep physics-related stuff in FixedUpdate
    }
    
    private void FixedUpdate()
    {
        // Check grounded state (for animations etc.)
        bool wasGrounded = isGrounded;
        CheckGrounded();
        
        // --- Unparent if player becomes not grounded --- 
        if (wasGrounded && !isGrounded && transform.parent != null)
        {
            transform.parent = null;
            if (showDebugLogs) Debug.Log("Player became ungrounded, unparented from platform.");
        }
        // --- End modification --- 
        
        // Trigger camera follow on first landing
        if (!wasGrounded && isGrounded && !hasLandedOnce)
        {
            hasLandedOnce = true;
            if (cameraFollow != null)
            {
                cameraFollow.StartFollowing();
            }
            if(showDebugLogs) Debug.Log("Player landed for the first time.");
        }
        
        // Process movement input regardless of attack state
        movement = Input.GetAxis("Horizontal") * movementSpeed;
    
        // Apply horizontal movement (Doodle Jump style)
        // Allow horizontal velocity change even during attack?
        // Or maybe freeze horizontal movement during attack animation?
        // Let's allow it for now, attack animation takes priority for visuals.
        Vector2 velocity = rb.linearVelocity;
        velocity.x = movement;
        rb.linearVelocity = velocity;
        
        // Handle standard animations (Attack coroutine will override if active)
        HandleAnimations();
        
        // Handle screen wrapping (moved from Update)
        WrapAroundScreen();
        
        // Check if player has fallen below camera view (moved from Update)
        CheckIfOutOfCameraView();
        
        // Periodically log state for debugging (moved from Update)
        if (showDebugLogs)
        {
            debugTimer -= Time.fixedDeltaTime; // Use fixedDeltaTime in FixedUpdate
            if (debugTimer <= 0)
            {
                Debug.Log($"Is Grounded: {isGrounded}, Position: {transform.position}, Velocity: {rb.linearVelocity}");
                debugTimer = 1f;
            }
        }
    }
    
    private void WrapAroundScreen()
    {
        // Wrap player around screen edges
        Vector3 pos = transform.position;
        
        if (pos.x + playerHalfWidth < -screenHalfWidth)
        {
            pos.x = screenHalfWidth + playerHalfWidth;
        }
        else if (pos.x - playerHalfWidth > screenHalfWidth)
        {
            pos.x = -screenHalfWidth - playerHalfWidth;
        }
        
        transform.position = pos;
    }

    private void HandleAnimations()
    {
        // Determine current direction based on input/velocity if needed
        // We still need this for flipping the sprite correctly
        if (movement > 0.1f) lastDirection = "right";
        else if (movement < -0.1f) lastDirection = "left";
        
        // Animation handling based on velocity
        // MODIFIED: Use jump anim for positive Y vel, run anim for negative Y vel
        if (rb.linearVelocity.y > 0.1f) 
        {   
            // Moving up
            animationLoop(jumpAnimation); 
        } 
        else if (rb.linearVelocity.y < -0.1f) 
            {
            // Falling - Use Run Animation as requested
            animationLoop(runAnimation); // << CHANGED FROM fallAnimation
        }
        else 
        {   
            // On ground or very small Y velocity
            if (Mathf.Abs(movement) > 0.1f)
            {
                // Use Run animation if moving horizontally on ground
                animationLoop(runAnimation);
            }
            else
            {
                 // Use Idle animation if still on ground
                animationLoop(idleAnimation);
            }
            }
            
        // Update sprite direction based on actual velocity or last input direction
            if (rb.linearVelocity.x < -0.1f)
            {
                spriteRenderer.flipX = true;
                lastDirection = "left";
            }
            else if (rb.linearVelocity.x > 0.1f)
            {
                spriteRenderer.flipX = false;
                lastDirection = "right";
            }
            else
            {
            // Horizontal velocity is near zero, use the last known movement input direction
                if (lastDirection == "left") spriteRenderer.flipX = true;
                else spriteRenderer.flipX = false;
            }
    }

    private void animationLoop(Sprite[] animationArray) 
    {
        if (animationArray == null || animationArray.Length == 0) return;
        if (isAttacking && animationArray != attackAnimation) return; // Don't override attack animation

        animationTimer -= Time.deltaTime;
        if (animationTimer <= 0) 
        {
            animationTimer = 1f / animationFPS;
            currentFrame = (currentFrame + 1) % animationArray.Length;
            // Check bounds just in case
            if (currentFrame >= 0 && currentFrame < animationArray.Length)
            {
                spriteRenderer.sprite = animationArray[currentFrame];
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if colliding with the ground layer
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            // Check if the collision came from above (landing)
            bool landedOnTop = false;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // A normal pointing upwards (positive Y) indicates collision on the top surface
                if (contact.normal.y > 0.5f) 
                {
                    landedOnTop = true;
                    break; // Found a top contact, no need to check others
                }
            }

            if (landedOnTop)
            {
                // --- Handle Parenting --- 
                MovingPlatformMarker movingPlatform = collision.gameObject.GetComponent<MovingPlatformMarker>();
                if (movingPlatform != null)
                {
                    transform.parent = collision.transform;
                    if (showDebugLogs) Debug.Log($"Player landed on moving platform {collision.gameObject.name}, parenting.", collision.gameObject);
                }
                else
                {
                    // Ensure unparented if landing on a non-moving platform
                    transform.parent = null;
                }
                // --- End Parenting --- 

                // --- Prevent jumping off breaking platforms --- 
                BreakingPlatform breakingPlatform = collision.gameObject.GetComponent<BreakingPlatform>();
                if (breakingPlatform != null)
                {
                    if(showDebugLogs) Debug.Log("Landed on a BreakingPlatform, preventing auto-jump.");
                    // Make sure we unparent even from breaking platforms if somehow parented
                    transform.parent = null; 
                }
                else
                {
                    // Apply automatic jump force upon landing on normal platforms
                    if(showDebugLogs) Debug.Log($"Auto-Jumping off {collision.gameObject.name}");
                    // --- Unparent before jumping --- 
                    transform.parent = null;
                    // --- End unparent --- 
                    Vector2 velocity = rb.linearVelocity;
                    velocity.y = jumpForce;
                    rb.linearVelocity = velocity;
                }
                // --- End modification --- 
                
                // Optional: Trigger first landing camera follow here as well?
                // It's currently triggered in FixedUpdate based on isGrounded state change,
                // which should still work fine after this collision sets things up.
            }
        }
        
        // Handle enemy collision (keep existing logic if it was here)
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
             // Add enemy collision logic here if it was removed from Update
             EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
             if (enemy != null)
             {
                 // Example: Apply knockback
                 Vector2 knockbackDirection = (transform.position - enemy.transform.position).normalized;
                 rb.AddForce(knockbackDirection * 5f, ForceMode2D.Impulse); // Adjust force as needed
                 // Maybe trigger game over or damage?
                 // gameManager?.GameOver(); 
            }
        }
    }
    
    // Override collision resolution to prevent sticking to sides of platforms
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Only process for platform collisions
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            bool hasSideContact = false;
            bool hasTopContact = false;
            
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                Vector2 normal = contact.normal;
                
                // Check for top contact (normal pointing up)
                if (normal.y > 0.7f)
                {
                    hasTopContact = true;
                }
                
                // Check for side contact (normal mostly horizontal)
                if (Mathf.Abs(normal.x) > 0.7f && Mathf.Abs(normal.y) < 0.3f)
                {
                    hasSideContact = true;
                }
            }
            
            // If we have side contacts but no top contacts, the player is likely
            // colliding with the side of a platform
            if (hasSideContact && !hasTopContact)
            {
                // Get the platform's collider
                Collider2D platformCollider = collision.collider;
                
                // Temporarily ignore collision with this platform
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
                    
                    // Set up to re-enable collision later
                    StartCoroutine(ReenableCollision(playerCollider, platformCollider));
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"Ignoring side collision with {collision.gameObject.name}");
                    }
                }
            }
        }
    }
    
    private IEnumerator ReenableCollision(Collider2D player, Collider2D platform)
    {
        // Wait a short time before checking relative positions
        yield return new WaitForSeconds(0.2f);
        
        // Get the player's position and velocity
        Vector2 playerPos = player.transform.position;
        Vector2 platformPos = platform.transform.position;
        Vector2 playerVel = rb.linearVelocity;
        
        // Check if player is clearly above the platform
        bool isAbovePlatform = player.bounds.min.y > platform.bounds.max.y - 0.05f;
        
        // Check if player is moving away from the platform horizontally
        bool isMovingAway = (playerPos.x < platformPos.x && playerVel.x < -0.1f) || 
                           (playerPos.x > platformPos.x && playerVel.x > 0.1f);
        
        // Only re-enable collision if:
        // 1. Player is clearly above the platform, or
        // 2. Player has moved sufficiently away from the platform
        if (isAbovePlatform || isMovingAway)
        {
            if (showDebugLogs) Debug.Log($"Re-enabling collision with platform: Above={isAbovePlatform}, MovingAway={isMovingAway}");
            Physics2D.IgnoreCollision(player, platform, false);
        }
        else
        {
            // If the player is still beside or inside the platform, wait longer
            yield return new WaitForSeconds(0.3f);
            
            // If the player is now clearly above or has moved away, re-enable
            isAbovePlatform = player.bounds.min.y > platform.bounds.max.y - 0.05f;
            isMovingAway = (playerPos.x < platformPos.x && playerVel.x < -0.1f) || 
                           (playerPos.x > platformPos.x && playerVel.x > 0.1f);
            
            if (isAbovePlatform || isMovingAway || Vector2.Distance(playerPos, platformPos) > 1.5f)
            {
                if (showDebugLogs) Debug.Log($"Delayed re-enabling collision with platform");
                Physics2D.IgnoreCollision(player, platform, false);
            }
            else
            {
                // If still overlapping, keep collision disabled longer
                yield return new WaitForSeconds(0.5f);
                Physics2D.IgnoreCollision(player, platform, false);
            }
        }
    }
    
#if UNITY_EDITOR
    // Debug visualization for ground check
    private void OnDrawGizmos()
    {
        if (playerCollider != null)
        {
            // Draw the ground check rays
            Vector2 rayStart = new Vector2(transform.position.x, transform.position.y - playerCollider.bounds.extents.y + 0.1f);
            float raycastDistance = groundCheckRadius * 2;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayStart, rayStart + Vector2.down * raycastDistance);
            Gizmos.DrawLine(rayStart + new Vector2(-0.2f, 0), rayStart + new Vector2(-0.2f, 0) + Vector2.down * raycastDistance);
            Gizmos.DrawLine(rayStart + new Vector2(0.2f, 0), rayStart + new Vector2(0.2f, 0) + Vector2.down * raycastDistance);
        }
        
        // Draw attack raycast when debug enabled
        if (showDebugLogs && Application.isPlaying) // Only draw when playing
        {
             Vector2 attackDir = (lastDirection == "right") ? Vector2.right : Vector2.left;
             Gizmos.color = Color.red;
             Gizmos.DrawLine(transform.position, (Vector2)transform.position + attackDir * attackRange);
            }
        }
#endif

    IEnumerator Attack()
    {
        // Added debug log at the start of the coroutine
        if(showDebugLogs) Debug.Log("Attack() coroutine started.");
        
        isAttacking = true;
        lastAttackTime = Time.time;
        if(showDebugLogs) Debug.Log("Player attacking!");

        // Determine attack direction
        Vector2 attackDir = (lastDirection == "right") ? Vector2.right : Vector2.left;
        spriteRenderer.flipX = (lastDirection == "left"); // Ensure sprite faces attack direction

        // --- Play Attack Animation --- 
        if (attackAnimation != null && attackAnimation.Length > 0)
        {
            float frameDuration = 1f / animationFPS;
            for (int i = 0; i < attackAnimation.Length; i++)
            {
                spriteRenderer.sprite = attackAnimation[i];
                // --- Perform Raycast during a specific frame (e.g., midway) --- 
                if (i == attackAnimation.Length / 2) // Adjust frame index if needed
                {
                    PerformAttackRaycast(attackDir);
                }
                yield return new WaitForSeconds(frameDuration);
            }
        }
        else
        {
            // If no animation, perform raycast immediately
            PerformAttackRaycast(attackDir);
            yield return new WaitForSeconds(0.1f); // Small delay even without animation
        }
        // --- End Attack Animation --- 

        isAttacking = false;
        if(showDebugLogs) Debug.Log("Attack() coroutine finished, isAttacking set to false.");
        // Reset to appropriate animation after attacking (e.g., idle/run/fall)
        HandleAnimations(); 
    }

    void PerformAttackRaycast(Vector2 direction)
    {
        if(showDebugLogs) Debug.DrawRay(transform.position, direction * attackRange, Color.red, 0.5f);
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, attackRange, enemyLayer);
        
        if (hit.collider != null)
        {
            if(showDebugLogs) Debug.Log($"Attack hit: {hit.collider.name}");
            EnemyController enemy = hit.collider.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage();
            }
        }
        else
        {
             if(showDebugLogs) Debug.Log("Attack missed.");
        }
    }
    
    private void CheckIfOutOfCameraView()
    {
        if (mainCamera == null || gameManager == null) return;
        
        float cameraBottomY = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        // If player is completely below camera view, trigger game over immediately
        if (transform.position.y + playerCollider.bounds.extents.y < cameraBottomY)
        {
            // if(showDebugLogs) Debug.Log("Player fell out of camera view. Game Over!");
            gameManager.GameOver();
        }
    }

    // Call this method to apply a jump boost
    public void ApplyJumpBoost(float multiplier, float duration)
    {
        // Set the jump force to the boosted value
        jumpForce = defaultJumpForce * multiplier;
        
        // Store the boost multiplier for reference
        currentJumpBoostMultiplier = multiplier;
        
        // Set timer
        jumpBoostTimeRemaining = duration;
        jumpBoostActive = true;
        
        // Show the timer UI
        if (jumpBoostTimerText != null)
        {
            jumpBoostTimerText.gameObject.SetActive(true);
            jumpBoostTimerText.color = Color.red;
            jumpBoostTimerText.text = Mathf.Ceil(jumpBoostTimeRemaining).ToString();
        }
        
        if (showDebugLogs) Debug.Log($"Jump boost activated! Multiplier: {multiplier}, Duration: {duration}s");
    }
    
    // Deactivate the jump boost effect
    private void DeactivateJumpBoost()
    {
        // Reset jump force to default
        jumpForce = defaultJumpForce;
        
        // Reset state
        jumpBoostActive = false;
        jumpBoostTimeRemaining = 0f;
        currentJumpBoostMultiplier = 1f;
        
        // Hide the timer UI
        if (jumpBoostTimerText != null)
        {
            jumpBoostTimerText.gameObject.SetActive(false);
        }
        
        if (showDebugLogs) Debug.Log("Jump boost deactivated");
    }
} 