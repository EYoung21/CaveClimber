using UnityEngine;

public class TestPlatform : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Test Platform: Collision detected with {collision.gameObject.name}");
        // Check if it's the player
        if (collision.gameObject.GetComponent<PlayerController>() != null)
        {
            Debug.Log("Test Platform: This is the player!");
            // Check which way the collision is happening
            if (collision.contacts.Length > 0)
            {
                Debug.Log($"Test Platform: Contact normal = {collision.contacts[0].normal}");
                if (collision.contacts[0].normal.y > 0.5f)
                {
                    Debug.Log("Test Platform: Player is above this platform!");
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Test Platform: Trigger entered by {collision.gameObject.name}");
    }
    
    // Draw a debug visual in scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>()?.bounds.size ?? Vector3.one);
    }
} 