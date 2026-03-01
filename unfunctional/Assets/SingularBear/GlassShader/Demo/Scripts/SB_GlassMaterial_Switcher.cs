using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SingularBear
{
    /// <summary>
    /// Runtime material switcher with responsive UI.
    /// UI scales proportionally to screen size.
    /// </summary>
    public class SB_GlassMaterial_Switcher : MonoBehaviour
    {
        //=====================================================================
        // CONFIGURATION
        //=====================================================================
        
        [Header("Target Settings")]
        [Tooltip("Main renderer to identify material. All renderers with same material will be affected.")]
        public Renderer targetRenderer;
        
        [Tooltip("Material applied on start and when clicking 'Default'.")]
        public Material startingMaterial;
        
        [Tooltip("List of materials to switch between.")]
        public Material[] materialList;

        [Header("UI Positioning")]
        [Tooltip("Anchor X position (0 = left, 1 = right).")]
        [Range(0f, 1f)] public float anchorX = 0.01f;
        
        [Tooltip("Anchor Y position (0 = top, 1 = bottom).")]
        [Range(0f, 1f)] public float anchorY = 0.02f;
        
        [Tooltip("Key to toggle UI visibility.")]
        public KeyCode toggleKey = KeyCode.Tab;
        
        [Header("Scaling")]
        [Tooltip("Window width as percentage of screen width.")]
        [Range(0.08f, 0.3f)] public float windowWidthRatio = 0.13f;
        
        [Tooltip("Button height as percentage of screen height.")]
        [Range(0.02f, 0.08f)] public float buttonHeightRatio = 0.035f;
        
        [Tooltip("Base font size as percentage of screen height.")]
        [Range(0.008f, 0.025f)] public float fontSizeRatio = 0.014f;

        [Header("Visual Style")]
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        public Color buttonColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color activeColor = new Color(0.4f, 0.7f, 0.9f, 1f);
        public Color textColor = Color.white;

        //=====================================================================
        // PRIVATE STATE
        //=====================================================================
        
        private bool _isVisible = true;
        private Rect _windowRect;
        private int _currentIndex = -1;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        
        // Computed dimensions (recalculated on resolution change)
        private float _windowWidth;
        private float _buttonHeight;
        private float _padding;
        private int _fontSize;
        private int _fontSizeSmall;
        
        // Cached textures
        private Texture2D _bgTexture;
        private Texture2D _whiteTexture;
        
        // Cached styles
        private GUIStyle _windowStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _titleStyle;
        private bool _stylesInitialized;
        
        // Renderer cache
        private readonly List<Renderer> _matchingRenderers = new List<Renderer>();

        //=====================================================================
        // UNITY LIFECYCLE
        //=====================================================================
        
        private void Start()
        {
            CreateTextures();
            CacheMatchingRenderers();
            RecalculateDimensions();
            
            if (startingMaterial != null)
            {
                ApplyMaterial(startingMaterial);
            }
        }

        private void OnDestroy()
        {
            DestroyTextures();
        }

        private void Update()
        {
            HandleInput();
            
            // Check resolution change
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                RecalculateDimensions();
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;
            
            if (!_stylesInitialized || _windowStyle == null)
            {
                InitStyles();
            }

            // Store GUI state
            Color originalBg = GUI.backgroundColor;
            Color originalContent = GUI.contentColor;

            _windowRect = GUI.Window(99, _windowRect, DrawWindowContent, "", _windowStyle);
            ClampWindowToScreen();

            // Restore GUI state
            GUI.backgroundColor = originalBg;
            GUI.contentColor = originalContent;
        }

        //=====================================================================
        // INITIALIZATION
        //=====================================================================
        
        private void CreateTextures()
        {
            _bgTexture = CreateSolidTexture(backgroundColor);
            _whiteTexture = CreateSolidTexture(Color.white);
        }

        private void DestroyTextures()
        {
            if (_bgTexture != null) Destroy(_bgTexture);
            if (_whiteTexture != null) Destroy(_whiteTexture);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private void CacheMatchingRenderers()
        {
            _matchingRenderers.Clear();
            
            if (targetRenderer == null || targetRenderer.sharedMaterial == null) return;

            string targetName = targetRenderer.sharedMaterial.name;
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer r = allRenderers[i];
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.name == targetName)
                {
                    _matchingRenderers.Add(r);
                }
            }
        }

        private void RecalculateDimensions()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            
            // Calculate proportional dimensions
            _windowWidth = Screen.width * windowWidthRatio;
            _buttonHeight = Screen.height * buttonHeightRatio;
            _padding = _buttonHeight * 0.2f;
            _fontSize = Mathf.RoundToInt(Screen.height * fontSizeRatio);
            _fontSizeSmall = Mathf.RoundToInt(_fontSize * 0.8f);
            
            // Ensure minimum readability
            _fontSize = Mathf.Max(_fontSize, 10);
            _fontSizeSmall = Mathf.Max(_fontSizeSmall, 8);
            _buttonHeight = Mathf.Max(_buttonHeight, 20f);
            _windowWidth = Mathf.Max(_windowWidth, 150f);
            
            // Update window position
            float posX = Screen.width * anchorX;
            float posY = Screen.height * anchorY;
            _windowRect = new Rect(posX, posY, _windowWidth, 0);
            
            // Force style rebuild
            _stylesInitialized = false;
        }

        private void InitStyles()
        {
            int paddingInt = Mathf.RoundToInt(_padding * 2);
            int topPadding = paddingInt; // No extra space needed, we draw title manually
            
            // Window style
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = _fontSize,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(paddingInt, paddingInt, topPadding, paddingInt)
            };
            _windowStyle.normal.background = _bgTexture;
            _windowStyle.onNormal.background = _bgTexture;
            _windowStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            // Title style (drawn manually)
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = _fontSize,
                alignment = TextAnchor.MiddleCenter
            };
            _buttonStyle.normal.background = _whiteTexture;
            _buttonStyle.hover.background = _whiteTexture;
            _buttonStyle.active.background = _whiteTexture;
            _buttonStyle.normal.textColor = textColor;
            _buttonStyle.hover.textColor = textColor;
            _buttonStyle.active.textColor = textColor;

            // Subtitle style
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSizeSmall,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            _subtitleStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            _stylesInitialized = true;
        }

        private void ClampWindowToScreen()
        {
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
        }

        //=====================================================================
        // INPUT
        //=====================================================================
        
        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                _isVisible = !_isVisible;
            }
