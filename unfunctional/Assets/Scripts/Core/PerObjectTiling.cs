using UnityEngine;

/// <summary>
/// Overrides texture tiling and offset per-object using MaterialPropertyBlock.
/// Attach this to any GameObject with a Renderer to give it unique tiling
/// without affecting other objects that share the same material.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class PerObjectTiling : MonoBehaviour
{
    [Header("Texture Property")]
    [Tooltip("The shader texture property name to override tiling for. " +
             "Built-in Standard shader uses _MainTex. URP Lit uses _BaseMap.")]
    [SerializeField] private string texturePropertyName = "_MainTex";

    [Header("Tiling")]
    [SerializeField] private Vector2 tiling = Vector2.one;

    [Header("Offset")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Debug")]
    [Tooltip("Log the shader property name being set. Useful for troubleshooting.")]
    [SerializeField] private bool debugLog = false;

    private Renderer cachedRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void OnEnable()
    {
        cachedRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        Apply();
    }

    private void OnValidate()
    {
        if (cachedRenderer == null)
            cachedRenderer = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        Apply();
    }

    /// <summary>
    /// Applies the tiling and offset override to this object's renderer.
    /// The _ST convention is a Vector4: (tiling.x, tiling.y, offset.x, offset.y).
    /// </summary>
    public void Apply()
    {
        if (cachedRenderer == null) return;

        Material mat = cachedRenderer.sharedMaterial;
        if (mat == null) return;

        // Verify the texture property exists on this shader
        if (!mat.HasProperty(texturePropertyName))
        {
            Debug.LogWarning(
                $"[PerObjectTiling] Shader '{mat.shader.name}' on '{gameObject.name}' " +
                $"does not have a property called '{texturePropertyName}'. " +
                $"Check the property name in your shader.", this);
            return;
        }

        // Get existing property block so we don't overwrite other per-object overrides
        cachedRenderer.GetPropertyBlock(propertyBlock);

        // Set the _ST vector (tiling.x, tiling.y, offset.x, offset.y)
        string stProperty = texturePropertyName + "_ST";
        Vector4 tilingOffset = new Vector4(tiling.x, tiling.y, offset.x, offset.y);
        propertyBlock.SetVector(stProperty, tilingOffset);

        cachedRenderer.SetPropertyBlock(propertyBlock);

        if (debugLog)
        {
            Debug.Log(
                $"[PerObjectTiling] '{gameObject.name}': set {stProperty} = {tilingOffset} " +
                $"(shader: {mat.shader.name})", this);
        }
    }

    /// <summary>
    /// Set tiling from code at runtime.
    /// </summary>
    public void SetTiling(Vector2 newTiling)
    {
        tiling = newTiling;
        Apply();
    }

    /// <summary>
    /// Set offset from code at runtime.
    /// </summary>
    public void SetOffset(Vector2 newOffset)
    {
        offset = newOffset;
        Apply();
    }

    /// <summary>
    /// Set both tiling and offset from code at runtime.
    /// </summary>
    public void SetTilingAndOffset(Vector2 newTiling, Vector2 newOffset)
    {
        tiling = newTiling;
        offset = newOffset;
        Apply();
    }

    /// <summary>
    /// Logs all texture property names on this object's material for debugging.
    /// Call from a context menu or Inspector button.
    /// </summary>
    [ContextMenu("Log Shader Texture Properties")]
    private void LogShaderProperties()
    {
        Material mat = GetComponent<Renderer>()?.sharedMaterial;
        if (mat == null)
        {
            Debug.LogWarning("[PerObjectTiling] No material found.", this);
            return;
        }

        Shader shader = mat.shader;
        string result = $"[PerObjectTiling] Texture properties on shader '{shader.name}':\n";
        int count = shader.GetPropertyCount();

        for (int i = 0; i < count; i++)
        {
            if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
            {
                result += $"  - {shader.GetPropertyName(i)}\n";
            }
        }

        Debug.Log(result, this);
    }
}
