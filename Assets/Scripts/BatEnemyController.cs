using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BatEnemyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Animation (Optional)")]
    public Sprite[] flyingAnimation;
    public float animationFPS = 8f;

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

        // Ensure collider is a trigger
        GetComponent<Collider2D>().isTrigger = true;

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
        initialized = true;
        Debug.Log($"Bat initialized. Direction: {moveDirection} at {transform.position}");

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

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if collided with the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Bat hit Player! Game Over.");
            // Trigger Game Over via GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            // Optional: Destroy the bat immediately as well
            // Destroy(gameObject);
        }
    }
} 