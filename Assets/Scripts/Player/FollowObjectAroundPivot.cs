using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RotateAroundPivot : MonoBehaviour
{
    //public PivotPoint pivot;
    [Header("Required components")]
    public Transform pivot;
    public GameObject followTarget;

    [Header("Camera lerp settings")]
    float lerpSpeedX = 1;
    float lerpSpeedY = 1;
    float lerpSpeedZ = 1;

    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void UpdateCamera()
    {
        float camDist = Vector3.Distance(pivot.position, cam.transform.position);

        Vector3 targetPos = Vector3.zero;
        Vector3 newCamPos = Vector3.zero;
        newCamPos.x = Mathf.Lerp(cam.transform.position.x, targetPos.x, Time.deltaTime * lerpSpeedX);
        newCamPos.y = Mathf.Lerp(cam.transform.position.y, targetPos.y, Time.deltaTime * lerpSpeedY);
        newCamPos.z = Mathf.Lerp(cam.transform.position.z, targetPos.z, Time.deltaTime * lerpSpeedZ);

        cam.transform.position = newCamPos;
        cam.transform.LookAt(pivot);
    }
}
