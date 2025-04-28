using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    
    [Header("Ice Pick Settings")]
    public SpriteRenderer icePickSprite; // Reference to the ice pick sprite renderer

    SpriteRenderer spriteRenderer;
    public Transform icePickPivot; // Pivot point for the ice pick rotation
    
    private Rigidbody2D rb;
    private bool icePickEquipped = false;
    private Vector2 mousePosition;
    private bool isGrounded;
    private float groundCheckDistance = 0.1f; // Distance for the ground check raycast
    private Collider2D playerCollider;

    public Sprite[] jumpAnimation;
    public Sprite[] runAnimation;
    public Sprite[] fallAnimaiton;
    public Sprite[] idleAnimation;


    public float animationFPS;
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
        
        // Initially hide the ice pick
        if (icePickSprite != null)
        {
            icePickSprite.enabled = icePickEquipped;
        }
        
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
        // Check if player is grounded using Raycast
        // Calculate ray origin slightly below the player center based on collider bounds
        float raycastOriginOffsetY = playerCollider.bounds.extents.y + 0.05f;
        Vector2 raycastOrigin = (Vector2)transform.position - new Vector2(0, raycastOriginOffsetY);
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, groundLayer);
        
        isGrounded = hit.collider != null;
        Debug.Log($"Is Grounded: {isGrounded}"); // DEBUG: Log grounded status
        Debug.DrawRay(raycastOrigin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red); // Visualize the raycast

        // Toggle ice pick
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            icePickEquipped = !icePickEquipped;
            if (icePickSprite != null)
            {
                icePickSprite.enabled = icePickEquipped;
            }
        }
        
        // Get mouse position
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Handle movement only when grounded
        HandleMovement();
        
        // Handle ice pick rotation if equipped
        if (icePickEquipped)
        {
            HandleIcePickRotation(); // Renamed for clarity
        }

        // Animations handled (no changes needed here)
        if (isGrounded) {
            if (rb.linearVelocity.x < -0.1f) { // Added small threshold
                animationLoop(runAnimation);
                spriteRenderer.flipX = true;
                lastDirection = "left";
            } else if (rb.linearVelocity.x > 0.1f) { // Added small threshold
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
            if (rb.linearVelocity.y > 0.1f) { // Added small threshold
                animationLoop(jumpAnimation);
            } else if (rb.linearVelocity.y < -0.1f) { // Added small threshold
                 animationLoop(fallAnimaiton);
            } else {
                // Potentially keep falling animation if Y velocity is near zero but not grounded?
                // Or switch to a specific 'mid-air idle'? For now, let's keep falling.
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
        
        // Apply horizontal velocity regardless of grounded state
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        
        // Only allow jumping if grounded
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            Debug.Log("Jump Triggered!"); 
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
    
    // Renamed from HandleIcePick
    private void HandleIcePickRotation()
    {
        // Calculate direction to mouse relative to the pivot point for better accuracy
        Vector2 directionToMouse = (mousePosition - (Vector2)icePickPivot.position).normalized;
        
        // Rotate the ice pick pivot to point the ice pick towards the mouse
        if (icePickPivot != null)
        {
            float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
            // Adjust the angle offset (-90f) based on your ice pick sprite's default orientation.
            // If your sprite points right by default, use 0f. If it points up, use -90f.
            icePickPivot.rotation = Quaternion.Euler(0, 0, angle - 90f); 
        }
        
        // REMOVED: All climbing, latching, and swinging logic.
        // Physics interactions are now handled by Unity's physics engine 
        // based on the Colliders and Rigidbodies involved.
    }
} 