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
        
        // Get platform component to configure
        ClimbableSurface platformScript = platform.GetComponent<ClimbableSurface>();
        
        if (platformScript != null)
        {
            // Configure based on platform type (can be extended)
            if (platform.CompareTag("MovingPlatform"))
            {
                StartCoroutine(MovePlatform(platform));
            }
        }
    }
    
    private IEnumerator MovePlatform(GameObject platform)
    {
        Vector3 startPos = platform.transform.position;
        Vector3 endPos = startPos + new Vector3(movingPlatformDistance, 0, 0);
        
        float t = 0;
        bool movingRight = true;
        
        while (platform.activeInHierarchy)
        {
            if (movingRight)
            {
                t += Time.deltaTime * movingPlatformSpeed;
                if (t >= 1f)
                {
                    t = 1f;
                    movingRight = false;
                }
            }
            else
            {
                t -= Time.deltaTime * movingPlatformSpeed;
                if (t <= 0f)
                {
                    t = 0f;
                    movingRight = true;
                }
            }
            
            platform.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
    }
} 