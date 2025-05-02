using UnityEngine;
using System.Collections;

public class BatWingsPotion : MonoBehaviour
{
    [Header("Potion Settings")]
    public float bobSpeed = 1.5f;        // How fast the potion moves up and down
    public float bobHeight = 0.3f;       // How high the potion moves up and down
    public float rotationSpeed = 45f;    // How fast the potion rotates
    public float batWingsDuration = 10f; // Duration in seconds
    
    [Header("Visual Effects")]
    public GameObject collectEffectPrefab; // Optional particle effect prefab
    
    [Header("Animation")]
    public Sprite[] batWingsAnimation; // Assign the 4 bat wing sprites
    
    private Vector3 startLocalPosition;
    private float bobTime;

    void Awake()
    {
        // Register with PowerUpManager
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.RegisterBatWingsPotion(this);
        }
    }
    
    void OnDestroy()
    {
        // Unregister with PowerUpManager
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.UnregisterBatWingsPotion(this);
        }
    }

    void Start()
    {
        startLocalPosition = transform.localPosition;
        bobTime = Random.Range(0f, 2f * Mathf.PI); // Random start position in bob cycle
        
        // Set the sprite to the first frame of the animation
        if (batWingsAnimation != null && batWingsAnimation.Length > 0)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = batWingsAnimation[0];
            }
        }
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
            // Apply bat wings effect to player
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.ApplyBatWingsEffect(batWingsDuration, batWingsAnimation);
                
                // Activate powerup in the manager
                PowerUpManager.Instance.ActivateBatWingsPowerup(batWingsDuration);
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
        GameObject neutralizer = new GameObject("ScaleNeutralizer");
        neutralizer.transform.SetParent(parent.transform);
        neutralizer.transform.position = worldPosition;
        neutralizer.transform.localScale = new Vector3(
            1f / parent.transform.lossyScale.x,
            1f / parent.transform.lossyScale.y,
            1f / parent.transform.lossyScale.z
        );
        return neutralizer;
    }
} 