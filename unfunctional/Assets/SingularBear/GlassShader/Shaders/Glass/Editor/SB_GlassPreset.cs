// ============================================================================
//  SingularBear Glass Shader - Preset System
//  Copyright (c) SingularBear - All Rights Reserved
//  Version 1.0 - Preset ScriptableObject
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SingularBear.Glass
{
    /// <summary>
    /// ScriptableObject that stores all shader parameters for quick preset application.
    /// Drag & drop onto any SB Glass material to instantly apply the look.
    /// </summary>
    [CreateAssetMenu(fileName = "New Glass Preset", menuName = "SingularBear/Glass Shader Preset", order = 101)]
    public class SB_GlassPreset : ScriptableObject
    {
        // ====================================================================
        // PRESET METADATA
        // ====================================================================
        [Header("Preset Info")]
        [Tooltip("Name displayed in the preset selector")]
        public string presetName = "New Preset";
        
        [TextArea(2, 4)]
        [Tooltip("Description of this preset's visual style")]
        public string description = "";
        
        [Tooltip("Category for organization (e.g., Clear, Frosted, Stylized, Sci-Fi, etc.)")]
        public string category = "General";
        
        [Tooltip("Preview thumbnail (optional)")]
        public Texture2D thumbnail;
        
        [Tooltip("Author name")]
        public string author = "";
        
        [Tooltip("Creation date")]
        public string creationDate = "";
        
        // ====================================================================
        // EDITOR STATE (Optional - for restoring UI state when applying preset)
        // ====================================================================
        [Header("Editor State (Optional)")]
        [Tooltip("Store editor tab state with preset")]
        public bool saveEditorState = false;
        
        [Tooltip("Active category tab (0=All, 1=Base, 2=Optical, 3=Surface, 4=Effects, 5=Rendering)")]
        [Range(0, 5)]
        public int editorCategory = 0;
        
        [Tooltip("Hide unused sections")]
        public bool editorHideUnused = false;
        
        [Tooltip("Expanded section names")]
        public List<string> expandedSections = new List<string>();
        
        // ====================================================================
        // SHADER DATA
        // ====================================================================
        [Header("Shader Data (Auto-generated)")]
        [SerializeField] private List<FloatProperty> floatProperties = new List<FloatProperty>();
        [SerializeField] private List<ColorProperty> colorProperties = new List<ColorProperty>();
        [SerializeField] private List<VectorProperty> vectorProperties = new List<VectorProperty>();
        [SerializeField] private List<TextureProperty> textureProperties = new List<TextureProperty>();
        [SerializeField] private List<string> enabledKeywords = new List<string>();
        
        // ====================================================================
        // SERIALIZABLE PROPERTY CLASSES
        // ====================================================================
        [Serializable]
        public class FloatProperty
        {
            public string name;
            public float value;
            
            public FloatProperty(string name, float value)
            {
                this.name = name;
                this.value = value;
            }
        }
        
        [Serializable]
        public class ColorProperty
        {
            public string name;
            public Color value;
            
            public ColorProperty(string name, Color value)
            {
                this.name = name;
                this.value = value;
            }
        }
        
        [Serializable]
        public class VectorProperty
        {
            public string name;
            public Vector4 value;
            
            public VectorProperty(string name, Vector4 value)
            {
                this.name = name;
                this.value = value;
            }
        }
        
        [Serializable]
        public class TextureProperty
        {
            public string name;
            public Texture texture;
            public Vector2 scale;
            public Vector2 offset;
            
            public TextureProperty(string name, Texture texture, Vector2 scale, Vector2 offset)
            {
                this.name = name;
                this.texture = texture;
                this.scale = scale;
                this.offset = offset;
            }
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Save all properties from a material to this preset
        /// </summary>
        public void SaveFromMaterial(Material material)
        {
            if (material == null || material.shader == null) return;
            
            // Clear existing data
            floatProperties.Clear();
            colorProperties.Clear();
            vectorProperties.Clear();
            textureProperties.Clear();
            enabledKeywords.Clear();
            
            // Set creation date
            creationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            
            // ================================================================
            // TEXTURES TO EXCLUDE (Model-specific, not part of the "look")
            // These are unique to each model and should not be overwritten
            // ================================================================
            HashSet<string> excludedTextures = new HashSet<string>()
            {
                // Main textures (model-specific)
                "_MainTex",
                "_BaseMap",
                "_BaseColorMap",
                
                // Normal Maps (model-specific)
                "_BumpMap",
                "_NormalMap",
                "_DetailNormalMap",
                
                // PBR Textures (model-specific)
                "_MetallicGlossMap",
                "_OcclusionMap",
                
                // Thickness/Mask maps (model-specific)
                "_ThicknessMap",
                
                // Detail Maps (can be model-specific)
                "_DetailAlbedoMap",
            };
            
            Shader shader = material.shader;
            int propertyCount = shader.GetPropertyCount();
            
            for (int i = 0; i < propertyCount; i++)
            {
                string propName = shader.GetPropertyName(i);
                var propType = shader.GetPropertyType(i);
                
                // Skip internal Unity properties
                if (propName.StartsWith("unity_") || propName.StartsWith("_MainTex_")) continue;
                
                switch (propType)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        floatProperties.Add(new FloatProperty(propName, material.GetFloat(propName)));
                        break;
                        
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        colorProperties.Add(new ColorProperty(propName, material.GetColor(propName)));
                        break;
                        
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        vectorProperties.Add(new VectorProperty(propName, material.GetVector(propName)));
                        break;
                        
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        // Skip model-specific textures
                        if (excludedTextures.Contains(propName)) continue;
                        
                        Texture tex = material.GetTexture(propName);
                        Vector2 scale = material.GetTextureScale(propName);
                        Vector2 offset = material.GetTextureOffset(propName);
                        textureProperties.Add(new TextureProperty(propName, tex, scale, offset));
                        break;
                }
            }
            
            // Save enabled keywords
            foreach (string keyword in material.shaderKeywords)
            {
                if (keyword.StartsWith("_SB_"))
                {
                    enabledKeywords.Add(keyword);
                }
            }
            
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// Apply this preset to a material
        /// </summary>
        public void ApplyToMaterial(Material material)
        {
            if (material == null) return;
            
            #if UNITY_EDITOR
            Undo.RecordObject(material, "Apply Glass Preset");
            #endif
            
            // Disable all SB shader keywords first
            foreach (string keyword in material.shaderKeywords)
            {
                if (keyword.StartsWith("_SB_"))
                {
                    material.DisableKeyword(keyword);
                }
            }
            
            // Apply float properties
            foreach (var prop in floatProperties)
            {
                if (material.HasProperty(prop.name))
                {
                    material.SetFloat(prop.name, prop.value);
                }
            }
            
            // Apply color properties
            foreach (var prop in colorProperties)
            {
                if (material.HasProperty(prop.name))
                {
                    material.SetColor(prop.name, prop.value);
                }
            }
            
            // Apply vector properties
            foreach (var prop in vectorProperties)
            {
                if (material.HasProperty(prop.name))
                {
                    material.SetVector(prop.name, prop.value);
                }
            }
            
            // Apply texture properties
            foreach (var prop in textureProperties)
            {
                if (material.HasProperty(prop.name))
                {
                    material.SetTexture(prop.name, prop.texture);
                    material.SetTextureScale(prop.name, prop.scale);
                    material.SetTextureOffset(prop.name, prop.offset);
                }
            }
            
            // Enable saved keywords
            foreach (string keyword in enabledKeywords)
            {
                material.EnableKeyword(keyword);
            }
            
            #if UNITY_EDITOR
            EditorUtility.SetDirty(material);
            #endif
        }
        
        /// <summary>
        /// Get a summary of what this preset contains
        /// </summary>
        public string GetSummary()
        {
            int featureCount = enabledKeywords.Count;
            return $"{presetName}\n" +
                   $"Category: {category}\n" +
                   $"Features: {featureCount} enabled\n" +
                   $"Properties: {floatProperties.Count + colorProperties.Count + vectorProperties.Count}";
        }
        
        /// <summary>
        /// Check if this preset has any data
        /// </summary>
        public bool HasData()
        {
            return floatProperties.Count > 0 || colorProperties.Count > 0 || 
                   vectorProperties.Count > 0 || enabledKeywords.Count > 0;
        }
        
        /// <summary>
        /// Get list of enabled feature keywords for display
        /// </summary>
        public List<string> GetEnabledFeatures()
        {
            List<string> features = new List<string>();
            foreach (string keyword in enabledKeywords)
            {
                if (keyword.StartsWith("_SB_"))
                {
                    // Convert _SB_CHROMATIC_ABERRATION to "Chromatic Aberration"
                    string feature = keyword.Replace("_SB_", "").Replace("_", " ");
                    feature = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(feature.ToLower());
                    features.Add(feature);
                }
            }
            return features;
        }
    }
    
    // ====================================================================
    // CUSTOM EDITOR FOR THE PRESET
    // ====================================================================
    #if UNITY_EDITOR
    [CustomEditor(typeof(SB_GlassPreset))]
    public class SB_GlassPresetEditor : UnityEditor.Editor
    {
        private bool showFeatures = true;
        
        // Cached GUIStyle to avoid GC allocations
        private static GUIStyle s_titleStyle = null;
        
        public override void OnInspectorGUI()
        {
            SB_GlassPreset preset = (SB_GlassPreset)target;
            
            // Header
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                
                // Cache title style
                if (s_titleStyle == null)
                {
                    s_titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 16,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                
                EditorGUILayout.LabelField("Glass Shader Preset", s_titleStyle, GUILayout.Height(25));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.Space(5);
            
            // Thumbnail preview
            if (preset.thumbnail != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(preset.thumbnail, GUILayout.Width(128), GUILayout.Height(128));
                    GUILayout.FlexibleSpace();
                }
            }
            
            // Preset info
            EditorGUILayout.Space(5);
            preset.presetName = EditorGUILayout.TextField("Preset Name", preset.presetName);
            preset.category = EditorGUILayout.TextField("Category", preset.category);
            preset.author = EditorGUILayout.TextField("Author", preset.author);
            
            EditorGUILayout.LabelField("Description");
            preset.description = EditorGUILayout.TextArea(preset.description, GUILayout.Height(50));
            
            preset.thumbnail = (Texture2D)EditorGUILayout.ObjectField("Thumbnail", preset.thumbnail, typeof(Texture2D), false);
            
            if (!string.IsNullOrEmpty(preset.creationDate))
            {
                EditorGUILayout.LabelField("Created", preset.creationDate);
            }
            
            EditorGUILayout.Space(10);
            
            // Features foldout
            if (preset.HasData())
            {
                showFeatures = EditorGUILayout.Foldout(showFeatures, $"Enabled Features ({preset.GetEnabledFeatures().Count})", true);
                if (showFeatures)
                {
                    EditorGUI.indentLevel++;
                    foreach (string feature in preset.GetEnabledFeatures())
                    {
                        EditorGUILayout.LabelField("\u2022 " + feature);
                    }
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space(5);
                
                // Quick copy button
                EditorGUILayout.Space(10);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("\U0001F4CB Copy Preset Path", GUILayout.Width(150), GUILayout.Height(25)))
                    {
                        string path = AssetDatabase.GetAssetPath(preset);
                        EditorGUIUtility.systemCopyBuffer = path;
                        Debug.Log($"Copied preset path: {path}");
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This preset is empty. Use 'Create Preset' from a Glass material to save settings.", MessageType.Info);
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(preset);
            }
        }
    }
    #endif
}
