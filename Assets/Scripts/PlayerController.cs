using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float swingForce = 15f;
    public LayerMask groundLayer;
    
    [Header("Ice Pick Settings")]
    public Transform icePickTipLeft;
    public Transform icePickTipRight;
    public float icePickLength = 2f;
    public LayerMask climbableLayer;
    public SpriteRenderer icePickSprite; // Reference to the ice pick sprite renderer

    SpriteRenderer spriteRenderer;
    public Transform icePickPivot; // Pivot point for the ice pick rotation
    
    private Rigidbody2D rb;
    private bool isClimbing = false;
    private bool icePickEquipped = false;
    private Vector2 mousePosition;
    private Vector2 swingDirection;
    private float swingPower;
    private bool isGrounded;
    private float groundCheckDistance = 0.1f; // Distance for the ground check raycast
    private Collider2D playerCollider; // Store the player's collider

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
        playerCollider = GetComponent<Collider2D>(); // Get the player's collider
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentFrame = 0;
        animationTimer = 1f / animationFPS;

        lastDirection = "right";
        
        // Initially hide the ice pick
        if (icePickSprite != null)
        {
            icePickSprite.enabled = false;
        }
        
        // Ensure Rigidbody2D settings are correct
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
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
        
        // Handle movement regardless of ice pick state
        HandleMovement();
        
        // Handle ice pick if equipped
        if (icePickEquipped)
        {
            HandleIcePick();
        }

        //animations handled
        if (isGrounded) {
            if (rb.linearVelocity.x < 0) {
                animationLoop(runAnimation);
                spriteRenderer.flipX = true;
                lastDirection = "left";
            } else if (rb.linearVelocity.x > 0) {
                animationLoop(runAnimation);
                spriteRenderer.flipX = false;
                lastDirection = "right";
            } else {
                animationLoop(idleAnimation);
            }
        } else {
            if (rb.linearVelocity.y > 0) {
                animationLoop(jumpAnimation);
            } else {
                animationLoop(fallAnimaiton);
            }

            if (rb.linearVelocity.x < 0) {
                spriteRenderer.flipX = true;
                lastDirection = "left";
            } else if (rb.linearVelocity.x > 0) {
                spriteRenderer.flipX = false;
                lastDirection = "right";
            }
        }
    }

    private void animationLoop(Sprite[] animationArray) {
        animationTimer -= Time.deltaTime;
        if (animationTimer < 0) {
            animationTimer = 1f / animationFPS;
            currentFrame++;
            if (currentFrame >= animationArray.Length) {
                currentFrame = 0;
            }
            spriteRenderer.sprite = animationArray[currentFrame];
        }

    }
    
    private void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            Debug.Log("Jump Triggered!"); // DEBUG: Log jump event
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
    
    private void HandleIcePick()
    {
        // Calculate direction to mouse
        Vector2 directionToMouse = (mousePosition - (Vector2)transform.position).normalized;
        
        // Position ice pick tip - REMOVED as tips are now children of pivot
        // icePickTip.position = transform.position + (Vector3)(directionToMouse * icePickLength);
        
        // Rotate the ice pick to point at the mouse
        if (icePickPivot != null)
        {
            float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
            // Subtract 90 degrees because the default sprite orientation points UP
            icePickPivot.rotation = Quaternion.Euler(0, 0, angle - 90f); 
        }
        
        if (Input.GetMouseButton(0))
        {
            // Check if either tip hits a climbable surface
            bool hitLeft = false;
            bool hitRight = false;
            
            if (icePickTipLeft != null)
            {
                hitLeft = Physics2D.OverlapPoint(icePickTipLeft.position, climbableLayer);
            }
            if (icePickTipRight != null)
            {
                hitRight = Physics2D.OverlapPoint(icePickTipRight.position, climbableLayer);
            }
            
            if (hitLeft || hitRight) // Climb if either tip hits
            {
                isClimbing = true;
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
                
                // Calculate swing power based on mouse movement
                swingDirection = (mousePosition - (Vector2)transform.position).normalized;
                swingPower = Vector2.Distance(mousePosition, (Vector2)transform.position);
            }
        }
        else
        {
            if (isClimbing)
            {
                // Launch player based on swing
                rb.gravityScale = 1f;
                rb.AddForce(swingDirection * swingPower * swingForce, ForceMode2D.Impulse);
                isClimbing = false;
            }
        }
    }
} 