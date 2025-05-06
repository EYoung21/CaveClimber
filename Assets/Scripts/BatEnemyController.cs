using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BatEnemyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private float originalMoveSpeed; // To store the original speed

    [Header("Collision Handling")]
    // public LayerMask groundLayer; // Removed - Handled by BatCollisionIgnorer script

    [Header("Visuals")]
    public Color slowEffectColor = Color.blue; // Tint when slowed
    private Color originalSpriteColor; // To store the original color

    [Header("Animation (Optional)")]
    public Sprite[] flyingAnimation;
    public float animationFPS = 8f;

    [Header("Sound Effects")]
    [Tooltip("Assign 3 sounds to randomly play when the bat spawns.")]
    public AudioClip[] spawnSounds;
    // [Tooltip("Assign sounds to randomly play when the bat dies.")]
    // public AudioClip[] deathSounds; // Removed

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveDirection;
    private float screenHalfWidth;
    private float batHalfWidth;
    private bool initialized = false;

    // Animation state
    private float animationTimer;
    private int currentFrame;

    private Collider2D batCollider;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        batCollider = GetComponent<Collider2D>();
        
        // Apply frictionless physics
        ApplyFrictionlessPhysics();
        
        // Ensure bat is Dynamic and not affected by gravity
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Ensure collider is NOT a trigger for physics interaction
        if (batCollider != null) batCollider.isTrigger = false;
        else Debug.LogError("BatEnemyController: Missing Collider2D!");

        // Get screen bounds
        if (Camera.main != null)
        {
            screenHalfWidth = Camera.main.aspect * Camera.main.orthographicSize;
        } else {
            Debug.LogError("BatEnemyController: Could not find main camera!");
            screenHalfWidth = 10f; // Fallback
        }
        batHalfWidth = spriteRenderer != null ? spriteRenderer.bounds.extents.x : 0.5f;
        originalMoveSpeed = moveSpeed; // Store original speed
        if (spriteRenderer != null) 
        {
             originalSpriteColor = spriteRenderer.color; // Store original color
        }
    }

    // Called by LevelGenerator to set starting position and direction
    public void Initialize(bool startFromLeft)
    {
        if (Camera.main == null)
        {
            Debug.LogError("BatEnemyController: Cannot initialize, Main Camera not found!");
            Destroy(gameObject);
            return;
        }

        float startY = Camera.main.transform.position.y + Random.Range(-Camera.main.orthographicSize * 0.8f, Camera.main.orthographicSize * 0.8f);
        float startX;

        if (startFromLeft)
        {
            startX = -screenHalfWidth - batHalfWidth * 2; // Start just off the left edge
            moveDirection = Vector2.right;
            if(spriteRenderer) spriteRenderer.flipX = false; // Face right
        }
        else
        {
            startX = screenHalfWidth + batHalfWidth * 2; // Start just off the right edge
            moveDirection = Vector2.left;
             if(spriteRenderer) spriteRenderer.flipX = true; // Face left
        }

        transform.position = new Vector3(startX, startY, transform.position.z);
        
        // Set the scale
        transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
        
        // Recalculate half-width based on new scale if needed for precision (optional)
        batHalfWidth = spriteRenderer != null ? spriteRenderer.bounds.extents.x : 0.25f; 
        
        initialized = true;
        Debug.Log($"Bat initialized. Direction: {moveDirection} at {transform.position} with scale {transform.localScale.x}");

        // --- Check if Slow Powerup is Active --- 
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.ActivePowerUp == PowerUpType.Slow)
        {
            SetSlowEffect(true); // Apply slow effect immediately if active
            Debug.Log($"Bat spawned while slow effect active, applying immediately.");
        }
        // --- End Check ---

        // --- Play Spawn Sound --- 
        if (spawnSounds != null && spawnSounds.Length > 0)
        {
            int randIndex = Random.Range(0, spawnSounds.Length);
            if (spawnSounds[randIndex] != null)
            {
                AudioSource.PlayClipAtPoint(spawnSounds[randIndex], transform.position);
            }
        }
        // ----------------------

        // Start animation
        if (flyingAnimation != null && flyingAnimation.Length > 0)
        {
            animationTimer = 1f / animationFPS;
            currentFrame = 0;
            if (spriteRenderer != null) spriteRenderer.sprite = flyingAnimation[0];
        }
    }

    void FixedUpdate()
    {
        if (!initialized) return;

        // Move the bat horizontally
        rb.linearVelocity = moveDirection * moveSpeed;

        // Check if bat is off-screen and destroy it
        if ((moveDirection == Vector2.right && transform.position.x - batHalfWidth > screenHalfWidth) ||
            (moveDirection == Vector2.left && transform.position.x + batHalfWidth < -screenHalfWidth))
        {
            //Debug.Log("Bat off screen, destroying.");
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Handle animation loop
        if (flyingAnimation != null && flyingAnimation.Length > 0 && spriteRenderer != null)
        {
            animationTimer -= Time.deltaTime;
            if (animationTimer <= 0)
            {
                animationTimer = 1f / animationFPS;
                currentFrame = (currentFrame + 1) % flyingAnimation.Length;
                spriteRenderer.sprite = flyingAnimation[currentFrame];
            }
        }
    }

    private void ApplyFrictionlessPhysics()
    {
        if (batCollider != null)
        {
            PhysicsMaterial2D frictionlessMaterial = new PhysicsMaterial2D("BatFrictionless");
            frictionlessMaterial.friction = 0f;
            frictionlessMaterial.bounciness = 0.1f;
            
            batCollider.sharedMaterial = frictionlessMaterial;
            
            Debug.Log($"Applied frictionless material to {gameObject.name}");
            
            if (rb != null)
            {
                rb.sharedMaterial = frictionlessMaterial;
                rb.linearDamping = 0f;
                rb.angularDamping = 0f;
            }
        }
        else
        {
             Debug.LogError($"Cannot apply frictionless physics to {gameObject.name}: Collider not found!");
        }
    }

    // Renamed from OnTriggerEnter2D to handle physics collisions
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if collided with the player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Bat collided physically with Player.");
            // Physics engine handles the knockback based on Rigidbody masses and velocity
            // No need to call GameOver() anymore.

            // Optional: Apply a small extra force to the player for more noticeable impact
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                 knockbackDirection.y = Mathf.Max(knockbackDirection.y, 0.1f); // Add slight upward push
                playerRb.AddForce(knockbackDirection * 2f, ForceMode2D.Impulse); // Small extra force
            }
        }
        // --- REMOVED: Ground collision check is now handled by BatCollisionIgnorer ---
        // else if (((1 << collision.gameObject.layer) & groundLayer) != 0) 
        // {
        //     // If collided with ground, ignore collision with this specific platform
        //     if (batCollider != null && collision.collider != null)
        //     {
        //         Physics2D.IgnoreCollision(batCollider, collision.collider, true);
        //         // Debug.Log($"Bat {gameObject.name} ignoring collision with platform {collision.gameObject.name}");
        //     }
        // }
        // --- END REMOVAL ---
    }
    
    // Method called by PlayerController attack
    public void TakeDamage()
    {
         Debug.Log($"{gameObject.name} took damage and was destroyed.");
        
        // --- Play Death Sound --- (Removed)
        /* 
        if (deathSounds != null && deathSounds.Length > 0)
        {
            int randIndex = Random.Range(0, deathSounds.Length);
            if (deathSounds[randIndex] != null)
            {
                // Play sound at the bat's last position
                AudioSource.PlayClipAtPoint(deathSounds[randIndex], transform.position, 0.7f); // Slightly quieter maybe
            }
        }
        */
        // ----------------------
        
        // Optional: Add score for killing bat
        if (GameManager.Instance != null)
        { 
             GameManager.Instance.AddScore(200); // Changed from 5 to 200 (20 * 10)
        }

        // Destroy the bat object immediately
        Destroy(gameObject);
    }
    
    // --- ADDED: Method to handle slow effect --- 
    public void SetSlowEffect(bool isSlowed)
    {
        if (isSlowed)
        {
            moveSpeed = originalMoveSpeed / 2f; // Example: Halve the speed
            if (spriteRenderer != null)
            {
                // Apply blue tint
                spriteRenderer.color = new Color(slowEffectColor.r * 0.8f + 0.2f, 
                                                 slowEffectColor.g * 0.8f + 0.2f, 
                                                 slowEffectColor.b * 0.8f + 0.2f, 
                                                 1.0f);
            }
            // Debug.Log($"Bat {gameObject.name} slowed down to {moveSpeed}");
        }
        else
        {
            moveSpeed = originalMoveSpeed; // Restore original speed
            if (spriteRenderer != null)
            {
                 spriteRenderer.color = originalSpriteColor; // Restore original color
            }
            // Debug.Log($"Bat {gameObject.name} speed restored to {moveSpeed}");
        }
    }
    // --- END ADDITION ---
} 