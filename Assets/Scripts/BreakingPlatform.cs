using UnityEngine;
using System.Collections;

public class BreakingPlatform : ClimbableSurface
{
    [Header("Breaking Platform Settings")]
    public float breakDelay = 0.3f;
    public Color breakingColor = Color.red;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool hasBeenLandedOn = false;
    
    protected override void Awake()
    {
        // Call base Awake for platform ID assignment
        base.Awake();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        // First call the base class method to handle scoring
        base.OnCollisionEnter2D(collision);
        
        // Check if this is a landing collision from above
        if (collision.relativeVelocity.y <= 0f && !hasBeenLandedOn)
        {
            hasBeenLandedOn = true;
            StartCoroutine(BreakPlatform());
        }
    }
    
    private IEnumerator BreakPlatform()
    {
        // Change color to indicate breaking
        if (spriteRenderer != null)
        {
            spriteRenderer.color = breakingColor;
        }
        
        // Wait for the specified delay
        yield return new WaitForSeconds(breakDelay);
        
        // Disable the platform
        gameObject.SetActive(false);
        
        // Reset state for reuse
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        hasBeenLandedOn = false;
    }
} 