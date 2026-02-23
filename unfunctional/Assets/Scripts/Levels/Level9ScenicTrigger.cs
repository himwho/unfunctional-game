using UnityEngine;

/// <summary>
/// Trigger for scenic text along the hallway.
/// </summary>
public class Level9ScenicTrigger : MonoBehaviour
{
    public string message;
    public Level9_WalkingSimulator levelManager;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            triggered = true;
            if (levelManager != null)
                levelManager.ShowScenicText(message);
        }
    }
}
