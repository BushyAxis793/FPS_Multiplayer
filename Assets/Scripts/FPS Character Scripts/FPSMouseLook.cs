using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMouseLook : MonoBehaviour
{

    public enum RotationAxes { MouseX, MouseY }
    public RotationAxes axes = RotationAxes.MouseY;

    private float currentSensivityX = 1.5f;
    private float currentSensivityY = 1.5f;

    private float sensivityX = 1.5f;
    private float sensivityY = 1.5f;

    private float rotationX, rotationY;

    private float minimumX = -360f;
    private float maximumX = 360f;

    private float minimumY = -360f;
    private float maximumY = 360f;

    private Quaternion originalRotation;

    private float mouseSensivity = 1.7f;

    void Start()
    {
        originalRotation = transform.rotation;
    }


    void LateUpdate()
    {
        HandleRotation();
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f)
        {
            angle += 360f;
        }
        if (angle > 360f)
        {
            angle -= 360f;
        }
        return Mathf.Clamp(angle, min, max);
    }

    void HandleRotation()
    {
        if (currentSensivityX != mouseSensivity || currentSensivityY != mouseSensivity)
        {
            currentSensivityX = currentSensivityY = mouseSensivity;
        }

        sensivityX = currentSensivityX;
        sensivityY = currentSensivityY;

        if (axes == RotationAxes.MouseX)
        {
            rotationX += Input.GetAxis("Mouse X") * sensivityX;

            rotationX = ClampAngle(rotationX, minimumX, maximumX);
            Quaternion xquaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation = originalRotation * xquaternion;
        }
        if (axes == RotationAxes.MouseY)
        {
            rotationY += Input.GetAxis("Mouse Y") * sensivityY;
            rotationY = ClampAngle(rotationY, minimumY, maximumY);

            Quaternion yquaternion = Quaternion.AngleAxis(-rotationY, Vector3.right);
            transform.localRotation = originalRotation * yquaternion;

        }

    }


}



































