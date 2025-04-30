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
             Debug.LogWarning("BreakingPlatform is missing a SpriteRenderer!", this);
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
            
            bool landedOnTopWithFeet = false;

            if (collision.relativeVelocity.y < -0.1f)
            {
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    if (contact.normal.y > 0.5f && 
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
                StartCoroutine(BreakPlatform());
            }
        }
    }
    
    private IEnumerator BreakPlatform()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = breakingColor;
        }
        
        yield return new WaitForSeconds(breakDelay);
        
        gameObject.SetActive(false);
    }
} 