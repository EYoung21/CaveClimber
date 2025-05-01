using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [System.Serializable]
    public class PlatformType
    {
        public GameObject prefab;
        public float spawnChance; // Base probability weight
        public bool isBasic = false; // Is this the basic platform type?
        public bool isBreaking = false; // Is this a breaking platform?
        public bool isMoving = false; // Is this a moving platform?
        public bool isSingleUse = false; // Is this a single-use jump platform?
    }
    
    [Header("Platform Types")]
    public PlatformType[] platformTypes;
    
    [Header("Moving Platform Settings")]
    public float minMovingPlatformSpeed = 1.5f;
    public float maxMovingPlatformSpeed = 3.0f;
    public float minMovingPlatformDistance = 1.5f;
    public float maxMovingPlatformDistance = 3.5f;
    
    [Header("Difficulty Settings")]
    public float breakingPlatformBaseChance = 0.1f; // Base chance for breaking platforms (changed from 0.15f)
    public float breakingPlatformMaxChance = 0.4f;  // Max chance at highest difficulty
    public float movingPlatformBaseChance = 0.1f;   // Base chance for moving platforms (changed from 0.2f) 
    public float movingPlatformMaxChance = 0.5f;    // Max chance at highest difficulty
    public float singleUsePlatformBaseChance = 0.1f; // Base chance for single-use platforms (changed from 0.15f)
    public float singleUsePlatformMaxChance = 0.3f;  // Max chance at highest difficulty
    public float maxMovingSpeedAtMaxDifficulty = 5.0f; // Maximum speed for moving platforms at max difficulty
    public float maxMovingDistanceAtMaxDifficulty = 6.0f; // Maximum distance for moving platforms at max difficulty
    
    public bool enableDebugLogs = true; // Logs inside coroutine still depend on this
    
    // Track platform types by category
    private List<PlatformType> basicPlatforms = new List<PlatformType>();
    private List<PlatformType> breakingPlatforms = new List<PlatformType>();
    private List<PlatformType> movingPlatforms = new List<PlatformType>();
    private List<PlatformType> singleUsePlatforms = new List<PlatformType>();
    
    private void Start()
    {
        // Make sure platform types are configured
        if (platformTypes.Length == 0)
        {
            Debug.LogWarning("No platform types defined in PlatformManager!");
        }
        
        // Sort platform types into categories
        CategorizeBasePlatforms();
    }
    
    private void CategorizeBasePlatforms()
    {
        basicPlatforms.Clear();
        breakingPlatforms.Clear();
        movingPlatforms.Clear();
        singleUsePlatforms.Clear();
        
        foreach (PlatformType platform in platformTypes)
        {
            if (platform.isBreaking)
            {
                breakingPlatforms.Add(platform);
            }
            else if (platform.isMoving)
            {
                movingPlatforms.Add(platform);
            }
            else if (platform.isSingleUse)
            {
                singleUsePlatforms.Add(platform);
            }
            else if (platform.isBasic)
            {
                basicPlatforms.Add(platform);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Categorized platforms: {basicPlatforms.Count} basic, " +
                      $"{breakingPlatforms.Count} breaking, {movingPlatforms.Count} moving, " +
                      $"{singleUsePlatforms.Count} single-use");
        }
    }
    
    // Get a random platform with difficulty factored in
    public GameObject GetRandomPlatform(float difficulty = 0f)
    {
        if (platformTypes.Length == 0)
            return null;
        
        // Calculate platform type chance based on difficulty
        float breakingChance = Mathf.Lerp(breakingPlatformBaseChance, breakingPlatformMaxChance, difficulty);
        float movingChance = Mathf.Lerp(movingPlatformBaseChance, movingPlatformMaxChance, difficulty);
        float singleUseChance = Mathf.Lerp(singleUsePlatformBaseChance, singleUsePlatformMaxChance, difficulty);
        
        // Decide which platform category to use
        float randomValue = Random.value;
        float currentThreshold = 0f;
        
        // Try to select a breaking platform
        currentThreshold += breakingChance;
        if (randomValue < currentThreshold && breakingPlatforms.Count > 0)
        {
            return GetRandomFromCategory(breakingPlatforms);
        }
        
        // Otherwise try to select a moving platform
        currentThreshold += movingChance;
        if (randomValue < currentThreshold && movingPlatforms.Count > 0)
        {
            return GetRandomFromCategory(movingPlatforms);
        }
        
        // Otherwise try to select a single-use platform
        currentThreshold += singleUseChance;
        if (randomValue < currentThreshold && singleUsePlatforms.Count > 0)
        {
            return GetRandomFromCategory(singleUsePlatforms);
        }
        
        // Otherwise use a basic platform if available
        if (basicPlatforms.Count > 0)
        {
            return GetRandomFromCategory(basicPlatforms);
        }
        
        // Fallback to weighted selection from all platforms if categories are empty
        return GetRandomWeightedPlatform();
    }
    
    private GameObject GetRandomFromCategory(List<PlatformType> category)
    {
        if (category.Count == 0)
            return null;
            
        // Calculate total weights for this category
        float totalWeight = 0;
        foreach (var platform in category)
        {
            totalWeight += platform.spawnChance;
        }
        
        // Random selection based on weight
        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;
        
        foreach (var platform in category)
        {
            currentWeight += platform.spawnChance;
            if (randomValue <= currentWeight)
            {
                return platform.prefab;
            }
        }
        
        // Fallback to first platform in category
        return category[0].prefab;
    }
    
    // Original weighted selection method (fallback)
    private GameObject GetRandomWeightedPlatform()
    {
        if (platformTypes.Length == 0)
            return null;
            
        // Calculate total weights
        float totalWeight = 0;
        foreach (var platform in platformTypes)
        {
            totalWeight += platform.spawnChance;
        }
        
        // Random selection based on weight
        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;
        
        foreach (var platform in platformTypes)
        {
            currentWeight += platform.spawnChance;
            if (randomValue <= currentWeight)
            {
                return platform.prefab;
            }
        }
        
        // Fallback to first platform
        return platformTypes[0].prefab;
    }
    
    // For backwards compatibility
    public GameObject GetRandomPlatform()
    {
        return GetRandomPlatform(0f);
    }
    
    // Configure platform with difficulty adjustment
    public void ConfigurePlatform(GameObject platform, Vector3 position, float difficulty = 0f)
    {
        platform.transform.position = position;
        
        ClimbableSurface platformScript = platform.GetComponent<ClimbableSurface>();
        Rigidbody2D rb = platform.GetComponent<Rigidbody2D>();

        if (platformScript != null)
        {
            // Get platform components to determine type
            MovingPlatformMarker movingMarker = platform.GetComponent<MovingPlatformMarker>();
            BreakingPlatform breakingPlatform = platform.GetComponent<BreakingPlatform>();
            SingleUseJumpPlatform singleUsePlatform = platform.GetComponent<SingleUseJumpPlatform>();
            MovingSingleUseJumpPlatform movingSingleUsePlatform = platform.GetComponent<MovingSingleUseJumpPlatform>();
            
            // Get all sprite renderers on this platform
            SpriteRenderer[] renderers = platform.GetComponentsInChildren<SpriteRenderer>();
            
            // Define colors
            Color blueColor = new Color(0.7f, 0.9f, 1.0f, 1.0f); // Lighter blue tint for moving
            Color blackColor = new Color(0.4f, 0.4f, 0.4f, 1.0f); // Dark tint for breaking
            Color purpleColor = new Color(0.8f, 0.2f, 1.0f, 1.0f); // Bright purple for single-use platforms
            
            // Apply appropriate tint based on platform type
            if (movingSingleUsePlatform != null)
            {
                // Moving single-use platforms - purple only
                foreach (SpriteRenderer renderer in renderers)
                {
                    renderer.color = purpleColor;
                }
                
                if (enableDebugLogs) Debug.Log($"[PlatformManager] Tinted platform {platform.name} purple (moving + single-use)", platform);
            }
            else if (singleUsePlatform != null)
            {
                // Single-use platform - purple tint
                foreach (SpriteRenderer renderer in renderers)
                {
                    renderer.color = purpleColor;
                }
                
                if (enableDebugLogs) Debug.Log($"[PlatformManager] Tinted platform {platform.name} purple (single-use)", platform);
            }
            else if (movingMarker != null && breakingPlatform != null)
            {
                // Both moving AND breaking - apply just black tint, no blue
                foreach (SpriteRenderer renderer in renderers)
                {
                    renderer.color = blackColor;
                }
                
                if (enableDebugLogs) Debug.Log($"[PlatformManager] Tinted platform {platform.name} black (moving + breaking)", platform);
            }
            else if (movingMarker != null)
            {
                // Only moving - apply light blue tint
                foreach (SpriteRenderer renderer in renderers)
                {
                    renderer.color = blueColor;
                }
                
                if (enableDebugLogs) Debug.Log($"[PlatformManager] Tinted platform {platform.name} light blue (moving)", platform);
            }
            else if (breakingPlatform != null)
            {
                // Only breaking - apply dark tint
                foreach (SpriteRenderer renderer in renderers)
                {
                    renderer.color = blackColor;
                }
                
                if (enableDebugLogs) Debug.Log($"[PlatformManager] Tinted platform {platform.name} dark (breaking)", platform);
            }
            
            // Log warning if no renderers found
            if (renderers.Length == 0 && enableDebugLogs && (movingMarker != null || breakingPlatform != null))
            {
                Debug.LogWarning($"[PlatformManager] No SpriteRenderers found to tint on platform {platform.name}", platform);
            }
            
            // Configure moving platform behavior
            if (movingMarker != null)
            {
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    StartCoroutine(MovePlatformRigidbody(platform, rb, difficulty));
                }
                else
                {
                    Debug.LogWarning($"[PlatformManager] MovingPlatform {platform.name} is missing a Rigidbody2D. Movement disabled.", platform);
                }
            }
            
            // Configure breaking platform properties if needed
            if (breakingPlatform != null)
            {
                // Optionally adjust breaking platform properties based on difficulty
                // For example: breakingPlatform.breakDelay = Mathf.Lerp(1.0f, 0.5f, difficulty);
            }
        }
        else 
        {
             Debug.LogWarning($"[PlatformManager] ConfigurePlatform called for {platform.name}, but ClimbableSurface script is missing!", platform);
        }
    }
    
    // For backwards compatibility
    public void ConfigurePlatform(GameObject platform, Vector3 position)
    {
        ConfigurePlatform(platform, position, 0f);
    }
    
    private IEnumerator MovePlatformRigidbody(GameObject platform, Rigidbody2D rb, float difficulty)
    {
        if (enableDebugLogs) Debug.Log($"[PlatformManager] MovePlatformRigidbody coroutine started for {platform.name} at difficulty {difficulty}", platform);
        
        // Scale platform movement parameters with difficulty
        float maxSpeed = Mathf.Lerp(maxMovingPlatformSpeed, maxMovingSpeedAtMaxDifficulty, difficulty);
        float maxDistance = Mathf.Lerp(maxMovingPlatformDistance, maxMovingDistanceAtMaxDifficulty, difficulty);
        
        // Randomize movement parameters for this platform
        float randomSpeed = Random.Range(minMovingPlatformSpeed, maxSpeed);
        float randomDistance = Random.Range(minMovingPlatformDistance, maxDistance);
        
        // Randomly choose direction
        bool startMovingRight = Random.value > 0.5f;
        float direction = startMovingRight ? 1f : -1f;
        
        Vector3 startPos = rb.position;
        Vector3 endPos = startPos + new Vector3(randomDistance * direction, 0, 0);
        
        float journeyLength = randomDistance;
        if (journeyLength <= 0) 
        {
             if (enableDebugLogs) Debug.LogWarning($"[PlatformManager] MovingPlatformDistance is zero or negative for {platform.name}. Movement disabled.", platform);
             yield break;
        }
        
        if (enableDebugLogs) Debug.Log($"[PlatformManager] Platform {platform.name} starting with Speed: {randomSpeed}, Distance: {randomDistance}, Direction: {(startMovingRight ? "right" : "left")}", platform);
        
        float startTime = Time.time;
        bool movingRight = startMovingRight;
        
        while (platform != null && platform.activeInHierarchy)
        {
            float distCovered = (Time.time - startTime) * randomSpeed;
            float fractionOfJourney = distCovered / journeyLength;
            
            Vector3 targetPosition;
            if (movingRight)
            {
                targetPosition = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                if (fractionOfJourney >= 1.0f)
                {
                    targetPosition = endPos;
                    movingRight = false;
                    startTime = Time.time;
                    Vector3 temp = startPos;
                    startPos = endPos;
                    endPos = temp;
                    if (enableDebugLogs) Debug.Log($"[PlatformManager] {platform.name} reached end, moving left. New Start: {startPos}, New End: {endPos}", platform);
                }
            }
            else
            {
                targetPosition = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                if (fractionOfJourney >= 1.0f)
                {
                    targetPosition = endPos;
                    movingRight = true;
                    startTime = Time.time;
                    Vector3 temp = startPos;
                    startPos = endPos;
                    endPos = temp;
                     if (enableDebugLogs) Debug.Log($"[PlatformManager] {platform.name} reached start, moving right. New Start: {startPos}, New End: {endPos}", platform);
                }
            }
            
            if (enableDebugLogs) Debug.Log($"[PlatformManager] Moving {platform.name} to Target: {targetPosition} (Fraction: {fractionOfJourney})", platform);
            
            rb.MovePosition(targetPosition);
            
            yield return new WaitForFixedUpdate();
        }
        
         if (enableDebugLogs) Debug.Log($"[PlatformManager] MovePlatformRigidbody coroutine ended for {platform.name}", platform);
    }
} 