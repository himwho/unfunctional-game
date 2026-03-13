using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SingularBear
{
    /// <summary>
    /// Runtime preview controller with rotation and responsive UI.
    /// UI scales proportionally to screen size.
    /// </summary>
    public class SB_Preview : MonoBehaviour
    {
        //=====================================================================
        // CONFIGURATION
        //=====================================================================
        
        [Header("Rotation Settings")]
        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float rotationSpeed = 30f;
        
        [Tooltip("Start rotating automatically on play.")]
        [SerializeField] private bool autoStart = true;

        [Header("Light Settings")]
        [Tooltip("Directional light to control. If empty, will try to find one.")]
        [SerializeField] private Light directionalLight;
        
        [Tooltip("Initial light X rotation (pitch).")]
        [SerializeField] private float defaultLightPitch = 50f;
        
        [Tooltip("Initial light Y rotation (yaw).")]
        [SerializeField] private float defaultLightYaw = -30f;

        [Header("UI Positioning")]
        [Tooltip("Horizontal anchor (0 = left, 1 = right).")]
        [Range(0f, 1f)] public float anchorX = 0.99f;
        
        [Tooltip("Vertical anchor (0 = top, 1 = bottom).")]
        [Range(0f, 1f)] public float anchorY = 0.5f;
        
        [Tooltip("Key to toggle UI visibility.")]
        public KeyCode toggleKey = KeyCode.P;
        
        [Header("Scaling")]
        [Tooltip("Window width as percentage of screen width.")]
        [Range(0.08f, 0.3f)] public float windowWidthRatio = 0.12f;
        
        [Tooltip("Button height as percentage of screen height.")]
        [Range(0.02f, 0.08f)] public float buttonHeightRatio = 0.03f;
        
        [Tooltip("Base font size as percentage of screen height.")]
        [Range(0.008f, 0.025f)] public float fontSizeRatio = 0.012f;

        [Header("Visual Style")]
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        public Color buttonColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color activeColor = new Color(0.4f, 0.7f, 0.9f, 1f);
        public Color sliderHandleColor = new Color(0.4f, 0.7f, 0.9f, 1f);
        public Color textColor = Color.white;

        //=====================================================================
        // PRIVATE STATE
        //=====================================================================
        
        private bool _isRotating;
        private bool _isVisible = true;
        private float _currentRotationY;
        private float _lightPitch;
        private float _lightYaw;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        
        // Computed dimensions
        private float _windowWidth;
        private float _buttonHeight;
        private float _padding;
        private int _fontSize;
        private float _sliderHeight;
        private float _thumbSize;
        
        private Rect _windowRect;
        
        // Cached textures
        private Texture2D _bgTexture;
        private Texture2D _whiteTexture;
        private Texture2D _handleTexture;
        private Texture2D _sliderRailTexture;
        private Texture2D _transparentTexture;
        private Color _cachedHandleColor;
        
        // Cached styles
        private GUIStyle _windowStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _sliderBgStyle;
        private GUIStyle _sliderThumbStyle;
        private bool _stylesInitialized;

        //=====================================================================
        // UNITY LIFECYCLE
        //=====================================================================
        
        private void Start()
        {
            _isRotating = autoStart;
            _currentRotationY = transform.eulerAngles.y;
            
            // Find directional light if not assigned
            if (directionalLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        directionalLight = lights[i];
                        break;
                    }
                }
            }
            
            // Initialize light rotation
            if (directionalLight != null)
            {
                _lightPitch = defaultLightPitch;
                _lightYaw = defaultLightYaw;
                ApplyLightRotation();
            }
            
            CreateTextures();
            RecalculateDimensions();
        }

        private void OnDestroy()
        {
            DestroyTextures();
        }

        private void Update()
        {
            HandleRotation();
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
            
            // Check handle color change
            if (_cachedHandleColor != sliderHandleColor)
            {
                UpdateHandleTexture();
            }

            // Update horizontal position (centered)
            UpdateWindowPosition();

            // Store GUI state
            Color originalBg = GUI.backgroundColor;
            Color originalContent = GUI.contentColor;
            Color originalColor = GUI.color;

            _windowRect = GUI.Window(100, _windowRect, DrawWindowContent, "", _windowStyle);
            ClampWindowToScreen();

            // Restore GUI state
            GUI.backgroundColor = originalBg;
            GUI.contentColor = originalContent;
            GUI.color = originalColor;
        }

        //=====================================================================
        // INITIALIZATION
        //=====================================================================
        
        private void CreateTextures()
        {
            _bgTexture = CreateSolidTexture(backgroundColor);
            _whiteTexture = CreateSolidTexture(Color.white);
            _sliderRailTexture = CreateSolidTexture(buttonColor);
            _transparentTexture = CreateSolidTexture(Color.clear);
            
            _handleTexture = CreateSolidTexture(sliderHandleColor);
            _cachedHandleColor = sliderHandleColor;
        }

        private void DestroyTextures()
        {
            if (_bgTexture != null) Destroy(_bgTexture);
            if (_whiteTexture != null) Destroy(_whiteTexture);
            if (_handleTexture != null) Destroy(_handleTexture);
            if (_sliderRailTexture != null) Destroy(_sliderRailTexture);
            if (_transparentTexture != null) Destroy(_transparentTexture);
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private void UpdateHandleTexture()
        {
            if (_handleTexture != null) Destroy(_handleTexture);
            
            _handleTexture = CreateSolidTexture(sliderHandleColor);
            _cachedHandleColor = sliderHandleColor;

            if (_sliderThumbStyle != null)
            {
                _sliderThumbStyle.normal.background = _handleTexture;
                _sliderThumbStyle.hover.background = _handleTexture;
                _sliderThumbStyle.active.background = _handleTexture;
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
            _sliderHeight = _buttonHeight * 0.4f;
            _thumbSize = _buttonHeight * 0.6f;
            
            // Ensure minimum readability
            _fontSize = Mathf.Max(_fontSize, 10);
            _buttonHeight = Mathf.Max(_buttonHeight, 20f);
            _windowWidth = Mathf.Max(_windowWidth, 180f);
            _sliderHeight = Mathf.Max(_sliderHeight, 8f);
            _thumbSize = Mathf.Max(_thumbSize, 14f);
            
            // Force style rebuild
            _stylesInitialized = false;
        }

        private void UpdateWindowPosition()
        {
            // Anchor X: 0 = left edge, 1 = right edge (window aligned to right)
            float posX = Screen.width * anchorX - _windowWidth;
            // Anchor Y: 0 = top, 1 = bottom (centered on anchor point)
            float posY = Screen.height * anchorY - _windowRect.height / 2f;
            
            _windowRect.x = Mathf.Max(0, posX);
            _windowRect.y = posY;
            _windowRect.width = _windowWidth;
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

            // Label style for slider labels
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(_fontSize * 0.85f),
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

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

            // Slider background (rail) - transparent since we draw it manually
            _sliderBgStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                fixedHeight = _thumbSize,
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            _sliderBgStyle.normal.background = _transparentTexture;

            // Slider thumb (handle)
            _sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedHeight = _thumbSize,
                fixedWidth = _thumbSize,
                border = new RectOffset(0, 0, 0, 0)
            };
            _sliderThumbStyle.normal.background = _handleTexture;
            _sliderThumbStyle.hover.background = _handleTexture;
            _sliderThumbStyle.active.background = _handleTexture;

            _stylesInitialized = true;
        }

        private void ClampWindowToScreen()
        {
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
        }

        //=====================================================================
        // INPUT & ROTATION
        //=====================================================================
        
        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
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

        private void HandleRotation()
        {
            if (_isRotating)
            {
                _currentRotationY += rotationSpeed * Time.deltaTime;
                _currentRotationY = Mathf.Repeat(_currentRotationY, 360f);
                transform.rotation = Quaternion.Euler(0f, _currentRotationY, 0f);
            }
            else if (transform.hasChanged)
            {
                _currentRotationY = transform.eulerAngles.y;
                transform.hasChanged = false;
            }
        }

        //=====================================================================
        // GUI DRAWING
        //=====================================================================
        
        private void DrawWindowContent(int windowID)
        {
            float contentWidth = _windowRect.width - _padding * 4;
            float currentY = 0f;
            float titleHeight = _fontSize * 1.8f;
            float buttonSpacing = 4f;
            float labelHeight = _fontSize * 1.5f;

            // Title (drawn manually for reliable positioning)
            GUI.Label(
                new Rect(_padding * 2, currentY, contentWidth, titleHeight),
                "Preview Controls",
                _titleStyle
            );
            currentY += titleHeight + _padding;

            GUI.contentColor = Color.white;

            // Play button
            GUI.backgroundColor = _isRotating ? activeColor : buttonColor;
            if (GUI.Button(new Rect(_padding * 2, currentY, contentWidth / 2f - buttonSpacing, _buttonHeight), "▶", _buttonStyle))
            {
                _isRotating = true;
            }

            // Pause button
            GUI.backgroundColor = !_isRotating ? activeColor : buttonColor;
            if (GUI.Button(new Rect(_padding * 2 + contentWidth / 2f + buttonSpacing, currentY, contentWidth / 2f - buttonSpacing, _buttonHeight), "❚❚", _buttonStyle))
            {
                _isRotating = false;
            }

            currentY += _buttonHeight + _padding * 2;

            // Object Rotation label
            GUI.Label(new Rect(_padding * 2, currentY, contentWidth, labelHeight), "Object Rotation", _labelStyle);
            currentY += labelHeight;

            // Object Rotation slider
            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;

            currentY = DrawSlider(currentY, contentWidth, ref _currentRotationY, 0f, 360f);
            transform.rotation = Quaternion.Euler(0f, _currentRotationY, 0f);

            // Light controls (only if light exists)
            if (directionalLight != null)
            {
                currentY += _padding;
                
                // Light Pitch label
                GUI.Label(new Rect(_padding * 2, currentY, contentWidth, labelHeight), "Light Pitch", _labelStyle);
                currentY += labelHeight;

                // Light Pitch slider
                float oldPitch = _lightPitch;
                currentY = DrawSlider(currentY, contentWidth, ref _lightPitch, 0f, 90f);
                
                currentY += _padding;
                
                // Light Yaw label
                GUI.Label(new Rect(_padding * 2, currentY, contentWidth, labelHeight), "Light Yaw", _labelStyle);
                currentY += labelHeight;

                // Light Yaw slider
                float oldYaw = _lightYaw;
                currentY = DrawSlider(currentY, contentWidth, ref _lightYaw, -180f, 180f);

                // Apply light rotation if changed
                if (!Mathf.Approximately(oldPitch, _lightPitch) || !Mathf.Approximately(oldYaw, _lightYaw))
                {
                    ApplyLightRotation();
                }
            }

            _windowRect.height = currentY + _padding * 2;
            
            // Drag area
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, _buttonHeight * 0.8f));
        }

        private float DrawSlider(float currentY, float contentWidth, ref float value, float min, float max)
        {
            // Draw rail manually (perfectly centered with thumb)
            float railY = currentY + (_thumbSize - _sliderHeight) / 2f;
            Rect railRect = new Rect(_padding * 2, railY, contentWidth, _sliderHeight);
            GUI.DrawTexture(railRect, _sliderRailTexture);
            
            // Draw slider with transparent rail (thumb only)
            Rect sliderRect = new Rect(_padding * 2, currentY, contentWidth, _thumbSize);
            value = GUI.HorizontalSlider(sliderRect, value, min, max, _sliderBgStyle, _sliderThumbStyle);

            return currentY + _thumbSize + _padding;
        }

        private void ApplyLightRotation()
        {
            if (directionalLight != null)
            {
                directionalLight.transform.rotation = Quaternion.Euler(_lightPitch, _lightYaw, 0f);
            }
        }

        //=====================================================================
        // PUBLIC API
        //=====================================================================
        
        public void SetRotating(bool rotating)
        {
            _isRotating = rotating;
        }

        public void SetRotation(float angle)
        {
            _currentRotationY = Mathf.Repeat(angle, 360f);
            transform.rotation = Quaternion.Euler(0f, _currentRotationY, 0f);
        }

        public void SetVisibility(bool visible)
        {
            _isVisible = visible;
        }

        public float GetCurrentRotation()
        {
            return _currentRotationY;
        }

        public bool IsRotating()
        {
            return _isRotating;
        }

        public void SetLightPitch(float pitch)
        {
            _lightPitch = Mathf.Clamp(pitch, 0f, 90f);
            ApplyLightRotation();
        }

        public void SetLightYaw(float yaw)
        {
            _lightYaw = Mathf.Clamp(yaw, -180f, 180f);
            ApplyLightRotation();
        }

        public void ResetLightRotation()
        {
            _lightPitch = defaultLightPitch;
            _lightYaw = defaultLightYaw;
            ApplyLightRotation();
        }
    }
}