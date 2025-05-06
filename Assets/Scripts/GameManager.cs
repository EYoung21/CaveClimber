using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Player Reference")]
    public Transform player;
    private PlayerController playerController;
    
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    
    [Header("Game Settings")]
    public float deathYThreshold = 10f; // How far below camera the player can fall
    // public int scorePerPlatform = 10; // Score given for each new platform (No longer used)
    public Vector3 playerSpawnOffset = new Vector3(0, 5, 0); // Offset relative to camera center
    
    private int currentScore = 0;
    private float maxPlayerHeight = 0f;
    private float playerStartY = 0f; // Added: To store player starting Y
    private float maxRelativePlayerHeight = 0f; // Added: To track max height relative to start
    private int bonusScore = 0; // Added: To track score from non-height sources
    private bool isGameOver = false;
    
    // Track platforms the player has already landed on (No longer used for scoring)
    // private HashSet<int> visitedPlatformIds = new HashSet<int>(); // Removed as unused
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; // Stop execution if this isn't the main instance
        }
    }
    
    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        Time.timeScale = 1f;
        isGameOver = false;
        currentScore = 0;
        bonusScore = 0; // Reset bonus score
        maxRelativePlayerHeight = 0f; // Reset max relative height
        
        // UpdateScoreUI(); // Don't call the general update yet
        if (scoreText != null) 
        {
             scoreText.text = "0"; // Initialize UI directly to 0
        }
        
        // Clear visited platforms (No longer needed)
        // visitedPlatformIds.Clear(); 
        
        // Position the player relative to the camera
        PositionPlayerAtStart();
        
        // Store starting Y position AFTER positioning
        if (player != null)
        {
            playerStartY = player.position.y;
             maxPlayerHeight = playerStartY; // Initialize max absolute height to start
        }
        
        // Get PlayerController reference
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                 Debug.LogError("GameManager could not find PlayerController component on the assigned player object!");
            }
        }
    }
    
    private void PositionPlayerAtStart()
    {
        if (player == null)
        {
            Debug.LogError("Player transform not assigned in GameManager!");
            return;
        }

        // Spawn player relative to the main camera's starting position
        Vector3 spawnPos = Camera.main.transform.position + playerSpawnOffset;
        spawnPos.z = 0; // Ensure player is on the correct Z plane
        player.position = spawnPos;
        // playerStartY = spawnPos.y; // Store start Y AFTER positioning is complete in Start()
        Debug.Log($"Player positioned at {spawnPos}");
    }
    
    private void Update()
    {
        // --- Check for Restart Input if Game Over ---
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                 RestartGame();
            }
            return; // Stop further updates if game is over
        }
        // --- End Restart Check ---
            
        // Check if player has fallen too far
        if (player != null && player.position.y < Camera.main.transform.position.y - deathYThreshold)
        {
            GameOver();
            return; // Don't update score after game over
        }
        
        // Update score based on max height, ONLY if gameplay has started
        if (player != null && playerController != null && playerController.hasStartedGameplay)
        {   
            // Track absolute max height (still useful for other systems like difficulty?)
            maxPlayerHeight = Mathf.Max(maxPlayerHeight, player.position.y);
            
            // Calculate current height relative to the start
            float currentRelativeHeight = Mathf.Max(0, player.position.y - playerStartY); 

            // Check if relative height is the new maximum
            if (currentRelativeHeight > maxRelativePlayerHeight)
            {
                maxRelativePlayerHeight = currentRelativeHeight;
                
                // Calculate total score (height * 10 + bonus)
                int heightScore = Mathf.FloorToInt(maxRelativePlayerHeight * 10); // Multiply height score by 10
                currentScore = heightScore + bonusScore; // Combine scores
                UpdateScoreUI(); // Update UI only when max relative height increases
            }
            // If only bonus score changed, UpdateScoreUI will be called from AddScore
        }
    }
    
    /* // No longer used for scoring
    // Called by platforms when player lands on them
    public void RegisterPlatformVisit(int platformId)
    {
        // Only count each platform once
        if (!visitedPlatformIds.Contains(platformId))
        {
            visitedPlatformIds.Add(platformId);
            // currentScore += scorePerPlatform; // No longer add score per platform
            // UpdateScoreUI();
            
            // Optional: Add a visual feedback for scoring
            // Debug.Log($"New platform visited! Platform ID: {platformId}");
        }
    }
    */
    
    // New method for adding score from defeating enemies or other sources
    public void AddScore(int points) // Receives already multiplied points (100 or 200)
    {
        // Only add score and update UI if gameplay has started
        if (playerController != null && playerController.hasStartedGameplay)
        {
            bonusScore += points; // Add to bonus score
            
            // Recalculate total score and update UI
            int heightScore = Mathf.FloorToInt(maxRelativePlayerHeight * 10); // Multiply height score by 10 here too
            currentScore = heightScore + bonusScore;
            UpdateScoreUI(); 
            
            // Optional: Add visual feedback for scoring
            Debug.Log($"Bonus score increased by {points}! New bonus score: {bonusScore}. Total score: {currentScore}");
        }
        else
        {
            // Store score internally even if gameplay hasn't started, but don't update UI
            bonusScore += points; // Still add to bonus score internally
            Debug.Log($"Gameplay not started. Bonus score increased internally by {points}. Current internal bonus score: {bonusScore}");
        }
    }
    
    private void UpdateScoreUI()
    {
        // Update UI
        if (scoreText != null)
            scoreText.text = currentScore.ToString();
    }
    
    public void GameOver()
    {
        if (isGameOver) return; // Prevent multiple calls
        
        isGameOver = true;
        
        // Show game over UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
            
        // Optional: slow down time slightly for dramatic effect
        Time.timeScale = 0.5f;
        Debug.Log("Game Over!");
    }
    
    public void RestartGame()
    {
        // Reset PowerUpManager if it exists
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Reset();
        }
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void ReturnToMainMenu()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }
} 