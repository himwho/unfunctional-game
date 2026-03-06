using UnityEngine;

/// <summary>
/// Overrides texture tiling and offset per-object using MaterialPropertyBlock.
/// Designed for meshes with cube-projected UVs (where UV coordinates are
/// proportional to real-world dimensions). The tiling value acts as a
/// multiplier — e.g. (1,1) keeps the original density, (2,2) doubles it.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class PerObjectTiling : MonoBehaviour
{
    [Header("Texture Property")]
    [Tooltip("Shader texture property to override. " +
             "URP Lit = _BaseMap, Built-in Standard = _MainTex.")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    [Header("Tiling & Offset")]
    [SerializeField] private Vector2 tiling = Vector2.one;
    [SerializeField] private Vector2 offset = Vector2.zero;

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

    public void Apply()
    {
        if (cachedRenderer == null) return;

        Material mat = cachedRenderer.sharedMaterial;
        if (mat == null) return;

        if (!mat.HasProperty(texturePropertyName))
        {
            Debug.LogWarning(
                $"[PerObjectTiling] Shader '{mat.shader.name}' on '{gameObject.name}' " +
                $"has no property '{texturePropertyName}'.", this);
            return;
        }

        cachedRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetVector(
            texturePropertyName + "_ST",
            new Vector4(tiling.x, tiling.y, offset.x, offset.y));

        cachedRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetTiling(Vector2 newTiling)
    {
        tiling = newTiling;
        Apply();
    }

    public void SetOffset(Vector2 newOffset)
    {
        offset = newOffset;
        Apply();
    }

    public void SetTilingAndOffset(Vector2 newTiling, Vector2 newOffset)
    {
        tiling = newTiling;
        offset = newOffset;
        Apply();
    }

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
        string result = $"[PerObjectTiling] Properties on '{shader.name}':\n";
        int count = shader.GetPropertyCount();

        for (int i = 0; i < count; i++)
        {
            if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                result += $"  - {shader.GetPropertyName(i)}\n";
        }

        Debug.Log(result, this);
    }
}
