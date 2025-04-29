using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    public float crawlSpeed = 2.5f; // Speed while crawling
    
    // These will be ignored but are kept to avoid errors in the scene
    [HideInInspector] public SpriteRenderer icePickSprite;
    [HideInInspector] public Transform icePickPivot;
    [HideInInspector] public Transform icePickTip;
    [HideInInspector] public float icePickLength;
    [HideInInspector] public float swingForce;
    [HideInInspector] public Transform groundCheck;
    [HideInInspector] public float groundCheckRadius;
    [HideInInspector] public LayerMask climbableLayer;
    
    [Header("Animation")]
    public Sprite[] jumpAnimation;
    public Sprite[] runAnimation;
    public Sprite[] fallAnimaiton;
    public Sprite[] idleAnimation;
    public Sprite[] crawlAnimation; // Animation for crawling
    public Sprite duckSprite; // Single sprite for idle crawling/ducking
    public float animationFPS;

    SpriteRenderer spriteRenderer;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private float groundCheckDistance = 0.1f; // Distance for the ground check raycast
    private Collider2D playerCollider;
    private bool isCrawling = false; // Track crawling state

    int currentFrame;
    float animationTimer;
    private string lastDirection;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentFrame = 0;
        animationTimer = 1f / animationFPS;

        lastDirection = "right";
        
        // Ensure Rigidbody2D settings are correct
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.gravityScale = 1f; // Ensure gravity is on by default
        }
    }
    
    private void Update()
    {
        // Check for crawl toggle
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrawl();
        }

        // Check if player is grounded using Raycast
        // Calculate ray origin slightly below the player center based on collider bounds
        float raycastOriginOffsetY = playerCollider.bounds.extents.y + 0.05f;
        Vector2 raycastOrigin = (Vector2)transform.position - new Vector2(0, raycastOriginOffsetY);
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, groundLayer);
        
        isGrounded = hit.collider != null;
        Debug.Log($"Is Grounded: {isGrounded}"); // DEBUG: Log grounded status
        Debug.DrawRay(raycastOrigin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red); // Visualize the raycast

        // Handle movement
        HandleMovement();
        
        // Handle animations
        HandleAnimations();
    }

    private void HandleAnimations()
    {
        // If crawling, use crawl animation or duck sprite if idle
        if (isCrawling)
        {
            // Check if idle (not moving)
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                // Use duck sprite when idle in crawl mode
                spriteRenderer.sprite = duckSprite;
            }
            else
            {
                // Use crawl animation when moving
                animationLoop(crawlAnimation);
            }
            
            // Set facing direction
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
                // Keep last facing direction when idle
                if (lastDirection == "left") spriteRenderer.flipX = true;
                else spriteRenderer.flipX = false;
            }
            return;
        }

        // Standard animation handling (when not crawling)
        if (isGrounded) {
            if (rb.linearVelocity.x < -0.1f) {
                animationLoop(runAnimation);
                spriteRenderer.flipX = true;
                lastDirection = "left";
            } else if (rb.linearVelocity.x > 0.1f) {
                animationLoop(runAnimation);
                spriteRenderer.flipX = false;
                lastDirection = "right";
            } else {
                animationLoop(idleAnimation);
                // Keep last facing direction when idle
                if (lastDirection == "left") spriteRenderer.flipX = true;
                else spriteRenderer.flipX = false;
            }
        } else { // In air
            if (rb.linearVelocity.y > 0.1f) {
                animationLoop(jumpAnimation);
            } else if (rb.linearVelocity.y < -0.1f) {
                 animationLoop(fallAnimaiton);
            } else {
                animationLoop(fallAnimaiton);
            }

            // Update facing direction based on horizontal velocity even in air
            if (rb.linearVelocity.x < -0.1f) {
                spriteRenderer.flipX = true;
                lastDirection = "left";
            } else if (rb.linearVelocity.x > 0.1f) {
                spriteRenderer.flipX = false;
                lastDirection = "right";
            } else {
                // Keep last facing direction if horizontal velocity is near zero
                if (lastDirection == "left") spriteRenderer.flipX = true;
                else spriteRenderer.flipX = false;
            }
        }
    }

    private void animationLoop(Sprite[] animationArray) {
        if (animationArray == null || animationArray.Length == 0) return; // Safety check

        animationTimer -= Time.deltaTime;
        if (animationTimer <= 0) {
            animationTimer = 1f / animationFPS;
            currentFrame = (currentFrame + 1) % animationArray.Length; // Simplified looping
            spriteRenderer.sprite = animationArray[currentFrame];
        }
    }
    
    private void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = isCrawling ? crawlSpeed : moveSpeed;
        
        // Apply horizontal velocity regardless of grounded state
        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
        
        // Only allow jumping if grounded and not crawling
        if (isGrounded && !isCrawling && Input.GetButtonDown("Jump"))
        {
            Debug.Log("Jump Triggered!"); 
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
    
    private void ToggleCrawl()
    {
        isCrawling = !isCrawling;
        
        // Reset animation frame when toggling
        currentFrame = 0;
        Debug.Log($"Crawl mode: {isCrawling}");
    }
} 