using UnityEngine;
using System.Collections;

public class MovingSingleUseJumpPlatform : ClimbableSurface
{
    [Header("Movement Settings")]
    [Tooltip("How far the platform moves horizontally")]
    public float moveDistance = 5f;
    
    [Tooltip("How fast the platform moves")]
    public float moveSpeed = 2f;
    
    [Header("Single Use Settings")]
    [Tooltip("How long the fade out animation takes")]
    public float fadeDuration = 0.2f;
    
    [Tooltip("Platform color")]
    public Color platformColor = new Color(0.8f, 0.2f, 1.0f, 1.0f); // Bright purple
    
    [Tooltip("Enable verbose debug logs")]
    public bool debugMode = true;
    
    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private Collider2D platformCollider;
    private bool hasBeenLandedOn = false;
    
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<Collider2D>();
        
        // Set bright purple tint
        spriteRenderer.color = platformColor;
        
        if (debugMode) Debug.Log($"[MovingSingleUseJumpPlatform] Initialized: {name}", gameObject);
    }
    
    private void Start()
    {
        startPosition = transform.position;
        if (debugMode) Debug.Log($"[MovingSingleUseJumpPlatform] Start position set: {startPosition}", gameObject);
    }
    
    private void Update()
    {
        if (!hasBeenLandedOn)
        {
            // Regular platform movement
            MoveHorizontally();
        }
    }
    
    private void MoveHorizontally()
    {
        float displacement = Mathf.PingPong(Time.time * moveSpeed, moveDistance);
        
        // Update position
        transform.position = startPosition + new Vector3(displacement, 0, 0);
    }
    
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
        
        if (hasBeenLandedOn) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Collider2D playerCollider = collision.collider;
            if (playerCollider == null) return;
            
            float playerBottomY = playerCollider.bounds.min.y;
            float contactThreshold = 0.1f;
            float relativeVelY = collision.relativeVelocity.y;
            
            if (debugMode) Debug.Log($"[MovingSingleUseJumpPlatform] Player collision - RelVelY: {relativeVelY}, Bottom: {playerBottomY}", gameObject);
            
            bool landedOnTopWithFeet = false;
            
            // Check if player is moving downward and landing from above
            if (relativeVelY < -0.1f)
            {
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    // THIS IS THE KEY DIFFERENCE! BreakingPlatform checks for normal.y < -0.5f
                    // which works correctly for detecting feet landing on top
                    if (contact.normal.y < -0.5f &&
                        contact.point.y <= playerBottomY + contactThreshold)
                    {
                        landedOnTopWithFeet = true;
                        if (debugMode) Debug.Log($"[MovingSingleUseJumpPlatform] Valid landing! Normal: {contact.normal.y}, Point: {contact.point.y}", gameObject);
                        break;
                    }
                    else if (debugMode)
                    {
                        Debug.Log($"[MovingSingleUseJumpPlatform] Contact rejected - Normal: {contact.normal.y}, Point: {contact.point.y}", gameObject);
                    }
                }
            }
            
            if (landedOnTopWithFeet)
            {
                hasBeenLandedOn = true;
                Debug.Log($"[MovingSingleUseJumpPlatform] Player landed on platform {name} with feet, fading out", gameObject);
                StartCoroutine(FadeOutAndDestroy());
            }
        }
    }
    
    private IEnumerator FadeOutAndDestroy()
    {
        // Disable collider immediately
        platformCollider.enabled = false;
        
        if (debugMode) Debug.Log($"[MovingSingleUseJumpPlatform] Starting fade out for {name}", gameObject);
        
        // Fade out animation
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        if (debugMode) Debug.Log($"[MovingSingleUseJumpPlatform] Destroying platform {name}", gameObject);
        
        // Destroy the platform
        Destroy(gameObject);
    }
} 