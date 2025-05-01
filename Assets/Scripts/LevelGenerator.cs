using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public GameObject platformPrefab; // Default platform if PlatformManager isn't used
    public GameObject enemyPrefab; // Assign your Enemy prefab here
    public GameObject jumpPotionPrefab; // Assign your jump potion prefab here
    public GameObject slowPotionPrefab; // Assign your slow potion prefab here
    public GameObject speedPotionPrefab; // Assign your speed potion prefab here
    public Transform platformContainer; // ADDED: Assign in Inspector to hold platforms
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float enemySpawnChance = 0.15f; // 15% chance to spawn an enemy
    [Range(0f, 1f)]
    public float powerupSpawnChance = 0.1f; // 10% chance to spawn any powerup
    
    // These are kept for backward compatibility but no longer used directly
    [HideInInspector]
    public float jumpPotionSpawnChance = 0.1f;
    [HideInInspector]
    public float slowPotionSpawnChance = 0.1f;
    [HideInInspector]
    public float speedPotionSpawnChance = 0.1f;
    public float levelWidth = 6f;
    public float minY = 0.5f;
    public float maxY = 2f;
    
    [Header("Initial Spawn Settings")]
    public int initialPlatformsInView = 8; // How many platforms to place in initial view
    public float platformHeightOffset = 0.5f; // Vertical offset for platform placements
    
    [Header("Infinite Generation Settings")]
    public int platformsToKeep = 50; // How many platforms to keep active at once
    public float platformGenerationThreshold = 15f; // How far ahead to generate platforms
    public float platformDespawnThreshold = 15f; // How far below to despawn platforms
    
    [Header("Difficulty Scaling")]
    public float minYAtMaxDifficulty = 2.5f; // Maximum vertical gap at highest difficulty
    public float maxYAtMaxDifficulty = 4.0f; // Maximum vertical gap at highest difficulty
    public float enemyChanceAtMaxDifficulty = 0.4f; // Maximum enemy spawn chance
    public float difficultyScalingHeight = 500f; // Height at which max difficulty is reached
    public float breakingPlatformChanceMax = 0.4f; // Maximum chance for breaking platforms
    public float movingPlatformChanceMax = 0.5f; // Maximum chance for moving platforms
    
    private PlatformManager platformManager;
    private Camera mainCamera;
    private int nextPlatformId = 0; // For unique platform IDs
    private List<GameObject> activePlatforms = new List<GameObject>(); // To track platforms
    private float highestPlatformY = 0f; // Track highest platform's Y position
    
    public bool enableDebugLogs = false; // General debug toggle
    
    // Spawn an object at position with given chance
    private bool TrySpawnObjectWithChance(float chance, GameObject prefab, GameObject platform, System.Func<GameObject, GameObject> spawnFunc)
    {
        if (prefab != null && Random.value < chance)
        {
            spawnFunc(platform);
            return true;
        }
        return false;
    }
    
    private GameObject SpawnPlatform(Vector3 position, float currentDifficulty)
    {
        GameObject prefabToUse = platformPrefab; // Default prefab
        bool isBreakingPlatform = false; // Flag for breaking platforms
        bool shouldConfigure = false; // Flag to check if PlatformManager should configure

        // Use PlatformManager if available
        if (platformManager != null)
        {
            // Apply difficulty to platform selection
            GameObject randomPrefab = platformManager.GetRandomPlatform(currentDifficulty);
            if (randomPrefab != null)
            {
                prefabToUse = randomPrefab;
                // Only log selection if general debug is enabled
                if (enableDebugLogs) 
                {
                    Debug.Log($"[LevelGenerator] PlatformManager selected: {prefabToUse.name} at difficulty {currentDifficulty}", prefabToUse);
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
            platformManager.ConfigurePlatform(newPlatform, position, currentDifficulty); 
        }

        // After platform is generated, spawn objects if not a breaking platform
        if (!isBreakingPlatform)
        {
            bool spawnedEnemy = false;
            
            // Try to spawn an enemy first (with adjusted chance based on difficulty)
            float adjustedEnemyChance = Mathf.Lerp(enemySpawnChance, enemyChanceAtMaxDifficulty, currentDifficulty);
            spawnedEnemy = TrySpawnObjectWithChance(adjustedEnemyChance, enemyPrefab, newPlatform, SpawnEnemyOnPlatform);
            
            // If we didn't spawn an enemy, maybe spawn a powerup
            if (!spawnedEnemy && Random.value < powerupSpawnChance)
            {
                // Randomly select which type of powerup to spawn (equal 33% chance for each)
                float randomValue = Random.value;
                
                if (randomValue < 0.33f && jumpPotionPrefab != null)
                {
                    // Spawn jump potion (33% of powerups)
                    SpawnJumpPotionOnPlatform(newPlatform);
                    if(enableDebugLogs) Debug.Log($"Spawning jump potion on platform (1/3 chance of powerups)");
                }
                else if (randomValue < 0.66f && slowPotionPrefab != null)
                {
                    // Spawn slow potion (33% of powerups)
                    SpawnSlowPotionOnPlatform(newPlatform);
                    if(enableDebugLogs) Debug.Log($"Spawning slow potion on platform (1/3 chance of powerups)");
                }
                else if (speedPotionPrefab != null)
                {
                    // Spawn speed potion (33% of powerups)
                    SpawnSpeedPotionOnPlatform(newPlatform);
                    if(enableDebugLogs) Debug.Log($"Spawning speed potion on platform (1/3 chance of powerups)");
                }
            }
        }
        
        activePlatforms.Add(newPlatform);
        
        // Update highest platform Y if this one is higher
        if (position.y > highestPlatformY)
        {
            highestPlatformY = position.y;
        }
        
        return newPlatform;
    }

    // Start is called before the first frame update
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
    }
    
    void Update()
    {
        if (mainCamera == null) return;
        
        // Calculate the height threshold for generating new platforms
        float cameraTop = mainCamera.transform.position.y + mainCamera.orthographicSize;
        float generationThreshold = cameraTop + platformGenerationThreshold;
        
        // Generate more platforms if needed
        if (highestPlatformY < generationThreshold)
        {
            GenerateMorePlatforms(highestPlatformY, generationThreshold);
        }
        
        // Clean up platforms below view
        CleanupPlatforms();
    }
    
    void GenerateInitialPlatforms(float screenBottom, float cameraHeight)
    {
        // Calculate spacing between platforms in view
        float verticalSpacing = cameraHeight / (initialPlatformsInView + 1);
        
        // Ensure we have a platform near player spawn position
        Vector3 playerSpawnPosition = new Vector3(0, screenBottom + cameraHeight * 0.8f, 0);
        SpawnPlatform(playerSpawnPosition, 0f); // Start with 0 difficulty
        
        // Spawn platforms starting from the bottom of the screen (with small offset)
        for (int i = 0; i < initialPlatformsInView; i++)
        {
            float yPosition = screenBottom + platformHeightOffset + (i * verticalSpacing);
            float xPosition = Random.Range(-levelWidth, levelWidth);
            Vector3 platformPosition = new Vector3(xPosition, yPosition, 0);
            SpawnPlatform(platformPosition, 0f); // Initial platforms have 0 difficulty
        }
        
        Debug.Log($"Generated {initialPlatformsInView} initial platforms from {screenBottom} to {screenBottom + cameraHeight}");
    }
    
    void GenerateMorePlatforms(float startY, float targetY)
    {
        // Calculate current difficulty based on height
        float currentDifficulty = CalculateDifficulty(startY);
        
        // Adjust platform spacing based on difficulty
        float minYSpacing = Mathf.Lerp(minY, minYAtMaxDifficulty, currentDifficulty);
        float maxYSpacing = Mathf.Lerp(maxY, maxYAtMaxDifficulty, currentDifficulty);
        
        Vector3 spawnPosition = new Vector3(0, startY, 0);
        
        // Generate platforms until we reach the target height
        while (spawnPosition.y < targetY)
        {
            // Recalculate difficulty at this height
            currentDifficulty = CalculateDifficulty(spawnPosition.y);
            
            // Update spacing values based on new difficulty
            minYSpacing = Mathf.Lerp(minY, minYAtMaxDifficulty, currentDifficulty);
            maxYSpacing = Mathf.Lerp(maxY, maxYAtMaxDifficulty, currentDifficulty);
            
            spawnPosition.y += Random.Range(minYSpacing, maxYSpacing);
            spawnPosition.x = Random.Range(-levelWidth, levelWidth);
            
            SpawnPlatform(spawnPosition, currentDifficulty);
            
            if (enableDebugLogs) 
            {
                Debug.Log($"Generated platform at {spawnPosition} with difficulty {currentDifficulty}");
            }
        }
    }
    
    // Calculate difficulty from 0 to 1 based on height
    float CalculateDifficulty(float height)
    {
        return Mathf.Clamp01(height / difficultyScalingHeight);
    }
    
    void CleanupPlatforms()
    {
        if (mainCamera == null) return;
        
        // Simplified: Immediately destroy platforms that go below camera view
        float despawnY = mainCamera.transform.position.y - mainCamera.orthographicSize;
        List<GameObject> platformsToRemove = new List<GameObject>();
        
        // Find platforms below the threshold
        foreach (GameObject platform in activePlatforms)
        {
            if (platform == null)
            {
                platformsToRemove.Add(platform);
                continue;
            }
            
            if (platform.transform.position.y < despawnY)
            {
                platformsToRemove.Add(platform);
            }
        }
        
        // Remove and destroy platforms
        foreach (GameObject platform in platformsToRemove)
        {
            activePlatforms.Remove(platform);
            if (platform != null)
            {
                Destroy(platform);
                if (enableDebugLogs) Debug.Log($"Destroyed platform at y={platform.transform.position.y}");
            }
        }
    }

    // New method to spawn jump potions
    GameObject SpawnJumpPotionOnPlatform(GameObject platform)
    {
        // Calculate spawn position slightly above the platform center
        float platformHeight = platform.GetComponent<Collider2D>()?.bounds.size.y ?? 0.2f;
        float potionOffsetY = 0.8f; // Slightly higher than enemies
        Vector3 potionSpawnPos = platform.transform.position + new Vector3(0, (platformHeight / 2f) + potionOffsetY, 0);

        // Check if this is a moving platform
        MovingPlatformMarker movingPlatform = platform.GetComponent<MovingPlatformMarker>();
        GameObject potion;
        
        if (movingPlatform != null)
        {
            // Create a scale neutralizer between the platform and potion
            GameObject neutralizer = JumpPotion.CreateScaleNeutralizer(platform, potionSpawnPos);
            
            // Create potion as a child of the neutralizer at local position zero
            potion = Instantiate(jumpPotionPrefab, potionSpawnPos, Quaternion.identity);
            potion.transform.parent = neutralizer.transform;
            
            if (enableDebugLogs) Debug.Log($"Parented jump potion to scale neutralizer on moving platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        else
        {
            // For non-moving platforms, just create the potion normally
            potion = Instantiate(jumpPotionPrefab, potionSpawnPos, Quaternion.identity);
            
            // If not a moving platform, parent to the main container for organization
            potion.transform.parent = platformContainer ?? transform;
        }
        
        // Keep potion spawn log conditional
        if(enableDebugLogs) 
        {
            Debug.Log($"Spawned jump potion on platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        
        return potion;
    }
    
    // New method to spawn slow potions
    GameObject SpawnSlowPotionOnPlatform(GameObject platform)
    {
        // Calculate spawn position slightly above the platform center
        float platformHeight = platform.GetComponent<Collider2D>()?.bounds.size.y ?? 0.2f;
        float potionOffsetY = 0.8f; // Slightly higher than enemies
        Vector3 potionSpawnPos = platform.transform.position + new Vector3(0, (platformHeight / 2f) + potionOffsetY, 0);

        // Check if this is a moving platform
        MovingPlatformMarker movingPlatform = platform.GetComponent<MovingPlatformMarker>();
        GameObject potion;
        
        if (movingPlatform != null)
        {
            // Create a scale neutralizer between the platform and potion
            GameObject neutralizer = SlowPotion.CreateScaleNeutralizer(platform, potionSpawnPos);
            
            // Create potion as a child of the neutralizer at local position zero
            potion = Instantiate(slowPotionPrefab, potionSpawnPos, Quaternion.identity);
            potion.transform.parent = neutralizer.transform;
            
            if (enableDebugLogs) Debug.Log($"Parented slow potion to scale neutralizer on moving platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        else
        {
            // For non-moving platforms, just create the potion normally
            potion = Instantiate(slowPotionPrefab, potionSpawnPos, Quaternion.identity);
            
            // If not a moving platform, parent to the main container for organization
            potion.transform.parent = platformContainer ?? transform;
        }
        
        // Keep potion spawn log conditional
        if(enableDebugLogs) 
        {
            Debug.Log($"Spawned slow potion on platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        
        return potion;
    }
    
    // Method to spawn enemies renamed for consistency
    GameObject SpawnEnemyOnPlatform(GameObject platform)
    {
        // Calculate spawn position slightly above the platform center
        float platformHeight = platform.GetComponent<Collider2D>()?.bounds.size.y ?? 0.2f;
        float enemyOffsetY = 0.5f; // Adjust as needed based on enemy sprite pivot
        Vector3 enemySpawnPos = platform.transform.position + new Vector3(0, (platformHeight / 2f) + enemyOffsetY, 0);

        GameObject enemy = Instantiate(enemyPrefab, enemySpawnPos, Quaternion.identity);
        
        // Keep enemy spawn log conditional
        if(enableDebugLogs) 
        {
            Debug.Log($"Spawned enemy on platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        
        return enemy;
    }

    // New method to spawn speed potions
    GameObject SpawnSpeedPotionOnPlatform(GameObject platform)
    {
        // Calculate spawn position slightly above the platform center
        float platformHeight = platform.GetComponent<Collider2D>()?.bounds.size.y ?? 0.2f;
        float potionOffsetY = 0.8f; // Slightly higher than enemies
        Vector3 potionSpawnPos = platform.transform.position + new Vector3(0, (platformHeight / 2f) + potionOffsetY, 0);

        // Check if this is a moving platform
        MovingPlatformMarker movingPlatform = platform.GetComponent<MovingPlatformMarker>();
        GameObject potion;
        
        if (movingPlatform != null)
        {
            // Create a scale neutralizer between the platform and potion
            GameObject neutralizer = SpeedPotion.CreateScaleNeutralizer(platform, potionSpawnPos);
            
            // Create potion as a child of the neutralizer at local position zero
            potion = Instantiate(speedPotionPrefab, potionSpawnPos, Quaternion.identity);
            potion.transform.parent = neutralizer.transform;
            
            if (enableDebugLogs) Debug.Log($"Parented speed potion to scale neutralizer on moving platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        else
        {
            // For non-moving platforms, just create the potion normally
            potion = Instantiate(speedPotionPrefab, potionSpawnPos, Quaternion.identity);
            
            // If not a moving platform, parent to the main container for organization
            potion.transform.parent = platformContainer ?? transform;
        }
        
        // Keep potion spawn log conditional
        if(enableDebugLogs) 
        {
            Debug.Log($"Spawned speed potion on platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        
        return potion;
    }
} 