using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject platformPrefab; // Default platform if PlatformManager isn't used
    public GameObject enemyPrefab; // Assign your Enemy prefab here
    
    [Range(0f, 1f)]
    public float enemySpawnChance = 0.15f; // 15% chance to spawn an enemy
    
    public int numberOfPlatforms = 200;
    public float levelWidth = 6f;
    public float minY = 0.5f;
    public float maxY = 2f;
    
    [Header("Initial Spawn Settings")]
    public int initialPlatformsInView = 8; // How many platforms to place in initial view
    public float platformHeightOffset = 0.5f; // Vertical offset for platform placements
    
    private PlatformManager platformManager;
    private Camera mainCamera;
    
    public bool enableDebugLogs = true; // Add debug toggle
    
    // Changed back to Start
    void Start()
    {
        platformManager = FindAnyObjectByType<PlatformManager>();
        mainCamera = Camera.main;
        
        // Get camera bounds for initial platform placement
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        float screenBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        // Generate initial platforms in view
        GenerateInitialPlatforms(screenBottom, cameraHeight);
        
        // Generate the rest of the platforms above the camera view
        GenerateUpcomingPlatforms(screenBottom + cameraHeight);
    }
    
    void GenerateInitialPlatforms(float screenBottom, float cameraHeight)
    {
        // Calculate spacing between platforms in view
        float verticalSpacing = cameraHeight / (initialPlatformsInView + 1);
        
        // Ensure we have a platform near player spawn position
        Vector3 playerSpawnPosition = new Vector3(0, screenBottom + cameraHeight * 0.8f, 0);
        SpawnPlatform(playerSpawnPosition);
        
        // Spawn platforms starting from the bottom of the screen (with small offset)
        for (int i = 0; i < initialPlatformsInView; i++)
        {
            float yPosition = screenBottom + platformHeightOffset + (i * verticalSpacing);
            float xPosition = Random.Range(-levelWidth, levelWidth);
            Vector3 platformPosition = new Vector3(xPosition, yPosition, 0);
            SpawnPlatform(platformPosition);
        }
        
        Debug.Log($"Generated {initialPlatformsInView} initial platforms from {screenBottom} to {screenBottom + cameraHeight}");
    }
    
    void GenerateUpcomingPlatforms(float startY)
    {
        Vector3 spawnPosition = new Vector3(0, startY, 0);
        int remainingPlatforms = numberOfPlatforms - initialPlatformsInView - 1; // -1 for player platform
        
        for (int i = 0; i < remainingPlatforms; i++)
        {
            spawnPosition.y += Random.Range(minY, maxY);
            spawnPosition.x = Random.Range(-levelWidth, levelWidth);
            SpawnPlatform(spawnPosition);
        }
    }
    
    void SpawnPlatform(Vector3 position)
    {
        GameObject platformObject;
        GameObject prefabToUse = platformPrefab; // Default
        string prefabSourceName = "Default"; // Track where the prefab came from
        bool isBreaking = false; // Keep track if it's a breaking platform
        
        // Use PlatformManager if available
        if (platformManager != null)
        {
            GameObject randomPrefab = platformManager.GetRandomPlatform();
            if (randomPrefab != null)
            {
                prefabToUse = randomPrefab;
                prefabSourceName = "PlatformManager";
                // Only log if it's a moving platform OR general debug is enabled
                if (enableDebugLogs || randomPrefab.CompareTag("MovingPlatform")) 
                {
                    Debug.Log($"[LevelGenerator] PlatformManager selected: {randomPrefab.name}", randomPrefab);
                }
                
                // Check if the selected prefab is a breaking platform
                if (randomPrefab.GetComponent<BreakingPlatform>() != null) 
                { 
                    isBreaking = true;
                }
            }
            else
            {
                 if (enableDebugLogs) Debug.LogWarning("[LevelGenerator] PlatformManager returned null prefab, using default.");
            }
        }
        else
        {
             if (enableDebugLogs) Debug.Log("[LevelGenerator] PlatformManager not found, using default platform prefab.");
        }
        
        // Instantiate the chosen platform prefab
        platformObject = Instantiate(prefabToUse, position, Quaternion.identity);
        
        // Configure if using PlatformManager AND the chosen prefab is different from the default one assigned in LevelGenerator's inspector
        bool shouldConfigure = platformManager != null && prefabToUse != platformPrefab;
        
        if (shouldConfigure)
        {
             // Only log the configuration call if it's a moving platform OR general debug is enabled
             if (enableDebugLogs || platformObject.CompareTag("MovingPlatform"))
             {
                 Debug.Log($"[LevelGenerator] ---> Calling PlatformManager.ConfigurePlatform for {platformObject.name}", platformObject);
             }
             platformManager.ConfigurePlatform(platformObject, position);
        }
        
        // --- Spawn Enemy --- 
        // Only spawn if enemy prefab exists AND it's not a breaking platform AND random chance succeeds
        if (enemyPrefab != null && !isBreaking && Random.value < enemySpawnChance)
        {
            // Keep enemy spawn logs conditional
            if (enableDebugLogs) Debug.Log($"[LevelGenerator] Attempting to spawn enemy on {platformObject.name}", platformObject);
            SpawnEnemyOnPlatform(platformObject);
        }
        // --- End Enemy Spawn ---
    }

    void SpawnEnemyOnPlatform(GameObject platform)
    {
        // Calculate spawn position slightly above the platform center
        float platformHeight = platform.GetComponent<Collider2D>()?.bounds.size.y ?? 0.2f;
        float enemyOffsetY = 0.5f; // Adjust as needed based on enemy sprite pivot
        Vector3 enemySpawnPos = platform.transform.position + new Vector3(0, (platformHeight / 2f) + enemyOffsetY, 0);

        Instantiate(enemyPrefab, enemySpawnPos, Quaternion.identity);
        // Keep enemy spawn log conditional
        if(enableDebugLogs) 
        {
            Debug.Log($"Spawned enemy on platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
    }
} 