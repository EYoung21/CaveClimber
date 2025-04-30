using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject platformPrefab; // Default platform if PlatformManager isn't used
    
    public int numberOfPlatforms = 200;
    public float levelWidth = 6f;
    public float minY = 0.5f;
    public float maxY = 2f;
    
    private PlatformManager platformManager;
    
    // Changed back to Start
    void Start()
    {
        // Try to get PlatformManager using non-obsolete method
        platformManager = FindAnyObjectByType<PlatformManager>();
        
        Vector3 spawnPosition = new Vector3();
        
        // Generate initial platforms
        for (int i = 0; i < numberOfPlatforms; i++)
        {
            spawnPosition.y += Random.Range(minY, maxY);
            spawnPosition.x = Random.Range(-levelWidth, levelWidth);
            
            // Spawn platform
            SpawnPlatform(spawnPosition); // Removed index parameter
        }
    }
    
    void SpawnPlatform(Vector3 position)
    {
        GameObject platformObject;
        GameObject prefabToUse = platformPrefab; // Default
        
        // Use PlatformManager if available
        if (platformManager != null)
        {
            GameObject randomPrefab = platformManager.GetRandomPlatform();
            if (randomPrefab != null)
            {
                prefabToUse = randomPrefab;
            }
        }
        
        // Instantiate the chosen platform prefab
        platformObject = Instantiate(prefabToUse, position, Quaternion.identity);
        
        // Configure if using PlatformManager
        if (platformManager != null && prefabToUse != platformPrefab)
        {
             platformManager.ConfigurePlatform(platformObject, position);
        }
        
        // Removed logic related to finding startPlatform ID 0
    }
} 