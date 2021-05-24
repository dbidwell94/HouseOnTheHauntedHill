using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CMCameraOverride : MonoBehaviour, AxisState.IInputAxisProvider
{
    public float GetAxisValue(int axis)
    {
        switch (axis)
        {
            case 0:
                if (!Input.GetMouseButton(1))
                {
                    return 0;
                }
                return -Input.GetAxis("Mouse X");
            case 1:
                return -Input.GetAxis("Mouse ScrollWheel");
            default:
                return 0;
        }

    }
}