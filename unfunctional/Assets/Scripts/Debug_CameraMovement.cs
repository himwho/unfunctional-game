using UnityEngine;

public class Debug_CameraMovement : MonoBehaviour
{
    public int Speed = 50;
    void Update()
    {
        float xAxisValue = Input.GetAxis("Horizontal") * Speed;
        float zAxisValue = Input.GetAxis("Vertical") * Speed;
        float yValue = 0.0f;
 
        if (Input.GetKey(KeyCode.Q))
        {
            yValue = -Speed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            yValue = Speed;
        }
 
        transform.position = new Vector3(transform.position.x + xAxisValue, transform.position.y + yValue, transform.position.z + zAxisValue);

        Camera mycam = GetComponent<Camera>();
        float mouseX = (Input.mousePosition.x / Screen.width ) - 0.5f;
     	float mouseY = (Input.mousePosition.y / Screen.height) - 0.5f;
     	transform.localRotation = Quaternion.Euler (new Vector4 (-1f * (mouseY * 180f), mouseX * 360f, transform.localRotation.z));
    }
}