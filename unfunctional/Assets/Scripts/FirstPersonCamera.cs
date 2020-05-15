using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
   
	public float speedH = 2.0f;
	public float speedV = 2.0f;

	private float azimuth = 0.0f;
	private float elevation = 0.0f;

    void Update()
    {
    	azimuth += speedH * Input.GetAxis("Mouse X");
    	elevation -= speedV * Input.GetAxis("Mouse Y");

    	transform.eulerAngles = new Vector3(elevation, azimuth, 0.0f);
    }
}
