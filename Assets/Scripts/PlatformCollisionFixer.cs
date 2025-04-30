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
        
        // Increase surface arc to ~270 degrees to allow side entry
        // The surface arc determines which angles can pass through
        effector.surfaceArc = 270f;
        
        // Rotate the platform effector so the arc is centered at the bottom
        effector.rotationalOffset = 180f;
        
        // Ensure one-way collision requires a downward force
        effector.useOneWayGrouping = true;
    }
    
    // This can be called from the editor to fix platforms
    public void EditorFixPlatforms()
    {
        FixAllPlatforms();
    }
} 