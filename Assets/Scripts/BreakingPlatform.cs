using UnityEngine;
using System.Collections;

public class BreakingPlatform : ClimbableSurface
{
    [Header("Breaking Platform Settings")]
    public float breakDelay = 0.05f;
    public Color breakingColor = new Color(1f, 0.5f, 0.5f, 0.8f);
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool hasBeenLandedOn = false;
    
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
             Debug.LogWarning("[BreakingPlatform] Missing a SpriteRenderer!", this);
        }
    }
    
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[BreakingPlatform] OnCollisionEnter2D with {collision.gameObject.name}");
        
        // Debug.Log($"[BreakingPlatform] Collision Enter with {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        
        base.OnCollisionEnter2D(collision);
        
        if (hasBeenLandedOn) 
        {
            // Debug.Log("[BreakingPlatform] Already landed on. Exiting.");
            return;
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[BreakingPlatform] Collision IS with Player.");
            // Debug.Log("[BreakingPlatform] Collision is with Player.");
            Collider2D playerCollider = collision.collider;
            if (playerCollider == null) 
            {
                 Debug.LogError("[BreakingPlatform] Player collider is null!");
                 return; 
            }
            
            float playerBottomY = playerCollider.bounds.min.y;
            float contactThreshold = 0.1f; 
            float relativeVelY = collision.relativeVelocity.y;
            Debug.Log($"[BreakingPlatform] Player Relative Vel Y: {relativeVelY}"); // Log relative velocity
            // Debug.Log($"[BreakingPlatform] Player Relative Vel Y: {relativeVelY}, Player Bottom Y: {playerBottomY}");
            
            bool landedOnTopWithFeet = false;

            if (relativeVelY < -0.1f) 
            {
                // Debug.Log("[BreakingPlatform] Player moving downwards.");
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    Debug.Log($"[BreakingPlatform] Checking Contact Point: Normal Y = {contact.normal.y}, Point Y = {contact.point.y}, Player Bottom Y: {playerBottomY}"); // Log contact point details + player bottom
                    // Debug.Log($"[BreakingPlatform] Checking Contact Point: Normal Y = {contact.normal.y}, Point Y = {contact.point.y}"); // Log contact point details
                    // Check if normal points downwards (opposite of expected top surface) and contact is near player feet
                    if (contact.normal.y < -0.5f && 
                        contact.point.y <= playerBottomY + contactThreshold)
                    { 
                        Debug.Log("[BreakingPlatform] LANDED ON TOP DETECTED based on contact point (inverted normal check)."); // Log detection with inverted check
                        // Debug.Log("[BreakingPlatform] LANDED ON TOP WITH FEET DETECTED!");
                        landedOnTopWithFeet = true;
                        break; 
                    }
                }
            }
            else
            {
                 Debug.Log("[BreakingPlatform] Player NOT moving downwards enough (Vel Y >= -0.1f).");
            }

            if (landedOnTopWithFeet)
            {
                 Debug.Log("[BreakingPlatform] Player landed correctly. Disabling collider and starting break coroutine.");
                 hasBeenLandedOn = true;
                 
                 // Immediately disable collider to prevent jumping off
                 Collider2D platformCollider = GetComponent<Collider2D>();
                 if (platformCollider != null)
                 {
                     platformCollider.enabled = false;
                     Debug.Log("[BreakingPlatform] Platform collider disabled.");
                 }
                 else
                 {
                      Debug.LogWarning("[BreakingPlatform] Could not find Collider2D to disable!");
                 }
                 
                 StartCoroutine(BreakPlatform());
            }
            else
            {
                 Debug.Log("[BreakingPlatform] Conditions for breaking not met (landedOnTopWithFeet is false).");
            }
        }
        else
        {
             Debug.Log("[BreakingPlatform] Collision NOT with Player.");
             // Debug.Log("[BreakingPlatform] Collision not with Player.");
        }
    }
    
    private IEnumerator BreakPlatform()
    {
        Debug.Log("[BreakingPlatform] BreakPlatform Coroutine Started.");
        if (spriteRenderer != null)
        {
            // Debug.Log("[BreakingPlatform] Setting color to breakingColor.");
            spriteRenderer.color = breakingColor;
        }
        
        yield return new WaitForSeconds(breakDelay);
        
        Debug.Log("[BreakingPlatform] Disabling GameObject.");
        gameObject.SetActive(false);
    }
} 