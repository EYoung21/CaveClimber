using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject platformPrefab; // Default platform if PlatformManager isn't used
    
    public int numberOfPlatforms = 200;
    public float levelWidth = 3f;
    public float minY = 0.5f;
    public float maxY = 2f;
    
    private PlatformManager platformManager;
    
    void Start()
    {
        // Try to get PlatformManager using non-obsolete method
        platformManager = FindAnyObjectByType<PlatformManager>();
        
        Vector3 spawnPosition = new Vector3();
        
        for (int i = 0; i < numberOfPlatforms; i++)
        {
            spawnPosition.y += Random.Range(minY, maxY);
            spawnPosition.x = Random.Range(-levelWidth, levelWidth);
            
            // Spawn platform
            SpawnPlatform(spawnPosition);
        }
    }
    
    void SpawnPlatform(Vector3 position)
    {
        GameObject platform;
        
        // Use PlatformManager if available
        if (platformManager != null)
        {
            GameObject platformPrefab = platformManager.GetRandomPlatform();
            if (platformPrefab != null)
            {
                platform = Instantiate(platformPrefab, position, Quaternion.identity);
                platformManager.ConfigurePlatform(platform, position);
                return;
            }
        }
        
        // Fallback to default platform
        platform = Instantiate(platformPrefab, position, Quaternion.identity);
    }
} 