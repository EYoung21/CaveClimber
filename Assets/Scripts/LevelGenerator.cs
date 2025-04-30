using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public GameObject platformPrefab; // Default platform if PlatformManager isn't used
    public GameObject enemyPrefab; // Assign your Enemy prefab here
    public Transform platformContainer; // ADDED: Assign in Inspector to hold platforms
    
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
    private int nextPlatformId = 0; // ADDED: For unique platform IDs
    private List<GameObject> activePlatforms = new List<GameObject>(); // ADDED: To track platforms
    
    public bool enableDebugLogs = false; // General debug toggle
    
    private GameObject SpawnPlatform(Vector3 position)
    {
        GameObject prefabToUse = platformPrefab; // Default prefab
        bool isBreakingPlatform = false; // Flag for breaking platforms
        bool shouldConfigure = false; // Flag to check if PlatformManager should configure

        // Use PlatformManager if available
        if (platformManager != null)
        {
            GameObject randomPrefab = platformManager.GetRandomPlatform();
            if (randomPrefab != null)
            {
                prefabToUse = randomPrefab;
                // Only log selection if general debug is enabled
                if (enableDebugLogs) 
                {
                    Debug.Log($"[LevelGenerator] PlatformManager selected: {prefabToUse.name}", prefabToUse);
                }
                
                // Check if the selected prefab is a breaking platform
                if (prefabToUse.GetComponent<BreakingPlatform>() != null)
                {
                    isBreakingPlatform = true;
                }
                
                 // Check if the selected prefab is different from the default basic one
                if (prefabToUse != platformPrefab) // Assuming platformPrefab is the basic one
                {
                    shouldConfigure = true;
                }
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning("[LevelGenerator] PlatformManager returned null prefab, using default.");
            }
        }
        
        GameObject newPlatform = Instantiate(prefabToUse, position, Quaternion.identity);
        newPlatform.transform.parent = platformContainer ?? transform; // Parent to container or this object
        
        // Assign unique ID
        ClimbableSurface surface = newPlatform.GetComponent<ClimbableSurface>();
        if (surface != null) 
        {
             surface.platformId = nextPlatformId++;
             // Only log if general debug is enabled
             if(enableDebugLogs) Debug.Log($"Assigned ID {surface.platformId} to {newPlatform.name}");
        }
        else
        {
            Debug.LogWarning($"Platform {newPlatform.name} is missing ClimbableSurface script! Cannot assign ID.", newPlatform);
        }

        // Call PlatformManager to configure if needed
        if (platformManager != null && shouldConfigure)
        {
            platformManager.ConfigurePlatform(newPlatform, position); 
        }

        // Spawn Enemy?
        if (!isBreakingPlatform && Random.value < enemySpawnChance) // Don't spawn on breaking platforms
        {
            // Only log if general debug is enabled
            if(enableDebugLogs) Debug.Log($"[LevelGenerator] Attempting to spawn enemy on {newPlatform.name}", newPlatform);
            SpawnEnemyOnPlatform(newPlatform);
        }
        
        activePlatforms.Add(newPlatform);
        return newPlatform;
    }

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