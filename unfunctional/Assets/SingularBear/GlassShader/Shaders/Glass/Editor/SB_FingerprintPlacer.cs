using UnityEngine;
using UnityEditor;

namespace SingularBear.Glass
{
    public class SB_FingerprintPlacer : EditorWindow
    {
        [MenuItem("Tools/SingularBear/Fingerprint Placer")]
        public static void ShowWindow()
        {
            GetWindow<SB_FingerprintPlacer>("Fingerprint Tool");
        }

        private Material _targetMaterial;
        private int _selectedSlot = 1;
        private bool _isPlacing = false;
        private float _defaultRadius = 0.15f;

        private void OnGUI()
        {
            GUILayout.Label("🖐️ Fingerprint Placer Tool", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _targetMaterial = (Material)EditorGUILayout.ObjectField("Target Material", _targetMaterial, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                _isPlacing = false; // Reset state on material change
            }

            _selectedSlot = EditorGUILayout.IntSlider("Slot (1-4)", _selectedSlot, 1, 4);
            _defaultRadius = EditorGUILayout.FloatField("Default Radius", _defaultRadius);

            if (_targetMaterial == null)
            {
                EditorGUILayout.HelpBox("PLEASE ASSIGN A GLASS MATERIAL FIRST", MessageType.Warning);
                return;
            }

            GUI.backgroundColor = _isPlacing ? Color.green : Color.white;
            if (GUILayout.Button(_isPlacing ? "🛑 STOP (Press ESC)" : "🎯 START PLACEMENT"))
            {
                _isPlacing = !_isPlacing;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            if (_isPlacing)
            {
                EditorGUILayout.HelpBox("🟢 ACTIVE: Click on the glass object in Scene View.\nEnsure the object has a COLLIDER.", MessageType.Info);
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isPlacing || _targetMaterial == null) return;

            // Prevent object selection while placing
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;
            
            // Only process on Repaint and MouseDown events to reduce allocations
            if (e.type != EventType.Repaint && e.type != EventType.MouseDown && e.type != EventType.KeyDown)
            {
                return;
            }
            
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Visualizer - only on Repaint
                if (e.type == EventType.Repaint)
                {
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(hit.point, hit.normal, _defaultRadius);
                    Handles.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
                }

                // Handle Click
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    // Convert to local space so fingerprint follows the object
                    Vector3 localPos = hit.transform.InverseTransformPoint(hit.point);
                    PlaceFingerprint(localPos);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                Debug.LogWarning("[SB Glass] Clicked void. Ensure the object has a Collider.");
            }

            // Cancel
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _isPlacing = false;
                Repaint();
            }
            
            // Only repaint on Layout to reduce frequency
            if (e.type == EventType.Repaint)
            {
                sceneView.Repaint();
            }
        }

        private void PlaceFingerprint(Vector3 localPosition)
        {
            Undo.RecordObject(_targetMaterial, "Place Fingerprint");

            string suffix = _selectedSlot.ToString(); 
            
            // 1. Enable Feature Keyword
            if (_selectedSlot == 1) _targetMaterial.EnableKeyword("_SB_FINGERPRINTS");
            else _targetMaterial.EnableKeyword("_SB_FINGERPRINTS_SLOT" + suffix);

            // 2. Set Mapping Mode to World (1)
            string mapProp = "_FingerprintMapping" + suffix;
            _targetMaterial.SetFloat(mapProp, 1.0f);

            // 3. Set Position (in LOCAL/Object space - follows object movement)
            string posProp = "_FingerprintWorldPos" + suffix;
            _targetMaterial.SetVector(posProp, localPosition);

            // 4. Ensure Visibility (Radius)
            string radProp = "_FingerprintWorldRadius" + suffix;
            float currentRadius = _targetMaterial.GetFloat(radProp);
            if (currentRadius < 0.01f)
            {
                _targetMaterial.SetFloat(radProp, _defaultRadius);
            }
            
            // 5. Ensure Visibility (Intensity)
            string intProp = "_FingerprintIntensity" + suffix;
            if (_targetMaterial.GetFloat(intProp) <= 0.01f)
                 _targetMaterial.SetFloat(intProp, 1.0f);

            Debug.Log($"[SB Glass] Fingerprint placed on Slot {_selectedSlot} at local position {localPosition}");
        }
    }
}