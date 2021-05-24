using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraFollow : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MoveCamera();
    }

    void MoveCamera()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 previousPos = this.transform.position;
        Vector3 newForwardVec = Vector3.Lerp(previousPos, previousPos + transform.right * x, Time.deltaTime * 10);
        Vector3 newPos = Vector3.Lerp(newForwardVec, newForwardVec + transform.forward * z, Time.deltaTime * 10);
        Ray ray = new Ray(newPos, -transform.up);

        if (Physics.Raycast(ray, 7))
        {
            transform.position = newPos;
        }

        transform.rotation = Quaternion.LookRotation(this.transform.position - new Vector3(Camera.main.transform.position.x, this.transform.position.y, Camera.main.transform.position.z));
    }
}

