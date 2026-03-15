using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which keypad button the fingertip trigger is currently overlapping.
/// </summary>
public class FingerTipKeypadDetector : MonoBehaviour
{
    private readonly HashSet<WorldKeypadButton> overlappingButtons = new HashSet<WorldKeypadButton>();

    public WorldKeypadButton CurrentTouchedButton
    {
        get
        {
            foreach (WorldKeypadButton button in overlappingButtons)
                return button;
            return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        WorldKeypadButton button = other.GetComponentInParent<WorldKeypadButton>();
        if (button != null)
            overlappingButtons.Add(button);
    }

    private void OnTriggerStay(Collider other)
    {
        WorldKeypadButton button = other.GetComponentInParent<WorldKeypadButton>();
        if (button != null)
            overlappingButtons.Add(button);
    }

    private void OnTriggerExit(Collider other)
    {
        WorldKeypadButton button = other.GetComponentInParent<WorldKeypadButton>();
        if (button != null)
            overlappingButtons.Remove(button);
    }

    public void Clear()
    {
        overlappingButtons.Clear();
    }
}
