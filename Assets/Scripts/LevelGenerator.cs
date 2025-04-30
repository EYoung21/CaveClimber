using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject platformPrefab; // Default platform if PlatformManager isn't used
    
    public int numberOfPlatforms = 200;
    public float levelWidth = 6f;
    public float minY = 0.5f;
    public float maxY = 2f;
    
    public Transform startPlatform { get; private set; } // Property to hold the transform of platform 0
    
    private PlatformManager platformManager;
    
    // Changed Start to Awake to ensure platforms are generated before GameManager tries to position the player
    void Awake()
    {
        // Try to get PlatformManager using non-obsolete method
        platformManager = FindAnyObjectByType<PlatformManager>();
        
        Vector3 spawnPosition = new Vector3();
        
        for (int i = 0; i < numberOfPlatforms; i++)
        {
            spawnPosition.y += Random.Range(minY, maxY);
            spawnPosition.x = Random.Range(-levelWidth, levelWidth);
            
            // Spawn platform
            SpawnPlatform(spawnPosition, i); // Pass the index as a potential ID hint (though ClimbableSurface handles the real ID)
        }
        
        if (startPlatform == null)
        {
            Debug.LogWarning("Start platform (ID 0) was not found or generated!");
        }
    }
    
    void SpawnPlatform(Vector3 position, int index)
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
        
        // Check if this is the starting platform (ID 0)
        ClimbableSurface platformScript = platformObject.GetComponent<ClimbableSurface>();
        if (platformScript != null && platformScript.platformId == 0)
        {
            startPlatform = platformObject.transform;
            Debug.Log($"Found start platform (ID 0) at position: {position}");
        }
    }
} 