using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InfoButton : MonoBehaviour
{
    [Tooltip("Reference to the tutorial panel to toggle")]
    public GameObject tutorialPanel;
    
    private Button button;
    
    private void Awake()
    {
        // Get the Button component
        button = GetComponent<Button>();
        
        // Add click listener
        if (button != null)
        {
            button.onClick.AddListener(ToggleTutorial);
        }
    }
    
    private void ToggleTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(!tutorialPanel.activeSelf);
        }
    }
    
    private void OnDestroy()
    {
        // Remove click listener to prevent memory leaks
        if (button != null)
        {
            button.onClick.RemoveListener(ToggleTutorial);
        }
    }
} 