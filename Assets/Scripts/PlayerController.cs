using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float swingForce = 15f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    
    [Header("Ice Pick Settings")]
    public Transform icePickTip;
    public float icePickLength = 2f;
    public LayerMask climbableLayer;
    public SpriteRenderer icePickSprite; // Reference to the ice pick sprite renderer
    public Transform icePickPivot; // Pivot point for the ice pick rotation
    
    private Rigidbody2D rb;
    private bool isClimbing = false;
    private bool icePickEquipped = false;
    private Vector2 mousePosition;
    private Vector2 swingDirection;
    private float swingPower;
    private bool isGrounded;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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
        // Check if player is grounded
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
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
    }
    
    private void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
    
    private void HandleIcePick()
    {
        // Calculate direction to mouse
        Vector2 directionToMouse = (mousePosition - (Vector2)transform.position).normalized;
        
        // Position ice pick tip
        icePickTip.position = transform.position + (Vector3)(directionToMouse * icePickLength);
        
        // Rotate the ice pick to point at the mouse
        if (icePickPivot != null)
        {
            float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
            icePickPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        if (Input.GetMouseButton(0))
        {
            // Check if we hit a climbable surface
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToMouse, icePickLength, climbableLayer);
            
            if (hit.collider != null)
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
    
    private void OnDrawGizmos()
    {
        // Draw ground check radius in editor
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
} 