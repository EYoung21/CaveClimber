using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialPanel : MonoBehaviour
{
    [Header("Tutorial Settings")]
    [Tooltip("The background panel's CanvasGroup")]
    public CanvasGroup panelGroup;
    
    [Tooltip("Background opacity (0 = fully transparent, 1 = fully opaque)")]
    [Range(0, 1)]
    public float backgroundOpacity = 0.8f;
    
    [Tooltip("The Text component displaying tutorial content")]
    public TextMeshProUGUI tutorialText;
    
    [TextArea(5, 10)]
    [Tooltip("The tutorial content text - NOTE: Awake now forces a specific value!")]
    public string tutorialContent = 
        "HOW TO PLAY\n\n" +
        "CONTROLS:\n" +
        "- A/D: Move left and right\n" +
        "- Left Click: Attack enemies"; // Default value, might be overridden

    private void Awake()
    {
        // --- FORCE THE CORRECT TEXT IN AWAKE --- 
        string forcedContent = "HOW TO PLAY\n\n" +
                               "CONTROLS:\n" +
                               "- A/D: Move left and right\n" +
                               "- Left Click: Attack enemies";
        
        Debug.Log($"[TutorialPanel] Awake() called. Initial tutorialContent variable: '{tutorialContent}'");
        Debug.Log($"[TutorialPanel] Forcing content in Awake to: '{forcedContent}'");
        
        // Force both the variable AND the text component
        tutorialContent = forcedContent;
        if (tutorialText != null)
        {
            tutorialText.text = forcedContent;
            Debug.Log($"[TutorialPanel] Directly set tutorialText.text in Awake to: '{tutorialText.text}'");
        }
        else
        {
            Debug.LogWarning("[TutorialPanel] tutorialText component is not assigned in Awake!");
        }
        // --- END FORCE --- 
    }

    private void Start()
    {
        Debug.Log("[TutorialPanel] Start() called.");
        // UpdateTutorialText(); // No longer strictly needed here if Awake worked
    }

    private void OnEnable()
    {
        Debug.Log("[TutorialPanel] OnEnable() called.");
        // UpdateTutorialText(); // Let's rely on Awake for initial setting
        
        // Set background opacity
        if (panelGroup != null)
        {
            Debug.Log($"[TutorialPanel] Setting panel alpha to: {backgroundOpacity}");
            panelGroup.alpha = backgroundOpacity;
        }
        else
        {
             Debug.LogWarning("[TutorialPanel] panelGroup is not assigned!");
        }
    }
    
    // This is now mainly for potential updates AFTER Awake/OnEnable
    private void UpdateTutorialText()
    {
        Debug.Log($"[TutorialPanel] UpdateTutorialText() called. Content to set: '{tutorialContent}'");
        if (tutorialText != null)
        {
            Debug.Log($"[TutorialPanel] Assigning text to tutorialText component.");
            tutorialText.text = tutorialContent;
            Debug.Log($"[TutorialPanel] tutorialText.text is now: '{tutorialText.text}'");
        }
        else
        {
            Debug.LogWarning("[TutorialPanel] tutorialText component is not assigned!");
        }
    }

    // Close the tutorial panel
    public void CloseTutorial()
    {
        Debug.Log("[TutorialPanel] CloseTutorial() called.");
        gameObject.SetActive(false);
    }
} 