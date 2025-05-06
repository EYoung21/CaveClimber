using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("The name of your game scene to load when Play is clicked")]
    public string gameSceneName = "MainScene";

    [Tooltip("Reference to the tutorial panel GameObject")]
    public GameObject tutorialPanel;

    private void Start()
    {
        // Make sure tutorial panel is hidden on start
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    // Called when the Play button is clicked
    public void PlayGame()
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene(gameSceneName);
    }

    // Called when the Exit button is clicked
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        
#if UNITY_EDITOR
        // Special case for the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Actually quit the application in a build
        Application.Quit();
#endif
    }
    
    // Called when the Tutorial/Info button is clicked
    public void ToggleTutorial()
    {
        if (tutorialPanel != null)
        {
            // Toggle the tutorial panel visibility
            tutorialPanel.SetActive(!tutorialPanel.activeSelf);
        }
    }
} 