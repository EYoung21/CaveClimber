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
    
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    
    [Header("Game Settings")]
    public float deathYThreshold = 10f; // How far below camera the player can fall
    public int scorePerPlatform = 10; // Score given for each new platform
    public Vector3 playerSpawnOffset = new Vector3(0, 5, 0); // Offset relative to camera center
    
    private int currentScore = 0;
    private bool isGameOver = false;
    
    // Track platforms the player has already landed on
    private HashSet<int> visitedPlatformIds = new HashSet<int>();
    
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
        UpdateScoreUI();
        
        // Clear visited platforms
        visitedPlatformIds.Clear();
        
        // Position the player relative to the camera
        PositionPlayerAtStart();
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
        Debug.Log($"Player positioned at {spawnPos}");
    }
    
    private void Update()
    {
        if (isGameOver)
            return;
            
        // Check if player has fallen too far
        if (player != null && player.position.y < Camera.main.transform.position.y - deathYThreshold)
        {
            GameOver();
        }
    }
    
    // Called by platforms when player lands on them
    public void RegisterPlatformVisit(int platformId)
    {
        // Only count each platform once
        if (!visitedPlatformIds.Contains(platformId))
        {
            visitedPlatformIds.Add(platformId);
            currentScore += scorePerPlatform;
            UpdateScoreUI();
            
            // Optional: Add a visual feedback for scoring
            // Debug.Log($"New platform visited! Score: {currentScore}");
        }
    }
    
    // New method for adding score from defeating enemies or other sources
    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreUI();
        
        // Optional: Add visual feedback for scoring
        Debug.Log($"Score increased by {points}! New score: {currentScore}");
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
} 