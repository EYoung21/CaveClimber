using UnityEngine;
using System.Collections; // Needed for IEnumerator

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
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
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
    
    private void Update()
    {
        if (isAttacking) return; // Don't process movement/other actions during attack animation
        
        // Store previous grounded state
        bool wasGrounded = isGrounded;
        
        // Check if grounded
        CheckGrounded();
        
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

        // Periodically log state for debugging
        if (showDebugLogs)
        {
            debugTimer -= Time.deltaTime;
            if (debugTimer <= 0)
            {
                Debug.Log($"Is Grounded: {isGrounded}, Position: {transform.position}, Velocity: {rb.linearVelocity}");
                debugTimer = 1f;
            }
        }
        
        // Get horizontal input
        movement = Input.GetAxis("Horizontal") * movementSpeed;
        
        // --- Attack Input --- 
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(Attack());
        }
        // --- End Attack Input ---
        
        // Handle jump input
        if (Input.GetButtonDown("Jump"))
        {
            if(showDebugLogs) Debug.Log("Jump button pressed");
            if (isGrounded)
            {
                if(showDebugLogs) Debug.Log("Jumping!");
                // Apply jump force on space press
                Vector2 velocity = rb.linearVelocity;
                velocity.y = jumpForce;
                rb.linearVelocity = velocity;
            }
            else
            {
                if(showDebugLogs) Debug.Log("Cannot jump - not grounded");
            }
        }
        
        // Only handle standard animations if not attacking
        if (!isAttacking)
        {
            // Handle animations based on movement
            HandleAnimations();
        }
        
        // Handle screen wrapping
        WrapAroundScreen();
        
        // Check if player has fallen below camera view
        CheckIfOutOfCameraView();
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
    
    private void FixedUpdate()
    {
        // Apply horizontal movement ONLY if not attacking
        if (!isAttacking)
        {
            // Apply horizontal movement (Doodle Jump style)
            Vector2 velocity = rb.linearVelocity;
            velocity.x = movement;
            rb.linearVelocity = velocity;
        }
        else
        {
            // Optionally reduce horizontal velocity during attack
            // rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
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
        if (movement > 0.1f) lastDirection = "right";
        else if (movement < -0.1f) lastDirection = "left";
        
        // Animation handling based on velocity
        if (rb.linearVelocity.y > 0.1f) 
        {
            // Jumping/moving up
            animationLoop(jumpAnimation);
        } 
        else if (rb.linearVelocity.y < -0.1f) 
        {
            // Falling
            animationLoop(fallAnimaiton);
        }
        else 
        {
            // On platform or moving horizontally
            if (Mathf.Abs(movement) > 0.1f)
            {
                animationLoop(runAnimation);
            }
            else
            {
                animationLoop(idleAnimation);
            }
        }
        
        // Update sprite direction
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
            // Keep last direction when idle
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
        // Log collision info for debugging
        if (showDebugLogs)
        {
            Debug.Log($"Collision with: {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
            // Force a ground check immediately after collision
            CheckGrounded();
            Debug.Log($"After collision ground check: {isGrounded}");
        }
    }
    
    // Override collision resolution to prevent sticking to sides of platforms
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Only process for platform collisions
        if (collision.gameObject.CompareTag("Platform"))
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                
                // If collision is from the side (normal.y is near 0)
                if (Mathf.Abs(normal.y) < 0.1f)
                {
                    // Get the platform effector
                    PlatformEffector2D effector = collision.gameObject.GetComponent<PlatformEffector2D>();
                    
                    // If the platform has an effector, temporarily disable collision
                    if (effector != null && effector.useOneWay)
                    {
                        // Create a temporary non-collision state
                        Physics2D.IgnoreCollision(playerCollider, collision.collider, true);
                        
                        // Re-enable collision after a short delay
                        StartCoroutine(ReenableCollision(playerCollider, collision.collider));
                    }
                }
            }
        }
    }
    
    private IEnumerator ReenableCollision(Collider2D player, Collider2D platform)
    {
        // Wait a short time
        yield return new WaitForSeconds(0.2f);
        
        // Only re-enable collision if the player is above the platform
        if (player.bounds.min.y > platform.bounds.max.y - 0.1f)
        {
            Physics2D.IgnoreCollision(player, platform, false);
        }
        else
        {
            // Check again later if still beside the platform
            yield return new WaitForSeconds(0.2f);
            Physics2D.IgnoreCollision(player, platform, false);
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
            if(showDebugLogs) Debug.Log("Player fell out of camera view. Game Over!");
            gameManager.GameOver();
        }
    }
} 