using UnityEngine;

public class ClimbableSurface : MonoBehaviour
{
    [Header("Surface Settings")]
    public float gripStrength = 1f;
    public bool isBreakable = false;
    public float breakThreshold = 10f;
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBreakable)
        {
            // Check if the impact force is strong enough to break the surface
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > breakThreshold)
            {
                // TODO: Add break effect and destroy the surface
                Destroy(gameObject);
            }
        }
    }
} 