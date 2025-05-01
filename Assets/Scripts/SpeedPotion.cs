using UnityEngine;
using System.Collections;

public class SpeedPotion : MonoBehaviour
{
    [Header("Potion Settings")]
    public float bobSpeed = 1.5f;        // How fast the potion moves up and down
    public float bobHeight = 0.3f;       // How high the potion moves up and down
    public float rotationSpeed = 45f;    // How fast the potion rotates
    public float speedBoostMultiplier = 1.5f;  // 50% increase in movement speed
    public float speedBoostDuration = 15f;     // Duration in seconds
    
    [Header("Visual Effects")]
    public GameObject collectEffectPrefab; // Optional particle effect prefab
    
    private Vector3 startLocalPosition;
    private float bobTime;

    void Awake()
    {
        // Register with PowerUpManager
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.RegisterSpeedPotion(this);
        }
    }
    
    void OnDestroy()
    {
        // Unregister with PowerUpManager
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.UnregisterSpeedPotion(this);
        }
    }

    void Start()
    {
        startLocalPosition = transform.localPosition;
        bobTime = Random.Range(0f, 2f * Mathf.PI); // Random start position in bob cycle
    }

    void Update()
    {
        // Floating animation (relative to parent)
        bobTime += Time.deltaTime * bobSpeed;
        float yOffset = Mathf.Sin(bobTime) * bobHeight;
        transform.localPosition = startLocalPosition + new Vector3(0f, yOffset, 0f);
        
        // Slow rotation (transform.Rotate works in local space by default)
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if player collided with the potion AND no powerup is active
        if (other.CompareTag("Player") && PowerUpManager.Instance != null && 
            PowerUpManager.Instance.ActivePowerUp == PowerUpType.None)
        {
            // Apply speed boost to player
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.ApplySpeedBoost(speedBoostMultiplier, speedBoostDuration);
                
                // Activate powerup in the manager
                PowerUpManager.Instance.ActivateSpeedPowerup(speedBoostDuration);
            }
            
            // Spawn collection effect if assigned
            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Add score through game manager (optional)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(25); // Bonus points for collecting potion
            }
            
            // Destroy the potion
            Destroy(gameObject);
        }
    }
    
    // This static method creates an intermediate GameObject that neutralizes a parent's non-uniform scale
    public static GameObject CreateScaleNeutralizer(GameObject parent, Vector3 worldPosition)
    {
        // Create empty GameObject as a scale neutralizer
        GameObject neutralizer = new GameObject("ScaleNeutralizer");
        neutralizer.transform.position = worldPosition;
        neutralizer.transform.parent = parent.transform;
        
        // Calculate inverse scale to neutralize parent's scale
        Vector3 parentScale = parent.transform.lossyScale; // Get the actual world scale
        
        // Prevent division by zero
        float scaleX = Mathf.Approximately(parentScale.x, 0) ? 1 : 1f / parentScale.x;
        float scaleY = Mathf.Approximately(parentScale.y, 0) ? 1 : 1f / parentScale.y;
        float scaleZ = Mathf.Approximately(parentScale.z, 0) ? 1 : 1f / parentScale.z;
        
        neutralizer.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        
        return neutralizer;
    }
} 