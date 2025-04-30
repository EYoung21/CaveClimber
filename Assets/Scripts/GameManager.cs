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
    public float playerSpawnOffsetY = 1.0f; // How high above the start platform the player spawns
    
    private int currentScore = 0;
    private bool isGameOver = false;
    
    // Track platforms the player has already landed on
    private HashSet<int> visitedPlatformIds = new HashSet<int>();
    
    private LevelGenerator levelGenerator;
    
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
        
        // Find the LevelGenerator - needed for player positioning
        levelGenerator = FindAnyObjectByType<LevelGenerator>();
        if (levelGenerator == null)
        {
            Debug.LogError("GameManager could not find LevelGenerator!");
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
        
        // Position the player above the start platform
        PositionPlayerAtStart();
    }
    
    private void PositionPlayerAtStart()
    {
        if (player == null)
        {
            Debug.LogError("Player transform not assigned in GameManager!");
            return;
        }
        
        if (levelGenerator != null && levelGenerator.startPlatform != null)
        {
            // Calculate spawn position
            Vector3 startPos = levelGenerator.startPlatform.position;
            startPos.y += playerSpawnOffsetY;
            
            // Set player position
            player.position = startPos;
            Debug.Log($"Player positioned at {startPos} above start platform.");
        }
        else
        {
            Debug.LogWarning("Could not position player - start platform not found by LevelGenerator.");
            // Optionally, set a default spawn position if platform 0 isn't found
            // player.position = new Vector3(0, 2, 0);
        }
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
            Debug.Log($"New platform visited! Score: {currentScore}");
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
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
} 