using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 6: Quicktime event level. Forces the player to do timed key-based 
/// quicktime events in a long sequence. Display key may differ from actual required key.
/// 
/// Attach to a root GameObject in the LEVEL6 scene.
/// </summary>
public class Level5_QuicktimeEvents : LevelManager
{
    [Header("Level 6 - Quicktime Events")]
    public Canvas qteCanvas;
    public Text promptText;                 // Shows "Press [X]!" 
    public Text timerText;                  // Shows remaining time
    public Image progressBar;               // Visual progress through the sequence
    public Text feedbackText;               // "SUCCESS!" / "FAILED!"

    [Header("QTE Settings")]
    public float timePerEvent = 2.0f;       // Seconds to respond
    public float timeBetweenEvents = 0.8f;  // Pause between events
    public int totalEvents = 20;            // How many QTEs in the sequence
    public bool enableMislabeling = true;   // Show wrong key on display

    [Header("Fail Penalty")]
    public bool resetOnFail = true;         // Go back to start on failure
    public int failsBeforeReset = 3;        // Or allow some failures

    [System.Serializable]
    public class QTEEvent
    {
        public KeyCode correctKey;          // The actual key to press
        public KeyCode displayKey;          // The key shown on screen (may differ)
        public string animationTrigger;     // Optional animation to play
    }

    private List<QTEEvent> eventSequence = new List<QTEEvent>();
    private int currentEventIndex = 0;
    private float currentTimer = 0f;
    private bool waitingForInput = false;
    private int failCount = 0;
    private bool sequenceActive = false;

    // All possible keys for QTEs
    private KeyCode[] possibleKeys = new KeyCode[]
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B,
        KeyCode.Space, KeyCode.LeftShift
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Quicktime Gauntlet";
        levelDescription = "Press the buttons. ALL of them. Fast.";

        GenerateSequence();
        StartCoroutine(BeginSequenceAfterDelay(2f));
    }

    private void Update()
    {
        if (levelComplete || !sequenceActive) return;

        if (waitingForInput)
        {
            currentTimer -= Time.deltaTime;

            // Update timer display
            if (timerText != null)
                timerText.text = currentTimer.ToString("F1");

            // Update progress bar
            if (progressBar != null)
                progressBar.fillAmount = (float)currentEventIndex / totalEvents;

            // Time ran out
            if (currentTimer <= 0f)
            {
                OnEventFailed("TOO SLOW!");
                return;
            }

            // Check for any key press
            foreach (KeyCode key in possibleKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    CheckInput(key);
                    return;
                }
            }
        }
    }

    private void GenerateSequence()
    {
        eventSequence.Clear();

        for (int i = 0; i < totalEvents; i++)
        {
            QTEEvent evt = new QTEEvent();
            evt.correctKey = possibleKeys[Random.Range(0, possibleKeys.Length)];

            if (enableMislabeling && Random.value > 0.6f)
            {
                // Show a different key than the correct one (evil)
                do
                {
                    evt.displayKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
                }
                while (evt.displayKey == evt.correctKey);
            }
            else
            {
                evt.displayKey = evt.correctKey;
            }

            eventSequence.Add(evt);
        }
    }

    private IEnumerator BeginSequenceAfterDelay(float delay)
    {
        if (promptText != null)
            promptText.text = "GET READY...";

        yield return new WaitForSeconds(delay);

        sequenceActive = true;
        currentEventIndex = 0;
        failCount = 0;
        ShowCurrentEvent();
    }

    private void ShowCurrentEvent()
    {
        if (currentEventIndex >= eventSequence.Count)
        {
            OnSequenceComplete();
            return;
        }

        QTEEvent evt = eventSequence[currentEventIndex];
        currentTimer = timePerEvent;
        waitingForInput = true;

        if (promptText != null)
        {
            string keyName = evt.displayKey.ToString();
            // Make some keys show confusing names
            if (evt.displayKey == KeyCode.Space)
                keyName = "SPACEBAR";
            else if (evt.displayKey == KeyCode.LeftShift)
                keyName = "SHIFT";

            promptText.text = $"Press [{keyName}]!";
            promptText.color = Color.white;
        }

        if (feedbackText != null)
            feedbackText.text = "";
    }

    private void CheckInput(KeyCode pressedKey)
    {
        QTEEvent evt = eventSequence[currentEventIndex];

        if (pressedKey == evt.correctKey)
        {
            OnEventSuccess();
        }
        else
        {
            OnEventFailed("WRONG KEY!");
        }
    }

    private void OnEventSuccess()
    {
        waitingForInput = false;

        if (feedbackText != null)
        {
            feedbackText.text = "SUCCESS!";
            feedbackText.color = Color.green;
        }

        if (promptText != null)
            promptText.color = Color.green;

        currentEventIndex++;
        StartCoroutine(NextEventAfterDelay(timeBetweenEvents));
    }

    private void OnEventFailed(string reason)
    {
        waitingForInput = false;
        failCount++;

        if (feedbackText != null)
        {
            feedbackText.text = reason;
            feedbackText.color = Color.red;
        }

        if (promptText != null)
            promptText.color = Color.red;

        if (resetOnFail && failCount >= failsBeforeReset)
        {
            // Reset the entire sequence
            StartCoroutine(ResetSequence());
        }
        else
        {
            currentEventIndex++;
            StartCoroutine(NextEventAfterDelay(timeBetweenEvents * 1.5f));
        }
    }

    private IEnumerator NextEventAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowCurrentEvent();
    }

    private IEnumerator ResetSequence()
    {
        if (promptText != null)
            promptText.text = "SEQUENCE FAILED!\nRestarting...";

        yield return new WaitForSeconds(2f);

        currentEventIndex = 0;
        failCount = 0;

        // Regenerate with potentially different mislabeling
        GenerateSequence();

        ShowCurrentEvent();
    }

    private void OnSequenceComplete()
    {
        sequenceActive = false;
        waitingForInput = false;

        if (promptText != null)
            promptText.text = "SEQUENCE COMPLETE!";

        if (progressBar != null)
            progressBar.fillAmount = 1f;

        Debug.Log("[Level6] QTE sequence complete!");
        StartCoroutine(CompleteLevelAfterDelay(1.5f));
    }

    private IEnumerator CompleteLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
    }
}
