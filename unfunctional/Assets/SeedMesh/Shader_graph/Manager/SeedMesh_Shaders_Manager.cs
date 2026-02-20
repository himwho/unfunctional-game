using UnityEngine;

public class CoveringController : MonoBehaviour
{
    public Color coveringColor = Color.white;

    [Range(0f, 1f)]
    public float commonCoveringController = 0f;
    [Range(0f, 1f)]
    public float foliageCoveringController = 0f;
    [Range(0f, 1f)]
    public float variantCoveringController = 0f;
    [Range(0f, 2f)]
    public float windMultiply = 1f;
    [Range(0f, 1f)]
    public float wetness = 0f;
    [Range(-1f, 1f)]
    public float vertexAOStrength = 0f;
    public bool covering = false;

    public Texture2D coveringBaseColor;
    public Texture2D coveringNormal;
    public Texture2D coveringMask;
    public Texture2D coveringHeight;

    public Vector2 smoothnessCoveringRemap = new Vector2(0, 1);
    public float coveringTillingMultiply = 1f;

    private void Start()
    {
        
        UpdateMaterials();
    }

    private void OnValidate()
    {
        UpdateMaterials();
    }

    private void UpdateMaterials()
    {
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        foreach (Material material in allMaterials)
        {
            if (material.HasProperty("_Lock_Covering_Textures") && material.GetFloat("_Lock_Covering_Textures") == 1.0f)
            {
                continue;
            }

            if (material.HasProperty("_Covering_Color"))
            {
                material.SetColor("_Covering_Color", coveringColor);
            }
            if (material.HasProperty("_Covering_Controller_sm"))
            {
                material.SetFloat("_Covering_Controller_sm", commonCoveringController);
            }
            if (material.HasProperty("_Foliage_Covering_Controller_sm"))
            {
                material.SetFloat("_Foliage_Covering_Controller_sm", foliageCoveringController);
            }
            if (material.HasProperty("_Variant_Covering_Controller_sm"))
            {
                material.SetFloat("_Variant_Covering_Controller_sm", variantCoveringController);
            }
            if (material.HasProperty("_Wind_Multiply_sm"))
            {
                material.SetFloat("_Wind_Multiply_sm", windMultiply);
            }
            if (material.HasProperty("_Wetness_sm"))
            {
                material.SetFloat("_Wetness_sm", wetness);
            }
            if (material.HasProperty("_Vertex_AO_sm"))
            {
                material.SetFloat("_Vertex_AO_sm", vertexAOStrength);
            }
            if (material.HasProperty("_Covering_sm"))
            {
                material.SetFloat("_Covering_sm", covering ? 1.0f : 0.0f);
            }
            if (material.HasProperty("_Snow_Base_Color_sm"))
            {
                material.SetTexture("_Snow_Base_Color_sm", coveringBaseColor);
            }
            if (material.HasProperty("_Snow_Normal_sm"))
            {
                material.SetTexture("_Snow_Normal_sm", coveringNormal);
            }
            if (material.HasProperty("_Snow_Mask_sm"))
            {
                material.SetTexture("_Snow_Mask_sm", coveringMask);
            }
            if (material.HasProperty("_Covering_Height"))
            {
                material.SetTexture("_Covering_Height", coveringHeight);
            }
            if (material.HasProperty("_Smoothness_Snow_sm"))
            {
                material.SetVector("_Smoothness_Snow_sm", smoothnessCoveringRemap);
            }
            if (material.HasProperty("_Covering_Tilling_Multiply_sm"))
            {
                material.SetFloat("_Covering_Tilling_Multiply_sm", coveringTillingMultiply);
            }
        }
    }
}

