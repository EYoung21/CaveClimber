using UnityEngine;

public class PlatformCollisionFixer : MonoBehaviour
{
    // This script should be attached to the GameManager or another persistent object
    
    void Start()
    {
        FixAllPlatforms();
    }
    
    void FixAllPlatforms()
    {
        // Find all Platform Effectors in the scene using the non-deprecated method
        PlatformEffector2D[] platformEffectors = Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None);
        
        foreach (PlatformEffector2D effector in platformEffectors)
        {
            // Fix each platform effector to prevent side collisions
            ConfigurePlatformEffector(effector);
        }
        
        Debug.Log($"Fixed {platformEffectors.Length} platform effectors to prevent side collisions");
    }
    
    void ConfigurePlatformEffector(PlatformEffector2D effector)
    {
        // Keep one-way functionality
        effector.useOneWay = true;
        
        // Increase surface arc to 175 degrees (less than default 180)
        // This narrow arc ensures collision happens primarily at the top
        effector.surfaceArc = 175f;
        
        // Rotate the platform effector so the arc is centered at the bottom
        effector.rotationalOffset = 180f;
        
        // Ensure one-way collision requires a downward force
        effector.useOneWayGrouping = true;
        
        // Set the collider to be used for one-way platforms
        effector.useSideBounce = false;
        effector.useSideFriction = false;
        
        // Make sure the collider correctly captures the platform's shape
        BoxCollider2D boxCollider = effector.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Adjust collider to be slightly shorter in height
            // This helps prevent side collisions
            Vector2 size = boxCollider.size;
            size.y *= 0.95f; // Reduce height by 5%
            boxCollider.size = size;
            
            // Move the collider up slightly to ensure it's at the top of the platform
            Vector2 offset = boxCollider.offset;
            offset.y += 0.025f * size.y; // Move up by 2.5% of the height
            boxCollider.offset = offset;
        }
    }
    
    // This can be called from the editor to fix platforms
    public void EditorFixPlatforms()
    {
        FixAllPlatforms();
    }
} 