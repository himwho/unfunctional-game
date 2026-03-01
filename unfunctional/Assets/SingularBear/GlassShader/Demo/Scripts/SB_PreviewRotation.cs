using UnityEngine;

namespace SingularBear
{
    [AddComponentMenu("SingularBear/SB Preview Rotation")]
    public class SB_PreviewRotation : MonoBehaviour
    {
        [Header("Rotation Settings")]
        
        [Tooltip("Rotation speed in degrees per second.")]
        [Range(-360f, 360f)] 
        [SerializeField] private float rotationSpeed = 45f;

        [Tooltip("Axis around which the object rotates.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("Determines whether to rotate relative to the object itself or the world.")]
        [SerializeField] private Space coordinateSpace = Space.Self;

        private void Update()
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, coordinateSpace);
        }
    }
}