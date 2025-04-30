using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class PlatformEffectorEditor : EditorWindow
{
    [MenuItem("Tools/Fix Platform Collisions")]
    public static void FixPlatformCollisions()
    {
        // Find all Platform Effectors in the scene using the non-deprecated method
        PlatformEffector2D[] platformEffectors = Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None);
        
        int fixedCount = 0;
        foreach (PlatformEffector2D effector in platformEffectors)
        {
            // Record object for undo
            Undo.RecordObject(effector, "Fix Platform Effector");
            
            // Configure platform to prevent side collisions
            effector.useOneWay = true;
            effector.surfaceArc = 270f;
            effector.rotationalOffset = 180f;
            effector.useOneWayGrouping = true;
            
            EditorUtility.SetDirty(effector);
            fixedCount++;
        }
        
        // Show result message
        if (fixedCount > 0)
            Debug.Log($"Fixed {fixedCount} platform effectors to prevent side collisions");
        else
            Debug.Log("No platform effectors found in the scene");
    }
    
    [MenuItem("Tools/Fix Selected Platform")]
    public static void FixSelectedPlatform()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            Debug.LogWarning("No object selected");
            return;
        }
        
        PlatformEffector2D effector = selectedObject.GetComponent<PlatformEffector2D>();
        if (effector == null)
        {
            Debug.LogWarning("Selected object doesn't have a Platform Effector 2D component");
            return;
        }
        
        // Record object for undo
        Undo.RecordObject(effector, "Fix Platform Effector");
        
        // Configure platform to prevent side collisions
        effector.useOneWay = true;
        effector.surfaceArc = 270f;
        effector.rotationalOffset = 180f;
        effector.useOneWayGrouping = true;
        
        EditorUtility.SetDirty(effector);
        Debug.Log($"Fixed Platform Effector 2D on {selectedObject.name}");
    }
}
#endif 