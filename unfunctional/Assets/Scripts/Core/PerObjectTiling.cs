using UnityEngine;

/// <summary>
/// Per-object texture tiling. Two modes:
///   - Scale Compensation OFF: simple _ST override via MaterialPropertyBlock.
///   - Scale Compensation ON:  rewrites mesh UVs with world-space triplanar
///     projection so every face of every object gets uniform texel density.
///     Tiling becomes tiles-per-world-unit.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class PerObjectTiling : MonoBehaviour
{
    [Header("Texture Property")]
    [Tooltip("The shader texture property name to override tiling for. " +
             "URP Lit uses _BaseMap. Built-in Standard shader uses _MainTex.")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    [Header("Tiling")]
    [SerializeField] private Vector2 tiling = Vector2.one;

    [Tooltip("When enabled, mesh UVs are rewritten using world-space projection " +
             "so that texture density is uniform across all faces and all objects. " +
             "The tiling value becomes tiles-per-world-unit.")]
    [SerializeField] private bool scaleCompensation = false;

    [Header("Offset")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Debug")]
    [Tooltip("Log the applied tiling info. Useful for troubleshooting.")]
    [SerializeField] private bool debugLog = false;

    [SerializeField, HideInInspector] private Mesh originalSharedMesh;

    private Renderer cachedRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Mesh meshInstance;

    private void OnEnable()
    {
        cachedRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        CacheOriginalMesh();
        Apply();
    }

    private void OnDisable()
    {
        RestoreOriginalMesh();
        CleanupMeshInstance();
    }

    private void OnDestroy()
    {
        RestoreOriginalMesh();
        CleanupMeshInstance();
    }

    private void OnValidate()
    {
        if (cachedRenderer == null)
            cachedRenderer = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        CacheOriginalMesh();
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
                $"does not have a property called '{texturePropertyName}'. " +
                $"Check the property name in your shader.", this);
            return;
        }

        cachedRenderer.GetPropertyBlock(propertyBlock);
        string stProperty = texturePropertyName + "_ST";

        if (scaleCompensation)
        {
            ApplyWorldSpaceUVs();
            propertyBlock.SetVector(stProperty, new Vector4(1, 1, 0, 0));
        }
        else
        {
            RestoreOriginalMesh();
            CleanupMeshInstance();
            propertyBlock.SetVector(stProperty,
                new Vector4(tiling.x, tiling.y, offset.x, offset.y));
        }

        cachedRenderer.SetPropertyBlock(propertyBlock);

        if (debugLog)
        {
            Debug.Log(
                $"[PerObjectTiling] '{gameObject.name}': " +
                $"mode={(scaleCompensation ? "WorldSpaceUV" : "PropertyBlock")}, " +
                $"tiling={tiling}, offset={offset}, scale={transform.lossyScale}", this);
        }
    }

    /// <summary>
    /// Rewrites mesh UVs so each face is projected from world space based on
    /// its normal direction, giving uniform texel density on every surface.
    /// </summary>
    private void ApplyWorldSpaceUVs()
    {
        Mesh sourceMesh = originalSharedMesh;
        if (sourceMesh == null)
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null) sourceMesh = mf.sharedMesh;
        }

        if (sourceMesh == null || sourceMesh.vertexCount == 0) return;

        if (meshInstance == null)
        {
            meshInstance = Instantiate(sourceMesh);
            meshInstance.name = sourceMesh.name + "_PerObjectTiling";
            meshInstance.hideFlags = HideFlags.HideAndDontSave;
        }

        Vector3[] vertices = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;

        if (normals == null || normals.Length != vertices.Length)
        {
            meshInstance.RecalculateNormals();
            normals = meshInstance.normals;
        }

        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(vertices[i]);
            Vector3 worldNormal = transform.TransformDirection(normals[i]).normalized;

            float absX = Mathf.Abs(worldNormal.x);
            float absY = Mathf.Abs(worldNormal.y);
            float absZ = Mathf.Abs(worldNormal.z);

            Vector2 uv;
            if (absY >= absX && absY >= absZ)
                uv = new Vector2(worldPos.x, worldPos.z);   // up/down face → XZ
            else if (absX >= absZ)
                uv = new Vector2(worldPos.z, worldPos.y);   // left/right face → ZY
            else
                uv = new Vector2(worldPos.x, worldPos.y);   // front/back face → XY

            uvs[i] = new Vector2(uv.x * tiling.x + offset.x,
                                 uv.y * tiling.y + offset.y);
        }

        meshInstance.uv = uvs;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
            meshFilter.sharedMesh = meshInstance;
    }

    private void CacheOriginalMesh()
    {
        if (originalSharedMesh != null) return;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null && mf.sharedMesh != meshInstance)
            originalSharedMesh = mf.sharedMesh;
    }

    private void RestoreOriginalMesh()
    {
        if (originalSharedMesh == null) return;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh == meshInstance)
            mf.sharedMesh = originalSharedMesh;
    }

    private void CleanupMeshInstance()
    {
        if (meshInstance == null) return;

        if (Application.isPlaying)
            Destroy(meshInstance);
        else
            DestroyImmediate(meshInstance);

        meshInstance = null;
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
