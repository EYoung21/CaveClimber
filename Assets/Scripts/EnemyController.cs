using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float moveSpeed = 2f;
    public float offScreenDestroyOffset = 15f; // How far below camera enemy must be to be destroyed
    public bool debugMode = true; // Enable debugging by default
    
    [Header("Visual Effects")]
    public float damageBlinkDuration = 0.2f; // How long the enemy blinks red before being destroyed
    public Color damageColor = new Color(1f, 0.5f, 0.5f, 0.8f); // Bright red with slight transparency
    
    [Header("Animation (Optional)")]
    public Sprite[] runAnimation;
    public float animationFPS = 10f;

    private Transform playerTarget;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool facingRight = false; // Tracks enemy direction
    private Camera mainCamera;
    private bool isActivated = false; // Track if enemy is activated
    private Collider2D enemyCollider; // Reference to the enemy's collider
    private Color originalColor; // Store the original sprite color
    
    // Animation state
    private int currentFrame;
    private float animationTimer;
    private float debugTimer = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        enemyCollider = GetComponent<Collider2D>();
        lastPosition = transform.position;
        
        // Store original sprite color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Create and apply a frictionless physics material to prevent player sticking
        ApplyFrictionlessPhysics();
        
        // Initially disable the enemy
        SetEnemyActive(false);
        
        // Try multiple ways to find the player
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) {
            // Try finding by name
            playerObject = GameObject.Find("Player");
        }
        
        if (playerObject != null) {
            playerTarget = playerObject.transform;
            Debug.Log($"{gameObject.name}: Found Player at {playerTarget.position}");
        }
        else {
            Debug.LogWarning($"{gameObject.name}: Player not found on Start - will keep looking.");
        }
        
        // Configure rigidbody if needed
        if (rb != null) {
            // Ensure enemy can move
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 1f; // Make sure gravity is on
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        } else {
            Debug.LogError($"{gameObject.name}: NO RIGIDBODY ATTACHED!");
        }
        
        animationTimer = 1f / animationFPS;
        currentFrame = 0;
    }

    void Update()
    {
        // Check if the enemy should be activated
        CheckVisibility();
        
        // If enemy is not activated, don't process further
        if (!isActivated)
            return;
            
        // Periodically show debug info
        if (debugMode) {
            debugTimer -= Time.deltaTime;
            if (debugTimer <= 0f) {
                Vector3 movement = transform.position - lastPosition;
                Debug.Log($"{gameObject.name}: Pos={transform.position}, Movement={movement.magnitude}, RB={rb != null}, Player={playerTarget != null}");
                lastPosition = transform.position;
                debugTimer = 2f;
            }
        }
        
        // Constantly try to find player if needed
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) {
                playerTarget = playerObject.transform;
                if (debugMode) Debug.Log($"{gameObject.name}: Found player!");
            }
            else return; // Exit if player still not found
        }

        // Only proceed if we have a player target
        if (playerTarget != null) {
            MoveTowardsPlayer();
            // Always handle running animation
            Animate();
        }
        
        // Check if fallen off screen
        if (mainCamera != null && transform.position.y < mainCamera.transform.position.y - offScreenDestroyOffset)
        {
            Debug.Log($"{gameObject.name} fell off screen and was destroyed.");
            Destroy(gameObject);
        }
    }
    
    void CheckVisibility()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (mainCamera == null)
            return;
            
        // Calculate the camera's view bounds
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        Vector2 cameraMin = (Vector2)mainCamera.transform.position - new Vector2(cameraWidth/2, cameraHeight/2);
        Vector2 cameraMax = (Vector2)mainCamera.transform.position + new Vector2(cameraWidth/2, cameraHeight/2);
        
        // Check if enemy is within camera bounds
        bool isVisible = 
            transform.position.x >= cameraMin.x - 1f && 
            transform.position.x <= cameraMax.x + 1f &&
            transform.position.y >= cameraMin.y - 1f && 
            transform.position.y <= cameraMax.y + 1f;
            
        // Activate/deactivate the enemy if visibility changed
        if (isVisible && !isActivated)
        {
            SetEnemyActive(true);
            if (debugMode) Debug.Log($"{gameObject.name} entered camera view and was activated.");
        }
        else if (!isVisible && isActivated)
        {
            // Optional: You can choose to keep enemies active once they've been seen
            // For now, we'll deactivate them when they leave the view
            // SetEnemyActive(false);
            // if (debugMode) Debug.Log($"{gameObject.name} left camera view and was deactivated.");
        }
    }
    
    void SetEnemyActive(bool active)
    {
        isActivated = active;
        
        // Keep the renderer enabled to see when it enters view, but disable physics
        if (rb != null)
        {
            rb.simulated = active;
        }
        
        if (enemyCollider != null)
        {
            enemyCollider.enabled = active;
        }
    }
    
    void MoveTowardsPlayer() 
    {
        // Get direction to player
        float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);
        
        // APPROACH 1: Rigidbody velocity
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            
            // If still not moving, try adding force instead
            if (rb.linearVelocity.magnitude < 0.1f) {
                rb.AddForce(new Vector2(direction * moveSpeed * 10f, 0), ForceMode2D.Force);
                if (debugMode) Debug.Log($"{gameObject.name}: Added force {direction * moveSpeed * 10f}");
            }
        }
        
        // APPROACH 2: Transform-based movement as a fallback
        if (rb == null || rb.linearVelocity.magnitude < 0.01f) {
            transform.Translate(new Vector3(direction * moveSpeed * Time.deltaTime, 0, 0));
            if (debugMode) Debug.Log($"{gameObject.name}: Used Transform.Translate as fallback");
        }

        // Update facing direction
        if (direction > 0 && !facingRight) Flip();
        else if (direction < 0 && facingRight) Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
        }
    }

    void Animate()
    {
        if (runAnimation == null || runAnimation.Length == 0 || spriteRenderer == null) return;

        animationTimer -= Time.deltaTime;
        if (animationTimer <= 0)
        {
            animationTimer = 1f / animationFPS;
            currentFrame = (currentFrame + 1) % runAnimation.Length;
            if (currentFrame >= 0 && currentFrame < runAnimation.Length)
            {
                 spriteRenderer.sprite = runAnimation[currentFrame];
            }
        }
    }

    // Create and apply a physics material with no friction to prevent player sticking
    private void ApplyFrictionlessPhysics()
    {
        if (enemyCollider != null)
        {
            // Create a new physics material
            PhysicsMaterial2D frictionlessMaterial = new PhysicsMaterial2D("EnemyFrictionless");
            frictionlessMaterial.friction = 0f;       // Zero friction
            frictionlessMaterial.bounciness = 0.1f;   // A tiny bit of bounce
            
            // Apply to the collider
            enemyCollider.sharedMaterial = frictionlessMaterial;
            
            if (debugMode) Debug.Log($"Applied frictionless material to {gameObject.name}");
            
            // Also add these properties to the Rigidbody2D if it exists
            if (rb != null)
            {
                // These settings make it harder for the player to "stick" to the enemy
                rb.linearDamping = 0f;
                rb.angularDamping = 0f;
            }
        }
    }

    // Public method called by player's attack
    public void TakeDamage()
    {
        Debug.Log($"{gameObject.name} took damage and was destroyed.");
        
        // Disable movement and collisions during death animation
        if (rb != null)
        {
            rb.simulated = false;
        }
        
        if (enemyCollider != null) 
        {
            enemyCollider.enabled = false;
        }
        
        // Start blink and destroy coroutine
        StartCoroutine(BlinkAndDestroy());
        
        // Add 10 points to the score when enemy is defeated
        if (GameManager.Instance != null)
        {
            // Add 10 points to the score
            GameManager.Instance.AddScore(10);
            
            if (debugMode) Debug.Log("Player earned 10 points for defeating an enemy!");
        }
    }
    
    // Coroutine to make the enemy blink red before destroying
    private IEnumerator BlinkAndDestroy()
    {
        // Check if we have a sprite renderer
        if (spriteRenderer != null)
        {
            // Change to damage color (red)
            spriteRenderer.color = damageColor;
        }
        
        // Wait for the visual blink duration
        yield return new WaitForSeconds(damageBlinkDuration);
        
        // Now destroy the enemy
        Destroy(gameObject);
    }
    
    // Collision logic - basic knockback attempt
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if colliding with the player
        if (collision.gameObject.CompareTag("Player"))
        {
             Debug.Log($"{gameObject.name} collided with Player.");
             Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
             if (playerRb != null)
             {
                 // Apply a small force to the player away from the enemy
                 Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                 knockbackDirection.y = Mathf.Max(knockbackDirection.y, 0.2f); // Ensure some upward push
                 playerRb.AddForce(knockbackDirection * 3f, ForceMode2D.Impulse); // Adjust force magnitude as needed
             }
        }
    }

    void OnDrawGizmos()
    {
        // Draw a line indicating the direction of movement
        if (Application.isPlaying && playerTarget != null && isActivated)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }
} 