#else
            if (Input.GetKeyDown(toggleKey))
            {
                _isVisible = !_isVisible;
            }
#endif
        }

        //=====================================================================
        // GUI DRAWING
        //=====================================================================
        
        private void DrawWindowContent(int windowID)
        {
            float contentWidth = _windowRect.width - _padding * 4;
            float currentY = 0f;
            float titleHeight = _fontSize * 1.8f;
            float subtitleHeight = _fontSizeSmall * 2.5f; // More space for subtitle text

            // Title (drawn manually for reliable positioning)
            GUI.Label(
                new Rect(_padding * 2, currentY, contentWidth, titleHeight),
                "Material Switcher",
                _titleStyle
            );
            currentY += titleHeight;

            // Subtitle
            GUI.Label(
                new Rect(_padding * 2, currentY, contentWidth, subtitleHeight),
                "Select a material preset to preview.",
                _subtitleStyle
            );
            currentY += subtitleHeight + _padding;

            // Default button
            GUI.backgroundColor = (_currentIndex == -1) ? activeColor : buttonColor;
            GUI.contentColor = Color.white;

            if (GUI.Button(new Rect(_padding * 2, currentY, contentWidth, _buttonHeight), "Default Material", _buttonStyle))
            {
                ResetToStart();
            }
            currentY += _buttonHeight + _padding;

            // Material buttons
            if (materialList != null)
            {
                for (int i = 0; i < materialList.Length; i++)
                {
                    if (materialList[i] == null) continue;

                    string displayName = GetDisplayName(materialList[i].name);
                    GUI.backgroundColor = (_currentIndex == i) ? activeColor : buttonColor;

                    if (GUI.Button(new Rect(_padding * 2, currentY, contentWidth, _buttonHeight), displayName, _buttonStyle))
                    {
                        SetMaterial(i);
                    }
                    currentY += _buttonHeight + _padding;
                }
            }

            _windowRect.height = currentY + _padding * 2;
            
            // Drag area
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, _buttonHeight * 0.8f));
        }

        private string GetDisplayName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Unknown";
            
            string[] parts = rawName.Split('_');
            if (parts.Length == 0) return rawName;

            string lastPart = parts[parts.Length - 1];
            
            // If last part is numeric and we have more parts, use second-to-last
            if (parts.Length > 1 && int.TryParse(lastPart, out _))
            {
                return parts[parts.Length - 2];
            }
            
            return lastPart;
        }

        //=====================================================================
        // PUBLIC API
        //=====================================================================
        
        public void SetMaterial(int index)
        {
            if (materialList == null) return;
            if (index < 0 || index >= materialList.Length) return;
            if (materialList[index] == null) return;

            ApplyMaterial(materialList[index]);
            _currentIndex = index;
        }

        public void ResetToStart()
        {
            if (startingMaterial != null)
            {
                ApplyMaterial(startingMaterial);
                _currentIndex = -1;
            }
        }

        public void SetVisibility(bool visible)
        {
            _isVisible = visible;
        }

        //=====================================================================
        // INTERNAL
        //=====================================================================
        
        private void ApplyMaterial(Material mat)
        {
            if (mat == null) return;
            
            for (int i = 0; i < _matchingRenderers.Count; i++)
            {
                if (_matchingRenderers[i] != null)
                {
                    _matchingRenderers[i].sharedMaterial = mat;
                }
            }
        }
    }
}