using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [System.Serializable]
    public class PlatformType
    {
        public GameObject prefab;
        public float spawnChance; // Probability weight
    }
    
    [Header("Platform Types")]
    public PlatformType[] platformTypes;
    
    [Header("Moving Platform Settings")]
    public float movingPlatformSpeed = 2f;
    public float movingPlatformDistance = 2f;
    public bool enableDebugLogs = true; // Logs inside coroutine still depend on this
    
    private void Start()
    {
        // Make sure platform types are configured
        if (platformTypes.Length == 0)
        {
            Debug.LogWarning("No platform types defined in PlatformManager!");
        }
    }
    
    public GameObject GetRandomPlatform()
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
    
    public void ConfigurePlatform(GameObject platform, Vector3 position)
    {
        platform.transform.position = position;
        
        ClimbableSurface platformScript = platform.GetComponent<ClimbableSurface>();
        Rigidbody2D rb = platform.GetComponent<Rigidbody2D>();

        if (platformScript != null)
        {
            if (platform.GetComponent<MovingPlatformMarker>() != null) 
            {
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    StartCoroutine(MovePlatformRigidbody(platform, rb));
                }
                else
                {
                    Debug.LogWarning($"[PlatformManager] MovingPlatform {platform.name} is missing a Rigidbody2D. Movement disabled.", platform);
                }
            }
        }
        else 
        {
             Debug.LogWarning($"[PlatformManager] ConfigurePlatform called for {platform.name}, but ClimbableSurface script is missing!", platform);
        }
    }
    
    private IEnumerator MovePlatformRigidbody(GameObject platform, Rigidbody2D rb)
    {
        if (enableDebugLogs) Debug.Log($"[PlatformManager] MovePlatformRigidbody coroutine started for {platform.name}", platform);
        
        Vector3 startPos = rb.position;
        Vector3 endPos = startPos + new Vector3(movingPlatformDistance, 0, 0);
        
        float journeyLength = movingPlatformDistance;
        if (journeyLength <= 0) 
        {
             if (enableDebugLogs) Debug.LogWarning($"[PlatformManager] MovingPlatformDistance is zero or negative for {platform.name}. Movement disabled.", platform);
             yield break;
        }
        
        float startTime = Time.time;
        bool movingRight = true;
        
        while (platform != null && platform.activeInHierarchy)
        {
            float distCovered = (Time.time - startTime) * movingPlatformSpeed;
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