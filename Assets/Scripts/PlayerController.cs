using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 10f;
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f; // Set a default value that's not zero
    
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
    }
    
    private void Update()
    {
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
        
        // Handle animations based on movement
        HandleAnimations();
        
        // Check for attack input
        if (Input.GetMouseButtonDown(0))
        {
            // Implement attack logic if needed
        }
        
        // Handle screen wrapping
        WrapAroundScreen();
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
        // Apply horizontal movement (Doodle Jump style)
        Vector2 velocity = rb.linearVelocity;
        velocity.x = movement;
        rb.linearVelocity = velocity;
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
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
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

        animationTimer -= Time.deltaTime;
        if (animationTimer <= 0) 
        {
            animationTimer = 1f / animationFPS;
            currentFrame = (currentFrame + 1) % animationArray.Length;
            spriteRenderer.sprite = animationArray[currentFrame];
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
    }
#endif
} 