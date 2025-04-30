using UnityEngine;

public class ClimbableSurface : MonoBehaviour
{
    [Header("Surface Settings")]
    public float jumpForce = 10f;
    public bool isBreakable = false;
    public float breakThreshold = 10f;
    
    // Unique ID for this platform - will be automatically assigned
    [HideInInspector]
    public int platformId;
    
    private static int nextPlatformId = 0;
    
    protected virtual void Awake()
    {
        // Assign a unique ID to this platform
        platformId = nextPlatformId++;
        
        // Ensure platform has the "Platform" tag for layer setup
        if (gameObject.tag != "Platform")
        {
            gameObject.tag = "Platform";
        }
    }
    
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if player lands on this platform from above
        if (collision.relativeVelocity.y <= 0f)
        {
            // Get player controller
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                // Register this platform visit with GameManager for scoring
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegisterPlatformVisit(platformId);
                }
            }
        }
        
        // Player doesn't automatically bounce anymore
        // They will need to press space to jump
        
        // Breaking functionality remains
        if (isBreakable)
        {
            // Check if the impact force is strong enough to break the surface
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > breakThreshold)
            {
                // TODO: Add break effect and destroy the surface
                Destroy(gameObject);
            }
        }
    }
    
    // For debug purposes only
    protected virtual void OnDrawGizmos()
    {
        // Draw platform ID for debugging in the editor
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, "ID: " + platformId);
        }
        #endif
    }
} 