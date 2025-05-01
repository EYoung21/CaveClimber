using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class JumpPotion : MonoBehaviour
{
    [Header("Potion Settings")]
    public float bobSpeed = 1.5f;        // How fast the potion moves up and down
    public float bobHeight = 0.3f;       // How high the potion moves up and down
    public float rotationSpeed = 45f;    // How fast the potion rotates
    public float jumpBoostMultiplier = 1.5f;  // 50% increase in jump height
    public float jumpBoostDuration = 15f;     // Duration in seconds
    
    [Header("Visual Effects")]
    public GameObject collectEffectPrefab; // Optional particle effect prefab
    
    // Static tracking for all active potions and cooldown state
    public static bool isJumpBoostActive = false;
    private static List<JumpPotion> activePotions = new List<JumpPotion>();
    
    private Vector3 startLocalPosition;
    private float bobTime;

    void Awake()
    {
        // Register this potion in the active potions list
        activePotions.Add(this);
    }
    
    void OnDestroy()
    {
        // Remove this potion from the active potions list when destroyed
        activePotions.Remove(this);
    }

    void Start()
    {
        startLocalPosition = transform.localPosition;
        bobTime = Random.Range(0f, 2f * Mathf.PI); // Random start position in bob cycle
        
        // If a jump boost is already active, destroy this potion immediately
        if (isJumpBoostActive)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Floating animation (relative to parent)
        bobTime += Time.deltaTime * bobSpeed;
        float yOffset = Mathf.Sin(bobTime) * bobHeight;
        // Update localPosition instead of position
        transform.localPosition = startLocalPosition + new Vector3(0f, yOffset, 0f);
        
        // Slow rotation (transform.Rotate works in local space by default)
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if player collided with the potion
        if (other.CompareTag("Player"))
        {
            // First, remove this potion from active list since we're about to destroy it
            activePotions.Remove(this);
            
            // Apply jump boost to player
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.ApplyJumpBoost(jumpBoostMultiplier, jumpBoostDuration);
                
                // Set the static flag to true - a jump boost is now active
                isJumpBoostActive = true;
                
                // Despawn all other potions
                DespawnAllPotions();
                
                // Start cooldown timer
                StartCoroutine(JumpBoostCooldown(jumpBoostDuration));
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
    
    // Static method to despawn all existing potions
    public static void DespawnAllPotions()
    {
        // Create a copy of the list to avoid modification during iteration
        List<JumpPotion> potionsToDestroy = new List<JumpPotion>(activePotions);
        
        foreach (JumpPotion potion in potionsToDestroy)
        {
            if (potion != null && potion.gameObject != null)
            {
                Destroy(potion.gameObject);
            }
        }
        
        // Clear the list
        activePotions.Clear();
    }
    
    // Coroutine to track when the jump boost has ended
    private static IEnumerator JumpBoostCooldown(float duration)
    {
        // Wait for the duration of the boost
        yield return new WaitForSeconds(duration);
        
        // Reset the flag
        isJumpBoostActive = false;
        
        Debug.Log("Jump boost cooldown ended - potions can spawn again");
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