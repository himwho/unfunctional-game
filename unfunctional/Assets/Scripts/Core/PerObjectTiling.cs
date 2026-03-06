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
    /// <summary>
    /// Which plane of the object the primary textured surface lies on.
    /// Auto detects this from the mesh's world-space bounds (thinnest axis = normal).
    /// </summary>
    public enum SurfacePlane
    {
        Auto,
        XY,
        XZ,
        YZ,
    }

    [Header("Texture Property")]
    [Tooltip("The shader texture property name to override tiling for. " +
             "URP Lit uses _BaseMap. Built-in Standard shader uses _MainTex.")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    [Header("Tiling")]
    [SerializeField] private Vector2 tiling = Vector2.one;

    [Tooltip("When enabled, tiling is multiplied by the object's world-space surface " +
             "dimensions (mesh bounds * scale) so texture density stays consistent " +
             "across differently sized objects. The tiling value becomes tiles-per-unit.")]
    [SerializeField] private bool scaleCompensation = false;

    [Tooltip("Which plane of the object the main textured surface lies on. " +
             "Auto detects from mesh bounds (thinnest axis = surface normal). " +
             "XY = wall facing Z, XZ = floor/ceiling, YZ = side wall facing X.")]
    [SerializeField] private SurfacePlane surfacePlane = SurfacePlane.Auto;

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

        if (!mat.HasProperty(texturePropertyName))
        {
            Debug.LogWarning(
                $"[PerObjectTiling] Shader '{mat.shader.name}' on '{gameObject.name}' " +
                $"does not have a property called '{texturePropertyName}'. " +
                $"Check the property name in your shader.", this);
            return;
        }

        cachedRenderer.GetPropertyBlock(propertyBlock);

        string stProperty = texturePropertyName + "_ST";

        Vector2 finalTiling = tiling;
        if (scaleCompensation)
        {
            Vector3 worldSize = GetWorldSize();
            ResolveSurfaceAxes(worldSize, out int uAxis, out int vAxis);
            finalTiling.x *= Mathf.Abs(AxisComponent(worldSize, uAxis));
            finalTiling.y *= Mathf.Abs(AxisComponent(worldSize, vAxis));
        }

        Vector4 tilingOffset = new Vector4(finalTiling.x, finalTiling.y, offset.x, offset.y);
        propertyBlock.SetVector(stProperty, tilingOffset);

        cachedRenderer.SetPropertyBlock(propertyBlock);

        if (debugLog)
        {
            Vector3 ws = GetWorldSize();
            ResolveSurfaceAxes(ws, out int dbgU, out int dbgV);
            string axisNames = "XYZ";
            Debug.Log(
                $"[PerObjectTiling] '{gameObject.name}': set {stProperty} = {tilingOffset} " +
                $"(shader: {mat.shader.name}, worldSize: {ws}, " +
                $"surfacePlane: {surfacePlane}, " +
                $"axes: U→{axisNames[dbgU]} V→{axisNames[dbgV]}, " +
                $"scale: {transform.lossyScale})", this);
        }
    }

    /// <summary>
    /// World-space dimensions: mesh bounds size * absolute lossy scale.
    /// Falls back to lossy scale alone when no mesh is available.
    /// </summary>
    private Vector3 GetWorldSize()
    {
        Mesh mesh = GetMesh();
        Vector3 boundsSize = mesh != null ? mesh.bounds.size : Vector3.one;
        Vector3 s = transform.lossyScale;
        return new Vector3(
            boundsSize.x * Mathf.Abs(s.x),
            boundsSize.y * Mathf.Abs(s.y),
            boundsSize.z * Mathf.Abs(s.z));
    }

    private Mesh GetMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) return mf.sharedMesh;

        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        if (smr != null && smr.sharedMesh != null) return smr.sharedMesh;

        return null;
    }

    /// <summary>
    /// Determines which world axes map to UV U and V based on the chosen
    /// surface plane. In Auto mode the thinnest world-space axis is treated
    /// as the surface normal and the other two become U and V.
    /// </summary>
    private void ResolveSurfaceAxes(Vector3 worldSize, out int uAxis, out int vAxis)
    {
        switch (surfacePlane)
        {
            case SurfacePlane.XY:
                uAxis = 0; vAxis = 1;
                return;
            case SurfacePlane.XZ:
                uAxis = 0; vAxis = 2;
                return;
            case SurfacePlane.YZ:
                uAxis = 1; vAxis = 2;
                return;
            default:
                break;
        }

        float absX = Mathf.Abs(worldSize.x);
        float absY = Mathf.Abs(worldSize.y);
        float absZ = Mathf.Abs(worldSize.z);

        if (absY <= absX && absY <= absZ)
        {
            uAxis = 0; vAxis = 2; // XZ — floor / ceiling
        }
        else if (absZ <= absX && absZ <= absY)
        {
            uAxis = 0; vAxis = 1; // XY — wall facing Z
        }
        else
        {
            uAxis = 1; vAxis = 2; // YZ — side wall facing X
        }
    }

    private static float AxisComponent(Vector3 v, int axis)
    {
        switch (axis)
        {
            case 0:  return v.x;
            case 1:  return v.y;
            case 2:  return v.z;
            default: return 1f;
        }
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
