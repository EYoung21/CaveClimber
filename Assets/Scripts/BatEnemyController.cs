using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BatEnemyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Ensure bat is kinematic and not affected by gravity
        rb.isKinematic = true;
        rb.gravityScale = 0;

        // Ensure collider is NOT a trigger for physics interaction
        GetComponent<Collider2D>().isTrigger = false;

        // Get screen bounds
        if (Camera.main != null)
        {
            screenHalfWidth = Camera.main.aspect * Camera.main.orthographicSize;
        } else {
            Debug.LogError("BatEnemyController: Could not find main camera!");
            screenHalfWidth = 10f; // Fallback
        }
        batHalfWidth = spriteRenderer != null ? spriteRenderer.bounds.extents.x : 0.5f;
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
             GameManager.Instance.AddScore(5); // e.g., 5 points for a bat
        }

        // Destroy the bat object immediately
        Destroy(gameObject);
    }
} 