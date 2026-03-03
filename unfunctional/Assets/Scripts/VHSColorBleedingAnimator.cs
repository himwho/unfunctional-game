using UnityEngine;
using TMPro;

/// <summary>
/// Dynamically animates the _Colorbleedingamount property on VHS UI shader materials.
/// Attach to a GameObject with a MeshRenderer or TextMeshPro that uses the VHSUI/VHSUIText shader.
/// </summary>
public class VHSColorBleedingAnimator : MonoBehaviour
{
    [Header("Color Bleeding")]
    [Tooltip("Minimum color bleeding amount (shader range 0–0.01)")]
    [Range(0f, 0.01f)]
    public float minAmount = 0f;

    [Tooltip("Maximum color bleeding amount (shader range 0–0.01)")]
    [Range(0f, 0.01f)]
    public float maxAmount = 0.008f;

    [Header("Animation")]
    [Tooltip("Speed of the color bleeding pulse")]
    public float speed = 1f;

    [Tooltip("Use sine wave for smooth oscillation (else linear ping-pong)")]
    public bool useSineWave = true;

    private const string ShaderPropertyName = "_Colorbleedingamount";

    private Material materialInstance;

    private void Awake()
    {
        var tmpText = GetComponent<TMP_Text>();
        if (tmpText != null && tmpText.fontSharedMaterial != null)
        {
            materialInstance = tmpText.fontMaterial;
            return;
        }

        var r = GetComponent<Renderer>();
        if (r != null && r.sharedMaterial != null)
            materialInstance = r.material;
    }

    private void Update()
    {
        if (materialInstance == null || !materialInstance.HasProperty(ShaderPropertyName))
            return;

        float t = useSineWave
            ? (Mathf.Sin(Time.time * speed) + 1f) * 0.5f  // 0..1 smooth
            : Mathf.PingPong(Time.time * speed, 1f);       // 0..1 linear

        float amount = Mathf.Lerp(minAmount, maxAmount, t);
        materialInstance.SetFloat(ShaderPropertyName, amount);
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
            Destroy(materialInstance);
    }
}
