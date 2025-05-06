using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public GameObject platformPrefab; // Default platform if PlatformManager isn't used
    public GameObject enemyPrefab; // Assign your Enemy prefab here
    public GameObject jumpPotionPrefab; // Assign your jump potion prefab here
    public GameObject slowPotionPrefab; // Assign your slow potion prefab here
    public GameObject speedPotionPrefab; // Assign your speed potion prefab here
    public GameObject batWingsPotionPrefab; // Assign your bat wings potion prefab here
    public GameObject batEnemyPrefab; // Assign your Bat Enemy prefab here
    public Transform platformContainer; // ADDED: Assign in Inspector to hold platforms
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float baseEnemySpawnChance = 0.1f; // Base chance for cavemen at difficulty 0
    public float maxEnemySpawnChance = 0.95f; // MAX cavemen (was 0.85)
    [Range(0f, 1f)]
    public float baseBatSpawnChance = 0.05f;   // Base chance for bats at difficulty 0
    public float maxBatSpawnChance = 0.6f; // MORE bats (was 0.4)
    // [Range(0f, 1f)] public float powerupSpawnChance = 0.05f; // Removed, now scales with difficulty
    [Range(0f, 1f)]
    public float basePowerupSpawnChance = 0.05f; // Base chance for powerups at difficulty 0
    public float maxPowerupSpawnChance = 0.50f; // MORE powerups (was 0.35)
    
    // These are kept for backward compatibility but no longer used directly
    [HideInInspector]
    public float jumpPotionSpawnChance = 0.1f;
    [HideInInspector]
    public float slowPotionSpawnChance = 0.1f;
    [HideInInspector]
    public float speedPotionSpawnChance = 0.1f;
    public float levelWidth = 6f;
    public float minY = 0.4f; // Minimum vertical gap (slightly reduced)
    public float maxY = 1.2f; // Further reduced base max gap (was 1.5) -> Denser platforms
    
    [Header("Initial Spawn Settings")]
    public int initialPlatformsInView = 8; // How many platforms to place in initial view
    public float platformHeightOffset = 0.5f; // Vertical offset for platform placements
    
    [Header("Infinite Generation Settings")]
    public int platformsToKeep = 50; // How many platforms to keep active at once
    public float platformGenerationThreshold = 15f; // How far ahead to generate platforms
    public float platformDespawnThreshold = 15f; // How far below to despawn platforms
    
    [Header("Difficulty Scaling")]
    public float minYAtMaxDifficulty = 1.5f; // Reduced min gap at max difficulty (was 2.0)
    public float maxYAtMaxDifficulty = 2.5f; // Further reduced max gap (was 3.0) -> Denser platforms at high difficulty
    // public float enemyChanceAtMaxDifficulty = 0.4f; // Removed, replaced by maxEnemySpawnChance
    public float difficultyScalingHeight = 150f; // Drastically reduced for faster scaling (was 700f)
    public float breakingPlatformChanceMax = 0.4f; // Maximum chance for breaking platforms
    public float movingPlatformChanceMax = 0.5f; // Maximum chance for moving platforms
    public float batSpawnHeightInterval = 30f; // Reduced interval for more frequent bat spawn attempts (was 50f)
    
    [Header("Gameplay Constraints")]
    public float playerMaxJumpHeight = 2.5f; // Max vertical distance player can jump reliably
    
    private PlatformManager platformManager;
    private Camera mainCamera;
    private List<GameObject> activePlatforms = new List<GameObject>();
    private float highestPlatformY = 0f;
    private float highestPlayerY = 0f;
    private float nextBatSpawnHeight = 0f;
    
    public bool enableDebugLogs = false; // General debug toggle
    
    private bool lastPlatformWasBreakable = false; // Track if the last spawned platform was breakable
    
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
        GameObject prefabToUse = null; // Start with null
        bool isBreakingPlatform = false;
        bool shouldConfigure = false;

        // --- MODIFIED PLATFORM SELECTION LOGIC --- 
        if (platformManager != null)
        {
            bool forceNonBreakable = lastPlatformWasBreakable; // Should we force non-breakable based on the *previous* spawn?

            if (forceNonBreakable)
            {
                if (enableDebugLogs) Debug.Log($"[LevelGenerator ({position.y:F1})] Previous was breakable, forcing non-breakable.");
                prefabToUse = platformManager.GetNonBreakablePlatform(currentDifficulty); 
                if (prefabToUse == null)
                {
                     Debug.LogWarning($"[LevelGenerator ({position.y:F1})] PlatformManager couldn't provide a specific non-breakable platform! Using default.");
                     prefabToUse = platformManager.defaultPlatformPrefab; 
                     if (prefabToUse == null) 
                     {
                         Debug.LogError($"[LevelGenerator ({position.y:F1})] Default platform prefab is null in PlatformManager! Using LevelGenerator's default.");
                         prefabToUse = this.platformPrefab; 
                     }
                }
                isBreakingPlatform = false; // We forced non-breakable
            }
            else // Previous was not breakable, choose randomly
            {
                 if (enableDebugLogs) Debug.Log($"[LevelGenerator ({position.y:F1})] Previous was NOT breakable, choosing randomly.");
                 prefabToUse = platformManager.GetRandomPlatform(currentDifficulty);
                 if (prefabToUse == null)
                 {
                     Debug.LogWarning($"[LevelGenerator ({position.y:F1})] PlatformManager returned null prefab for random choice, using default.");
                     prefabToUse = platformManager.defaultPlatformPrefab; 
                     if (prefabToUse == null) 
                     {
                         Debug.LogError($"[LevelGenerator ({position.y:F1})] Default platform prefab is null in PlatformManager! Using LevelGenerator's default.");
                         prefabToUse = this.platformPrefab; 
                     }
                 }
                 
                 // Check if the randomly chosen platform is breakable
                 isBreakingPlatform = (prefabToUse != null && prefabToUse.GetComponent<BreakingPlatform>() != null);
                 if (isBreakingPlatform && enableDebugLogs) Debug.Log($"[LevelGenerator ({position.y:F1})] Randomly selected a breakable platform.");
            }
            
            // Check if the selected prefab requires configuration 
            // (Assuming this.platformPrefab is the basic one that doesn't need config)
            if (prefabToUse != null && prefabToUse != this.platformPrefab) 
            {
                shouldConfigure = true;
            }
        }
        else // Fallback if PlatformManager doesn't exist
        {
             if (enableDebugLogs) Debug.Log($"[LevelGenerator ({position.y:F1})] PlatformManager not found, using LevelGenerator default.");
             prefabToUse = this.platformPrefab;
             isBreakingPlatform = false; // LevelGenerator default assumed non-breakable
        }
        // --- END MODIFIED SELECTION LOGIC --- 
        
        // Final check if we somehow ended up without a prefab
        if (prefabToUse == null)
        {
            Debug.LogError($"[LevelGenerator ({position.y:F1})] CRITICAL: Failed to determine platform prefab! Cannot spawn.");
            return null;
        }

        // --- UPDATE lastPlatformWasBreakable state for the NEXT iteration --- 
        // This happens *before* potentially returning null, using the isBreakingPlatform flag determined above.
        this.lastPlatformWasBreakable = isBreakingPlatform; 
        // ------------------------------------------------------------------
        
        GameObject newPlatform = Instantiate(prefabToUse, position, Quaternion.identity);
        newPlatform.transform.parent = platformContainer ?? transform; // Parent to container or this object
        
        // Assign unique ID (Removed from here, assuming it was not intended? Add back if needed)
        // ClimbableSurface surface = newPlatform.GetComponent<ClimbableSurface>();
        // if (surface != null) 
        // {
        //      surface.platformId = nextPlatformId++;
        //      if(enableDebugLogs) Debug.Log($"Assigned ID {surface.platformId} to {newPlatform.name}");
        // }
        // else
        // {
        //     Debug.LogWarning($"Platform {newPlatform.name} is missing ClimbableSurface script! Cannot assign ID.", newPlatform);
        // }

        // Call PlatformManager to configure if needed
        if (platformManager != null && shouldConfigure)
        {
            platformManager.ConfigurePlatform(newPlatform, position, currentDifficulty); 
        }

        // After platform is generated, spawn objects if not a breaking platform
        if (!isBreakingPlatform) // Use the final determined flag for *this* platform
        {
            bool spawnedEnemy = false;
            
            // Calculate enemy spawn chance based on difficulty (base% to max%)
            float adjustedEnemyChance = Mathf.Lerp(baseEnemySpawnChance, maxEnemySpawnChance, currentDifficulty);
            spawnedEnemy = TrySpawnObjectWithChance(adjustedEnemyChance, enemyPrefab, newPlatform, SpawnEnemyOnPlatform);
            
            // Calculate powerup chance based on difficulty (base% to max%)
            float adjustedPowerupSpawnChance = Mathf.Lerp(basePowerupSpawnChance, maxPowerupSpawnChance, currentDifficulty);
            
            // If we didn't spawn an enemy, maybe spawn a powerup
            if (!spawnedEnemy && Random.value < adjustedPowerupSpawnChance)
            {
                // Randomly select which type of powerup to spawn (equal 25% chance for each)
                float randomValue = Random.value;
                
                if (randomValue < 0.25f && jumpPotionPrefab != null)
                {
                    // Spawn jump potion (25% of powerups)
                    SpawnJumpPotionOnPlatform(newPlatform);
                    if(enableDebugLogs) Debug.Log($"Spawning jump potion on platform (1/4 chance of powerups)");
                }
                else if (randomValue < 0.5f && slowPotionPrefab != null)
                {
                    // Spawn slow potion (25% of powerups)
                    SpawnSlowPotionOnPlatform(newPlatform);
                    if(enableDebugLogs) Debug.Log($"Spawning slow potion on platform (1/4 chance of powerups)");
                }
                else if (randomValue < 0.75f && speedPotionPrefab != null)
                {
                    // Spawn speed potion (25% of powerups)
                    SpawnSpeedPotionOnPlatform(newPlatform);
                    if(enableDebugLogs) Debug.Log($"Spawning speed potion on platform (1/4 chance of powerups)");
                }
                else if (batWingsPotionPrefab != null)
                {
                    // Spawn bat wings potion (25% of powerups)
                    SpawnBatWingsPotionOnPlatform(newPlatform);
                    if(enableDebugLogs) Debug.Log($"Spawning bat wings potion on platform (1/4 chance of powerups)");
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
        
        nextBatSpawnHeight = batSpawnHeightInterval;
        lastPlatformWasBreakable = false; // Initialize tracker
    }
    
    void Update()
    {
        if (mainCamera == null) return;
        
        // Update highest player Y
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
             highestPlayerY = Mathf.Max(highestPlayerY, GameManager.Instance.player.position.y);
        }
        
        // Calculate the height threshold for generating new platforms
        float cameraTop = mainCamera.transform.position.y + mainCamera.orthographicSize;
        float generationThreshold = cameraTop + platformGenerationThreshold;
        
        // Generate more platforms if needed
        if (highestPlatformY < generationThreshold)
        {
            GenerateMorePlatforms(highestPlatformY, generationThreshold);
        }
        
        // Attempt to spawn bat based on player height
        TrySpawnBat();
        
        // Clean up platforms below view
        CleanupPlatforms();
    }
    
    void GenerateInitialPlatforms(float screenBottom, float cameraHeight)
    {
        float currentY = screenBottom + platformHeightOffset;
        float screenTop = screenBottom + cameraHeight;
        int platformsSpawned = 0;
        // float currentDifficulty = 0f; // Removed - Initial platforms always use default, no difficulty needed here

        Vector3 lastPos = Vector3.zero; // Track last position to avoid overlap

        while (platformsSpawned < initialPlatformsInView)
        {
            GameObject prefabToUse = null;

            // Always request the DEFAULT platform for initial generation
            if (platformManager != null)
            {
                // Access the public field directly
                prefabToUse = platformManager.defaultPlatformPrefab; 
                if (prefabToUse == null)
                {
                     // Log error if default is null, use LevelGenerator's default as fallback
                     Debug.LogError("[LevelGenerator Initial] Default platform prefab is null in PlatformManager! Using LevelGenerator's default as fallback.");
                     prefabToUse = this.platformPrefab;
                }
                 // Check if the default prefab is actually breakable (shouldn't be, but log warning if it is)
                 else if (prefabToUse.GetComponent<BreakingPlatform>() != null)
                 {
                    Debug.LogWarning("[LevelGenerator Initial] The assigned default platform prefab in PlatformManager appears to be breakable! Initial platforms might break.");
                 }
            }
            else
            {
                // Fallback if PlatformManager doesn't exist
                prefabToUse = this.platformPrefab;
            }

            if (prefabToUse == null)
            {
                 Debug.LogError("[LevelGenerator Initial] CRITICAL: Could not determine any platform prefab to spawn initially!");
                 break; // Stop trying if we can't even find a fallback
            }

            float y = Random.Range(minY, maxY);
            currentY += y;
            float x = Random.Range(-levelWidth, levelWidth);
            Vector3 pos = new Vector3(x, currentY, 0);

            // Simple overlap check (optional but good practice)
            if (platformsSpawned > 0 && Vector3.Distance(pos, lastPos) < 1.0f)
            {
                currentY += 0.5f; // Nudge up if too close
                pos.y = currentY;
            }

            // Use the determined prefabToUse
            GameObject newPlatform = Instantiate(prefabToUse, pos, Quaternion.identity);
            newPlatform.transform.parent = platformContainer ?? transform;
            lastPos = pos; // Update last position

            activePlatforms.Add(newPlatform);
            highestPlatformY = Mathf.Max(highestPlatformY, currentY);
            platformsSpawned++;
        }
        if(enableDebugLogs) Debug.Log($"Generated {platformsSpawned} initial platforms (using default prefab) from {screenBottom + platformHeightOffset} to {highestPlatformY}");
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

            // --- Constrain Max Y Spacing if Last Platform Was Breakable ---
            float currentMaxY = maxYSpacing;
            if (lastPlatformWasBreakable)
            {
                // Ensure the next platform is reachable after a break - Use 80% of max jump height for a safer gap
                float safeMaxJump = playerMaxJumpHeight * 0.8f; // Reduced from 0.9f
                currentMaxY = Mathf.Min(maxYSpacing, safeMaxJump); 
                if (enableDebugLogs) Debug.Log($"[LevelGenerator] Constraining max Y spacing to {currentMaxY} (80% of jump height {playerMaxJumpHeight}) after breakable platform.");
            }
            // --- End Constraint ---
            
            spawnPosition.y += Random.Range(minYSpacing, currentMaxY); // Use potentially constrained currentMaxY
            spawnPosition.x = Random.Range(-levelWidth, levelWidth);
            
            // SpawnPlatform will handle setting lastPlatformWasBreakable for the *next* iteration
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

    // New method to spawn bat wings potions
    GameObject SpawnBatWingsPotionOnPlatform(GameObject platform)
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
            GameObject neutralizer = BatWingsPotion.CreateScaleNeutralizer(platform, potionSpawnPos);
            
            // Create potion as a child of the neutralizer at local position zero
            potion = Instantiate(batWingsPotionPrefab, potionSpawnPos, Quaternion.identity);
            potion.transform.parent = neutralizer.transform;
            
            if (enableDebugLogs) Debug.Log($"Parented bat wings potion to scale neutralizer on moving platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        else
        {
            // For non-moving platforms, just create the potion normally
            potion = Instantiate(batWingsPotionPrefab, potionSpawnPos, Quaternion.identity);
            
            // If not a moving platform, parent to the main container for organization
            potion.transform.parent = platformContainer ?? transform;
        }
        
        // Keep potion spawn log conditional
        if(enableDebugLogs) 
        {
            Debug.Log($"Spawned bat wings potion on platform {platform.GetComponent<ClimbableSurface>()?.platformId}", platform);
        }
        
        return potion;
    }

    // Method to spawn bats
    private void TrySpawnBat()
    {
        if (batEnemyPrefab == null) return; // Don't spawn if prefab isn't assigned

        // Check if player has passed the next spawn height threshold
        if (highestPlayerY > nextBatSpawnHeight)
        {
            // Calculate difficulty based on this height
            float currentDifficulty = CalculateDifficulty(highestPlayerY);
            
            // Calculate the chance to spawn a bat at this interval (base% to max%)
            float adjustedBatSpawnChance = Mathf.Lerp(baseBatSpawnChance, maxBatSpawnChance, currentDifficulty);

            if (Random.value < adjustedBatSpawnChance)
            {
                // Spawn the bat
                SpawnBat();
            }
            
            // Set the next spawn height, adding some randomness
            nextBatSpawnHeight += batSpawnHeightInterval * Random.Range(0.8f, 1.2f);
        }
    }

    private void SpawnBat()
    {
        GameObject newBat = Instantiate(batEnemyPrefab);
        BatEnemyController batController = newBat.GetComponent<BatEnemyController>();
        
        if (batController != null)
        {
            bool startLeft = Random.value < 0.5f; // 50% chance to start from left
            batController.Initialize(startLeft);
             if(enableDebugLogs) Debug.Log($"Spawning Bat. Start Left: {startLeft}");
        }
        else
        {
            Debug.LogError("Bat Enemy Prefab is missing the BatEnemyController script!", newBat);
            Destroy(newBat); // Destroy invalid bat
        }
    }
} 