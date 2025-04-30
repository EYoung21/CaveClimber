using UnityEngine;
using System.Collections;

public class BreakingPlatform : ClimbableSurface
{
    [Header("Breaking Platform Settings")]
    public float breakDelay = 0.2f;
    public Color breakingColor = new Color(1f, 0.5f, 0.5f, 0.8f);
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool hasBeenLandedOn = false;
    
    // Define the dark tint color
    private Color darkTint = new Color(0.4f, 0.4f, 0.4f, 1f); // Dark Gray
    
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Set the initial dark tint
            spriteRenderer.color = darkTint;
            originalColor = darkTint; // Store the tinted color as original for potential reuse
        }
        else
        {
             Debug.LogWarning("[BreakingPlatform] Missing a SpriteRenderer!", this);
        }
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
            
            bool landedOnTopWithFeet = false;

            if (relativeVelY < -0.1f) 
            {
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    if (contact.normal.y < -0.5f && 
                        contact.point.y <= playerBottomY + contactThreshold)
                    { 
                        landedOnTopWithFeet = true;
                        break; 
                    }
                }
            }

            if (landedOnTopWithFeet)
            {
                 hasBeenLandedOn = true;
                 
                 // Immediately disable collider to prevent jumping off
                 Collider2D platformCollider = GetComponent<Collider2D>();
                 if (platformCollider != null)
                 {
                     platformCollider.enabled = false;
                 }
                 else
                 {
                      Debug.LogWarning("[BreakingPlatform] Could not find Collider2D to disable!");
                 }
                 
                 StartCoroutine(BreakPlatform());
            }
        }
    }
    
    private IEnumerator BreakPlatform()
    {
        // Change to breaking color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = breakingColor;
        }
        
        // Wait for the visual delay
        yield return new WaitForSeconds(breakDelay);
        
        // Disable the GameObject visually after delay
        gameObject.SetActive(false);
        
        // Optional: Reset state if reusing/pooling
        // if (spriteRenderer != null) spriteRenderer.color = originalColor; 
        // hasBeenLandedOn = false; 
    }
} 