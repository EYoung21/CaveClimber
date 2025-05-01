using UnityEngine;
using System.Collections;

public class SingleUseJumpPlatform : ClimbableSurface
{
    [Tooltip("How long the fade out animation takes")]
    public float fadeDuration = 0.2f;
    
    [Tooltip("Platform color")]
    public Color platformColor = new Color(0.8f, 0.2f, 1.0f, 1.0f); // Bright purple
    
    [Tooltip("Enable verbose debug logs")]
    public bool debugMode = true;
    
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
        
        if (debugMode) Debug.Log($"[SingleUseJumpPlatform] Initialized: {name}", gameObject);
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
            
            if (debugMode) Debug.Log($"[SingleUseJumpPlatform] Player collision - RelVelY: {relativeVelY}, Bottom: {playerBottomY}", gameObject);
            
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
                        if (debugMode) Debug.Log($"[SingleUseJumpPlatform] Valid landing! Normal: {contact.normal.y}, Point: {contact.point.y}", gameObject);
                        break;
                    }
                    else if (debugMode)
                    {
                        Debug.Log($"[SingleUseJumpPlatform] Contact rejected - Normal: {contact.normal.y}, Point: {contact.point.y}", gameObject);
                    }
                }
            }
            
            if (landedOnTopWithFeet)
            {
                hasBeenLandedOn = true;
                Debug.Log($"[SingleUseJumpPlatform] Player landed on platform {name} with feet, fading out", gameObject);
                StartCoroutine(FadeOutAndDestroy());
            }
        }
    }
    
    private IEnumerator FadeOutAndDestroy()
    {
        // Disable collider immediately
        platformCollider.enabled = false;
        
        if (debugMode) Debug.Log($"[SingleUseJumpPlatform] Starting fade out for {name}", gameObject);
        
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
        
        if (debugMode) Debug.Log($"[SingleUseJumpPlatform] Destroying platform {name}", gameObject);
        
        // Destroy the platform
        Destroy(gameObject);
    }
} 