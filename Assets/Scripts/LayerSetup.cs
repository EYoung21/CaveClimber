using UnityEngine;

public class LayerSetup : MonoBehaviour
{
    // Run this from the editor to set up layers
    public void SetupLayers()
    {
        Debug.Log("Setting up layers for Cave Climbers game...");
        
        // Find all platforms and set them to the Ground layer
        GameObject[] platforms = GameObject.FindGameObjectsWithTag("Platform");
        int groundLayer = LayerMask.NameToLayer("Ground");
        
        if (groundLayer == -1)
        {
            Debug.LogError("Ground layer does not exist! Make sure to create this layer in Project Settings.");
            return;
        }
        
        // Count platforms fixed
        int platformsFixed = 0;
        
        foreach (GameObject platform in platforms)
        {
            if (platform.layer != groundLayer)
            {
                platform.layer = groundLayer;
                platformsFixed++;
            }
        }
        
        // Find the player and make sure it's NOT on the Ground layer
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.layer == groundLayer)
        {
            player.layer = 0; // Default layer
            Debug.Log("Player was on Ground layer, moved to Default layer");
        }
        
        // Set PlayerController's ground layer mask
        PlayerController playerController = player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.groundLayer = 1 << groundLayer; // Convert layer to mask
            Debug.Log($"Set player's groundLayer mask to: {playerController.groundLayer}");
            
            // Make sure radius is not zero
            if (playerController.groundCheckRadius <= 0)
            {
                playerController.groundCheckRadius = 0.2f;
                Debug.Log("Set player's groundCheckRadius to 0.2");
            }
        }
        
        Debug.Log($"Layer setup complete! Fixed {platformsFixed} platforms.");
    }
    
    // This will run once when the game starts
    void Start()
    {
        SetupLayers();
        
        // Disable this component after setup
        enabled = false;
    }
} 