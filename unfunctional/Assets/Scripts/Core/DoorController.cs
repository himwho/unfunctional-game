using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable door prefab controller. Supports two unlock methods that can be
/// toggled per-instance:
///   - Keypad: a wall-mounted panel with sticky notes (used in Level 4)
///   - Keyhole: a physical lock on the door frame (usable in any level)
///
/// Each level script decides when/how to call OpenDoor(). This component
/// only manages the visual configuration and the open/close animation.
///
/// Prefab hierarchy expected:
///   Door (root, this component)
///     DoorPanel          -- the sliding door slab
///     FrameLeft          -- left side of the door frame
///     FrameRight         -- right side of the door frame
///     FrameTop           -- top of the door frame
///     KeypadMount        -- parent for keypad visuals
///       KeypadPanel      -- the box on the wall
///       StickyNote_Email -- yellow note quad
///       StickyNote_Warn  -- orange note quad
///       StickyNotePoint  -- empty transform for interaction range
///     KeyholeLockMount   -- parent for keyhole visuals
///       LockPlate        -- metal plate around the keyhole
///       KeyholeSlot      -- the keyhole opening
/// </summary>
public class DoorController : MonoBehaviour
{
    // =========================================================================
    // Configuration
    // =========================================================================

    public enum UnlockMethod { None, Keypad, Keyhole }

    [Header("Unlock Method")]
    [Tooltip("Which unlock mechanism is visible on this door instance.")]
    public UnlockMethod unlockMethod = UnlockMethod.Keypad;

    [Header("Door Animation")]
    public float doorOpenSpeed = 2f;
    [Tooltip("How far the door slides up (world units) when opened.")]
    public float doorOpenDistance = 3f;

    // =========================================================================
    // Child references (wired by the editor prefab builder)
    // =========================================================================

    [Header("Door Parts")]
    public GameObject doorPanel;       // The door slab that moves
    public GameObject frameLeft;
    public GameObject frameRight;
    public GameObject frameTop;

    [Header("Keypad Children")]
    public GameObject keypadMount;     // Parent GO toggled on/off
    public GameObject keypadPanel;     // The visual keypad box (raycast target)
    public GameObject stickyNoteEmail; // Yellow sticky note quad
    public GameObject stickyNoteWarn;  // Orange sticky note quad
    public Transform  stickyNotePoint; // Interaction center point

    [Header("Keyhole Children")]
    public GameObject keyholeLockMount; // Parent GO toggled on/off
    public GameObject lockPlate;        // Metal plate
    public GameObject keyholeSlot;      // The keyhole opening

    // =========================================================================
    // Runtime state
    // =========================================================================

    private Vector3 doorClosedPos;
    private Vector3 doorOpenPos;
    private bool isOpen = false;
    private bool isAnimating = false;

    /// <summary>True after OpenDoor() has been called and completed.</summary>
    public bool IsOpen => isOpen;

    /// <summary>True while the door is mid-animation.</summary>
    public bool IsAnimating => isAnimating;

    // =========================================================================
    // Lifecycle
    // =========================================================================

    private void Awake()
    {
        ApplyUnlockMethod();

        if (doorPanel != null)
        {
            doorClosedPos = doorPanel.transform.position;
            doorOpenPos = doorClosedPos + Vector3.up * doorOpenDistance;
        }
    }

    /// <summary>
    /// Enables/disables the keypad and keyhole mounts based on the current
    /// unlockMethod value. Safe to call at edit-time or runtime.
    /// </summary>
    public void ApplyUnlockMethod()
    {
        if (keypadMount != null)
            keypadMount.SetActive(unlockMethod == UnlockMethod.Keypad);

        if (keyholeLockMount != null)
            keyholeLockMount.SetActive(unlockMethod == UnlockMethod.Keyhole);
    }

    // =========================================================================
    // Door animation
    // =========================================================================

    /// <summary>
    /// Slides the door panel upward. Call from your level script when the
    /// player has satisfied the unlock condition.
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen || isAnimating) return;
        StartCoroutine(AnimateDoor(doorClosedPos, doorOpenPos));
    }

    /// <summary>
    /// Slides the door panel back down (if you ever need to re-lock).
    /// </summary>
    public void CloseDoor()
    {
        if (!isOpen || isAnimating) return;
        StartCoroutine(AnimateDoor(doorOpenPos, doorClosedPos));
    }

    private IEnumerator AnimateDoor(Vector3 from, Vector3 to)
    {
        isAnimating = true;

        if (doorPanel != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * doorOpenSpeed;
                doorPanel.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            doorPanel.transform.position = to;
        }

        isOpen = !isOpen;
        isAnimating = false;
    }

    /// <summary>
    /// Performs a quick shake on the door panel (useful for "locked" feedback).
    /// </summary>
    public void ShakeDoor(float duration = 0.3f, float intensity = 0.03f)
    {
        if (doorPanel != null && !isAnimating)
            StartCoroutine(ShakeRoutine(duration, intensity));
    }

    private IEnumerator ShakeRoutine(float duration, float intensity)
    {
        if (doorPanel == null) yield break;

        Vector3 originalPos = doorPanel.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = originalPos.x + Random.Range(-intensity, intensity);
            float z = originalPos.z + Random.Range(-intensity, intensity);
            doorPanel.transform.position = new Vector3(x, originalPos.y, z);
            yield return null;
        }

        doorPanel.transform.position = originalPos;
    }

    // =========================================================================
    // Helpers for level scripts
    // =========================================================================

    /// <summary>
    /// Recalculate cached positions. Call this if you reposition the door
    /// after Awake (e.g. when instantiating from a prefab and moving it).
    /// </summary>
    public void RecalculatePositions()
    {
        if (doorPanel != null)
        {
            doorClosedPos = doorPanel.transform.position;
            doorOpenPos = doorClosedPos + Vector3.up * doorOpenDistance;
        }
    }
}
