using UnityEngine;
using UnityEngine.InputSystem;

namespace SingularBear.Glass
{
    public class SB_Glass_Camera : MonoBehaviour
{
    [Header("Target & Distance")]
    public Transform target;
    public float defaultDistance = 10.0f;
    public float minDistance = 2.0f;
    public float maxDistance = 20.0f;

    [Header("Sensitivity")]
    public Vector2 rotationSensitivity = new Vector2(0.5f, 0.5f);
    public float zoomSensitivity = 0.05f;

    [Header("Smoothness")]
    public float rotationSmoothTime = 0.12f;
    public float zoomSmoothTime = 0.15f;

    [Header("Premium Bumper FX")]
    [Range(0f, 1f)] public float overShootResistance = 0.5f;
    public float returnElasticity = 10.0f;

    [Header("Orbit Limits")]
    [Range(0f, 180f)] public float maxSideAngle = 60.0f;
    [Range(0f, 89f)] public float maxUpperAngle = 45.0f;
    [Range(0f, 89f)] public float maxLowerAngle = 20.0f;

    [Header("Gizmos")]
    public Color limitColor = new Color(0, 1, 0, 0.4f);
    public bool showGizmosAlways = true;

    private float _targetYaw;
    private float _targetPitch;
    private float _targetDistance;

    private float _currentYaw;
    private float _currentPitch;
    private float _currentDistance;

    private float _yawVelocity;
    private float _pitchVelocity;
    private float _distVelocity;

    void Start()
    {
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            _targetYaw = _currentYaw = angles.y;
            _targetPitch = _currentPitch = angles.x;
            _targetDistance = _currentDistance = defaultDistance;
            
             Vector3 direction = transform.position - target.position;
            _targetDistance = _currentDistance = direction.magnitude;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        ApplyElasticBounds();
        CalculateSmoothMovement();
        ApplyTransform();
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            float yawInput = mouseDelta.x * rotationSensitivity.x;
            float pitchInput = mouseDelta.y * rotationSensitivity.y;

            _targetYaw += IsOutsideYaw() ? yawInput * overShootResistance : yawInput;
            _targetPitch -= IsOutsidePitch() ? pitchInput * overShootResistance : pitchInput;
        }

        float scroll = Mouse.current.scroll.y.ReadValue();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float zoomInput = scroll * zoomSensitivity;
            _targetDistance -= IsOutsideDistance() ? zoomInput * overShootResistance : zoomInput;
        }
    }

    private void ApplyElasticBounds()
    {
        float dt = Time.deltaTime * returnElasticity;

        if (_targetYaw > maxSideAngle) _targetYaw = Mathf.Lerp(_targetYaw, maxSideAngle, dt);
        else if (_targetYaw < -maxSideAngle) _targetYaw = Mathf.Lerp(_targetYaw, -maxSideAngle, dt);

        if (_targetPitch > maxUpperAngle) _targetPitch = Mathf.Lerp(_targetPitch, maxUpperAngle, dt);
        else if (_targetPitch < -maxLowerAngle) _targetPitch = Mathf.Lerp(_targetPitch, -maxLowerAngle, dt);

        if (_targetDistance > maxDistance) _targetDistance = Mathf.Lerp(_targetDistance, maxDistance, dt);
        else if (_targetDistance < minDistance) _targetDistance = Mathf.Lerp(_targetDistance, minDistance, dt);
    }

    private void CalculateSmoothMovement()
    {
        _currentYaw = Mathf.SmoothDamp(_currentYaw, _targetYaw, ref _yawVelocity, rotationSmoothTime);
        _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, rotationSmoothTime);
        _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _distVelocity, zoomSmoothTime);
    }

    private void ApplyTransform()
    {
        Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);
        Vector3 position = target.position - (rotation * Vector3.forward * _currentDistance);

        transform.rotation = rotation;
        transform.position = position;
    }

    private bool IsOutsideYaw()
    {
        return _targetYaw > maxSideAngle || _targetYaw < -maxSideAngle;
    }

    private bool IsOutsidePitch()
    {
        return _targetPitch > maxUpperAngle || _targetPitch < -maxLowerAngle;
    }

    private bool IsOutsideDistance()
    {
        return _targetDistance > maxDistance || _targetDistance < minDistance;
    }

    private void OnDrawGizmos()
    {
        if (showGizmosAlways) DrawOrbitGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmosAlways) DrawOrbitGizmos();
    }

    private void DrawOrbitGizmos()
    {
        if (target == null) return;

        Gizmos.color = limitColor;
        Vector3 center = target.position;

        DrawArc(center, -maxSideAngle, maxSideAngle, 0, maxDistance);
        DrawVerticalArc(center, -maxLowerAngle, maxUpperAngle, 0, maxDistance);

        if (maxDistance != minDistance)
        {
            Gizmos.color = new Color(limitColor.r, limitColor.g, limitColor.b, 0.1f);
            DrawArc(center, -maxSideAngle, maxSideAngle, 0, minDistance);
        }

        Vector3 p1 = GetOrbitPosition(-maxSideAngle, -maxLowerAngle, maxDistance);
        Vector3 p2 = GetOrbitPosition(maxSideAngle, -maxLowerAngle, maxDistance);
        Vector3 p3 = GetOrbitPosition(maxSideAngle, maxUpperAngle, maxDistance);
        Vector3 p4 = GetOrbitPosition(-maxSideAngle, maxUpperAngle, maxDistance);

        Gizmos.color = limitColor;
        Gizmos.DrawLine(center, p1);
        Gizmos.DrawLine(center, p2);
        Gizmos.DrawLine(center, p3);
        Gizmos.DrawLine(center, p4);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);

        Gizmos.color = new Color(1, 0.5f, 0, 0.8f);
        Gizmos.DrawLine(center, transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    private void DrawArc(Vector3 center, float minAngle, float maxAngle, float pitch, float dist)
    {
        int segments = 20;
        float step = (maxAngle - minAngle) / segments;
        Vector3 prevPos = center + (Quaternion.Euler(pitch, minAngle, 0) * -Vector3.forward * dist);

        for (int i = 1; i <= segments; i++)
        {
            float angle = minAngle + (step * i);
            Vector3 nextPos = center + (Quaternion.Euler(pitch, angle, 0) * -Vector3.forward * dist);
            Gizmos.DrawLine(prevPos, nextPos);
            prevPos = nextPos;
        }
    }

    private void DrawVerticalArc(Vector3 center, float minAngle, float maxAngle, float yaw, float dist)
    {
        int segments = 20;
        float step = (maxAngle - minAngle) / segments;
        Vector3 prevPos = center + (Quaternion.Euler(minAngle, yaw, 0) * -Vector3.forward * dist);

        for (int i = 1; i <= segments; i++)
        {
            float angle = minAngle + (step * i);
            Vector3 nextPos = center + (Quaternion.Euler(angle, yaw, 0) * -Vector3.forward * dist);
            Gizmos.DrawLine(prevPos, nextPos);
            prevPos = nextPos;
        }
    }

    private Vector3 GetOrbitPosition(float yaw, float pitch, float dist)
    {
        return target.position - (Quaternion.Euler(pitch, yaw, 0) * Vector3.forward * dist);
    }
}
    }