using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight world-space keypad button used by Level 4's physical keypad input.
/// It forwards presses into KeypadController and plays a small button travel animation.
/// </summary>
public class WorldKeypadButton : MonoBehaviour
{
    public enum ButtonAction
    {
        Digit,
        Clear,
        Submit
    }

    [Header("Keypad Action")]
    [SerializeField] private ButtonAction buttonAction = ButtonAction.Digit;
    [SerializeField] private int digitValue = 0;
    [SerializeField] private string promptLabel = "";

    [Header("Press Animation")]
    [SerializeField] private Vector3 pressOffset = new Vector3(0f, 0f, 0.0025f);
    [SerializeField] private float pressDuration = 0.05f;
    [SerializeField] private float holdDuration = 0.06f;

    private bool isAnimating;
    private Vector3 restLocalPosition;
    private bool hasRestLocalPosition;

    private void Awake()
    {
        CacheRestPosition();
    }

    private void OnEnable()
    {
        CacheRestPosition();
    }

    public void ConfigureDigit(int digit, string label = null)
    {
        buttonAction = ButtonAction.Digit;
        digitValue = Mathf.Clamp(digit, 0, 9);
        promptLabel = string.IsNullOrWhiteSpace(label) ? digitValue.ToString() : label;
    }

    public void ConfigureAction(ButtonAction action, string label = null)
    {
        buttonAction = action;
        if (!string.IsNullOrWhiteSpace(label))
        {
            promptLabel = label;
            return;
        }

        promptLabel = action switch
        {
            ButtonAction.Clear => "CLR",
            ButtonAction.Submit => "OK",
            _ => digitValue.ToString()
        };
    }

    public string GetPromptLabel()
    {
        if (!string.IsNullOrWhiteSpace(promptLabel))
            return promptLabel;

        return buttonAction switch
        {
            ButtonAction.Clear => "CLR",
            ButtonAction.Submit => "OK",
            _ => digitValue.ToString()
        };
    }

    public bool Press(KeypadController keypad)
    {
        if (keypad == null || isAnimating)
            return false;

        switch (buttonAction)
        {
            case ButtonAction.Digit:
                keypad.PressDigit(digitValue);
                break;
            case ButtonAction.Clear:
                keypad.PressClear();
                break;
            case ButtonAction.Submit:
                keypad.PressSubmit();
                break;
        }

        StartCoroutine(AnimatePress());
        return true;
    }

    private void CacheRestPosition()
    {
        if (hasRestLocalPosition) return;
        restLocalPosition = transform.localPosition;
        hasRestLocalPosition = true;
    }

    private IEnumerator AnimatePress()
    {
        isAnimating = true;
        CacheRestPosition();

        Vector3 pressedLocalPosition = restLocalPosition + pressOffset;

        yield return MoveButton(restLocalPosition, pressedLocalPosition, pressDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return MoveButton(pressedLocalPosition, restLocalPosition, pressDuration);

        transform.localPosition = restLocalPosition;
        isAnimating = false;
    }

    private IEnumerator MoveButton(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            transform.localPosition = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.localPosition = to;
    }
}
