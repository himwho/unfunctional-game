using UnityEngine;

/// <summary>
/// Cycles a child Light through all colors of the rainbow in real time.
/// Also updates the emissive color of the light_ON child to match.
/// Attach to a parent object (e.g. LED_Light_7) that has a Light component on itself or a child.
/// Works with realtime/mixed lighting.
/// </summary>
public class RainbowLightCycle : MonoBehaviour
{
    [Tooltip("Lights to cycle. If not set, uses all Lights found in this object or its children (point, spot, etc.).")]
    public Light[] targetLights;

    [Tooltip("Child object with emissive material to match (e.g. light_ON). If not set, searches for 'light_ON'.")]
    public Renderer emissiveRenderer;

    [Tooltip("Time in seconds to complete one full rainbow cycle.")]
    public float cycleDuration = 4f;

    [Tooltip("Emissive intensity for the light_ON material (HDR). Matches LedLightWhite default (4) if unspecified.")]
    public float emissiveIntensity = 4f;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private Material _emissiveMaterial;
    private Light[] _lights;

    private void Start()
    {
        if (targetLights == null || targetLights.Length == 0)
            _lights = GetComponentsInChildren<Light>();
        else
            _lights = targetLights;

        if (emissiveRenderer == null)
        {
            Transform lightOn = transform.Find("light_ON");
            if (lightOn != null)
                emissiveRenderer = lightOn.GetComponent<Renderer>();
        }

        if (emissiveRenderer != null)
        {
            _emissiveMaterial = emissiveRenderer.material;
            _emissiveMaterial.EnableKeyword("_EMISSION");
        }

        if (_lights != null && _lights.Length > 0)
            enabled = true;
        else
        {
            Debug.LogWarning($"[RainbowLightCycle] No Light found on {gameObject.name} or its children.", this);
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_emissiveMaterial != null)
            Destroy(_emissiveMaterial);
    }

    private void Update()
    {
        if (_lights == null || _lights.Length == 0) return;

        float h = (Time.time / cycleDuration) % 1f;
        Color color = Color.HSVToRGB(h, 1f, 1f);

        for (int i = 0; i < _lights.Length; i++)
        {
            if (_lights[i] != null)
                _lights[i].color = color;
        }

        if (_emissiveMaterial != null)
        {
            Color emissiveColor = color * emissiveIntensity;
            _emissiveMaterial.SetColor(EmissionColorId, emissiveColor);
        }
    }
}
