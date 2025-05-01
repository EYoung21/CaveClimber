using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PowerUpType
{
    None,
    Jump,
    Slow,
    Speed
}

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;
    
    // Current active powerup state
    public PowerUpType ActivePowerUp { get; private set; } = PowerUpType.None;
    
    // Lists to track all powerups in the scene
    private List<JumpPotion> jumpPotions = new List<JumpPotion>();
    private List<SlowPotion> slowPotions = new List<SlowPotion>();
    private List<SpeedPotion> speedPotions = new List<SpeedPotion>();
    
    // Track enemies for slow effect
    private List<EnemyController> activeEnemies = new List<EnemyController>();
    
    // Reference to player controller for UI updates
    private PlayerController playerController;
    
    // Powerup timers
    private float powerupTimeRemaining = 0f;
    private Coroutine activeCoroutine = null;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = PowerUpManager.Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize to a clean state
            ActivePowerUp = PowerUpType.None;
            powerupTimeRemaining = 0f;
            jumpPotions.Clear();
            slowPotions.Clear();
            speedPotions.Clear();
            activeEnemies.Clear();
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Find the player controller
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
    }
    
    // Register methods for potions and enemies
    public void RegisterJumpPotion(JumpPotion potion)
    {
        if (!jumpPotions.Contains(potion))
            jumpPotions.Add(potion);
    }
    
    public void RegisterSlowPotion(SlowPotion potion)
    {
        if (!slowPotions.Contains(potion))
            slowPotions.Add(potion);
    }
    
    public void RegisterSpeedPotion(SpeedPotion potion)
    {
        if (!speedPotions.Contains(potion))
            speedPotions.Add(potion);
    }
    
    public void UnregisterJumpPotion(JumpPotion potion)
    {
        jumpPotions.Remove(potion);
    }
    
    public void UnregisterSlowPotion(SlowPotion potion)
    {
        slowPotions.Remove(potion);
    }
    
    public void UnregisterSpeedPotion(SpeedPotion potion)
    {
        speedPotions.Remove(potion);
    }
    
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
            
        // If slow powerup is active, immediately apply effect to new enemies
        if (ActivePowerUp == PowerUpType.Slow)
        {
            enemy.SetSlowEffect(true);
        }
    }
    
    public void UnregisterEnemy(EnemyController enemy)
    {
        activeEnemies.Remove(enemy);
    }
    
    // Activate a jump powerup
    public void ActivateJumpPowerup(float duration)
    {
        // If any powerup is already active, cancel it first
        if (ActivePowerUp != PowerUpType.None)
        {
            DeactivateCurrentPowerup();
        }
        
        // Set the active powerup
        ActivePowerUp = PowerUpType.Jump;
        powerupTimeRemaining = duration;
        
        // Start the cooldown coroutine
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
            
        activeCoroutine = StartCoroutine(PowerupCooldown(duration));
    }
    
    // Activate slow powerup
    public void ActivateSlowPowerup(float duration, float slowFactor)
    {
        // If any powerup is already active, cancel it first
        if (ActivePowerUp != PowerUpType.None)
        {
            DeactivateCurrentPowerup();
        }
        
        // Set the active powerup
        ActivePowerUp = PowerUpType.Slow;
        powerupTimeRemaining = duration;
        
        // Apply slow effect to all active enemies
        foreach (var enemy in activeEnemies)
        {
            enemy.SetSlowEffect(true);
        }
        
        // Start the cooldown coroutine
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
            
        activeCoroutine = StartCoroutine(PowerupCooldown(duration));
    }
    
    // Activate speed powerup
    public void ActivateSpeedPowerup(float duration)
    {
        // If any powerup is already active, cancel it first
        if (ActivePowerUp != PowerUpType.None)
        {
            DeactivateCurrentPowerup();
        }
        
        // Set the active powerup
        ActivePowerUp = PowerUpType.Speed;
        powerupTimeRemaining = duration;
        
        // Start the cooldown coroutine
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
            
        activeCoroutine = StartCoroutine(PowerupCooldown(duration));
    }
    
    // Deactivate current powerup
    private void DeactivateCurrentPowerup()
    {
        Debug.Log($"Deactivating power-up: {ActivePowerUp}");
        
        switch (ActivePowerUp)
        {
            case PowerUpType.Jump:
                // Call DeactivateJumpBoost on the PlayerController
                if (playerController != null)
                {
                    Debug.Log("Calling PlayerController.DeactivateJumpBoost()");
                    playerController.DeactivateJumpBoost();
                }
                else
                {
                    Debug.LogWarning("Cannot deactivate jump boost: playerController is null!");
                }
                break;
                
            case PowerUpType.Slow:
                // Remove slow effect from all enemies
                foreach (var enemy in activeEnemies)
                {
                    enemy.SetSlowEffect(false);
                }
                break;
                
            case PowerUpType.Speed:
                // No specific cleanup needed for speed, handled by PlayerController
                break;
        }
        
        // Hide the timer UI if player controller is available
        if (playerController != null)
        {
            playerController.HidePowerupTimer();
        }
        
        // Reset state
        PowerUpType oldType = ActivePowerUp;
        ActivePowerUp = PowerUpType.None;
        powerupTimeRemaining = 0f;
        
        Debug.Log($"Power-up {oldType} deactivated successfully");
    }
    
    // Powerup cooldown coroutine
    private IEnumerator PowerupCooldown(float duration)
    {
        yield return new WaitForSeconds(duration);
        DeactivateCurrentPowerup();
        Debug.Log($"Powerup effect ended.");
    }
    
    // Get remaining powerup time
    public float GetRemainingPowerupTime()
    {
        return powerupTimeRemaining;
    }
    
    // Update powerup time
    private void Update()
    {
        if (ActivePowerUp != PowerUpType.None && powerupTimeRemaining > 0)
        {
            powerupTimeRemaining -= Time.deltaTime;
        }
    }
    
    // New public method to reset the PowerUpManager state
    public void Reset()
    {
        // Deactivate any active powerup
        if (ActivePowerUp != PowerUpType.None)
        {
            DeactivateCurrentPowerup();
        }
        
        // Clear all lists
        jumpPotions.Clear();
        slowPotions.Clear();
        speedPotions.Clear();
        activeEnemies.Clear();
        
        // Reset any remaining state
        powerupTimeRemaining = 0f;
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        
        // Find the player controller again (it might be a new instance after restart)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
        
        Debug.Log("PowerUpManager has been reset");
    }
} 