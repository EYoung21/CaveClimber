using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PowerUpType
{
    None,
    Jump,
    Slow,
    Speed,
    BatWings
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
    private List<BatWingsPotion> batWingsPotions = new List<BatWingsPotion>();
    
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
            batWingsPotions.Clear();
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
    
    public void RegisterBatWingsPotion(BatWingsPotion potion)
    {
        if (!batWingsPotions.Contains(potion))
            batWingsPotions.Add(potion);
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
    
    public void UnregisterBatWingsPotion(BatWingsPotion potion)
    {
        batWingsPotions.Remove(potion);
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
        
        // Apply slow effect to all active caveman enemies
        foreach (var enemy in activeEnemies) // activeEnemies currently only holds EnemyController (cavemen)
        {
            if (enemy != null) // Safety check
            {
                 enemy.SetSlowEffect(true);
                 // Debug.Log($"Slowing down Caveman: {enemy.name}");
            }
        }
        
        // --- ADDED: Apply slow effect to all active bat enemies ---
        BatEnemyController[] activeBats = FindObjectsByType<BatEnemyController>(FindObjectsSortMode.None);
        foreach (var bat in activeBats)
        {   
            if (bat != null) // Safety check
            {
                bat.SetSlowEffect(true);
                // Debug.Log($"Slowing down Bat: {bat.name}");
            }
        }
        // --- END ADDITION ---
        
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
    
    // Activate bat wings powerup
    public void ActivateBatWingsPowerup(float duration)
    {
        // If any powerup is already active, cancel it first
        if (ActivePowerUp != PowerUpType.None)
        {
            DeactivateCurrentPowerup();
        }
        
        // Set the active powerup
        ActivePowerUp = PowerUpType.BatWings;
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
        PowerUpType oldType = ActivePowerUp; // Store type before resetting
        
        switch (ActivePowerUp)
        {
            case PowerUpType.Jump:
                // Call DeactivateJumpBoost on the PlayerController ONLY IF IT EXISTS
                if (playerController != null)
                {
                    // Debug.Log("Attempting PlayerController.DeactivateJumpBoost()");
                    playerController.DeactivateJumpBoost();
                }
                else
                {
                    Debug.LogWarning("Skipping DeactivateJumpBoost: playerController is null!");
                }
                break;
                
            case PowerUpType.Slow:
                // Remove slow effect from all enemies (uses FindObjectsByType - safer)
                EnemyController[] enemiesToUnslow = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
                foreach (var enemy in enemiesToUnslow)
                {
                    if (enemy != null) enemy.SetSlowEffect(false);
                }
                BatEnemyController[] batsToUnslow = FindObjectsByType<BatEnemyController>(FindObjectsSortMode.None);
                foreach (var bat in batsToUnslow)
                {
                    if (bat != null) bat.SetSlowEffect(false);
                }
                break;
                
            case PowerUpType.Speed:
                // Call DeactivateSpeedBoost on the PlayerController ONLY IF IT EXISTS
                if (playerController != null)
                {
                    // Debug.Log("Attempting PlayerController.DeactivateSpeedBoost()");
                    playerController.DeactivateSpeedBoost();
                }
                else
                {
                    Debug.LogWarning("Skipping DeactivateSpeedBoost: playerController is null!");
                }
                break;
                
            case PowerUpType.BatWings:
                // Call DeactivateBatWingsEffect on the PlayerController ONLY IF IT EXISTS
                if (playerController != null)
                {
                   // Debug.Log("Attempting PlayerController.DeactivateBatWingsEffect()");
                    playerController.DeactivateBatWingsEffect();
                }
                else
                {
                    Debug.LogWarning("Skipping DeactivateBatWingsEffect: playerController is null!");
                }
                break;
        }
        
        // Hide the timer UI ONLY IF player controller is available
        if (playerController != null)
        {
            playerController.HidePowerupTimer();
        }
        
        // Reset internal state (clears ActivePowerUp)
        ActivePowerUp = PowerUpType.None;
        powerupTimeRemaining = 0f;
        // Note: activeEnemies list is only used for applying slow on spawn, clearing it isn't critical here.
        // activeCoroutine is already null if called from the coroutine itself.
        
        Debug.Log($"Power-up {oldType} deactivation process complete.");
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
        Debug.Log("PowerUpManager Reset called."); // Add log

        // Stop any active powerup timer coroutine
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
            Debug.Log("PowerUpManager Reset: Stopped active coroutine.");
        }

        // Only reset effects that persist outside the player state (like enemy slow)
        // Find potentially remaining enemies/bats from the OLD scene (or new scene if Reset is called later) 
        // and un-slow them. Using FindObjectsByType might be safer during transitions.
        if (ActivePowerUp == PowerUpType.Slow)
        {
            EnemyController[] enemiesToUnslow = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var enemy in enemiesToUnslow)
            {
                if (enemy != null) enemy.SetSlowEffect(false);
            }
            BatEnemyController[] batsToUnslow = FindObjectsByType<BatEnemyController>(FindObjectsSortMode.None);
            foreach (var bat in batsToUnslow)
            {
                if (bat != null) bat.SetSlowEffect(false);
            }
            Debug.Log("PowerUpManager Reset: Attempted to clear slow effect from found enemies/bats.");
        }

        // Clear internal state *without* calling player-specific deactivation methods
        ActivePowerUp = PowerUpType.None;
        powerupTimeRemaining = 0f;
        activeEnemies.Clear(); // Clear list of registered cavemen

        // Nullify the player controller reference - it will be found again in the new scene's Start
        playerController = null;

        // Potion lists might need clearing too if they aren't managed correctly on scene changes
        jumpPotions.Clear();
        slowPotions.Clear();
        speedPotions.Clear();
        batWingsPotions.Clear();


        Debug.Log("PowerUpManager internal state has been reset.");
        // DO NOT CALL DeactivateCurrentPowerup() here as playerController is likely invalid during scene load.
    }
} 