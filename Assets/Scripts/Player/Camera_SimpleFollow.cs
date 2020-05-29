using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Camera_SimpleFollow : MonoBehaviour
{
    public GameObject followTarget;

    //ScriptableObject for current move force applied to target. Used instead of velocity because velocity can be erratic for 
    //odd-shaped rolling objects e.g. eggs
    //public PlayerMovementInfo targetMoveInfo;

    //Updated only if force magnitude > 0. Used to maintain camera rotation (which is based on move force) when force is 0.
    private Vector3 lastMoveForce;

    //minimum force sqrMagnitude to cause camera rotation - stops camera immediately rotating if e.g. player taps opposite direction to slow down
    private float minForceSqrMagToRotate;
    
    //if true, camera will interpolate between current position and desired offset; if false, camera will match offset exactly
    public bool lerp;

    //if true, lerp speed will be multiplied by sqrMagnitude of target's move force
    //(interpolated between last and current move forces to avoid sudden changes in lerp speed 
    //e.g. when follow target starts moving in opposite direction)
    public bool targetMoveForceAffectsLerpSpeed;
    private float moveForceLerpScale = 0.1f;
    private float lastMoveForceSqrMag; //used both to interpolate move forces for lerp speed and to maintain camera position when move force is zero

    //lerp speed multiplier
    public float lerpSpeed = 2f;

    //starting camera offset from target (in-scene offset between camera and target)
    private Vector3 offset;
    private Camera c;
    private GameObject pivot;

    /*--collision avoidance--*/
    private float camWhiskerLength = 1f;
    private float behindCamCheckDistance = 2f;


    void Start()
    {
        lastMoveForceSqrMag = 0f;
        offset = transform.position - followTarget.transform.position;
        pivot = new GameObject("Camera_SimpleFollow pivot");
        pivot.transform.position = followTarget.transform.position;
        c = GetComponent<Camera>();
        pivot.transform.rotation = c.transform.rotation;
        lastMoveForce = c.transform.forward; //to maintain camera at initial facing until move force applied
        c.transform.SetParent(pivot.transform);
    }

    void LateUpdate()
    {
        //Vector3 forceFlat = new Vector3(targetMoveInfo.moveForce.x, 0f, targetMoveInfo.moveForce.z);
        Vector3 targetVel = followTarget.GetComponent<Rigidbody>().velocity;
        Vector3 forceFlat = new Vector3(targetVel.x, 0f, targetVel.z);
        Vector3 targetPos = new Vector3(followTarget.transform.position.x, followTarget.transform.position.y + offset.y, followTarget.transform.position.z)
            + -CamWhiskersFromTarget() + (c.transform.forward * GeomDistanceBehindCamera());
        Quaternion targetRot = forceFlat.sqrMagnitude > 0f ? Quaternion.LookRotation(forceFlat) : Quaternion.LookRotation(lastMoveForce);

        if(lerp)
        {
            float lerpAmount = Mathf.Max(Time.deltaTime * lerpSpeed, 1f - GeomDistanceBehindCamera());
            //Debug.Log(lerpAmount);
            if(targetMoveForceAffectsLerpSpeed) lerpAmount *= Mathf.Lerp(lastMoveForceSqrMag, forceFlat.sqrMagnitude, Time.deltaTime * lerpSpeed);

            pivot.transform.position = Vector3.Slerp(pivot.transform.position, targetPos, lerpAmount);
            pivot.transform.rotation = Quaternion.Slerp(pivot.transform.rotation, targetRot, lerpAmount);
        }
        else
        {
            pivot.transform.position = targetPos;
            pivot.transform.rotation = targetRot;
        }
        Debug.DrawRay(pivot.transform.position, Vector3.up * 2, Color.cyan);
        Debug.DrawRay(pivot.transform.position, pivot.transform.rotation * Vector3.forward, Color.blue);

        //Debug.DrawRay(followTarget.transform.position, -CamWhiskersFromTarget(), Color.green);
        Debug.DrawRay(c.transform.position, c.transform.forward * GeomDistanceBehindCamera(), Color.yellow);

        //n.b. since this is used both for interpolating forces for targetMoveForceAffectsLerpSpeed and for maintaining camera direction,
        //only updating it when current force > 0 will cause the force lerp speed to be slightly inaccurate. Hopefully it will not be noticeable
        //if (moveForceSqrMag > 0f) lastMoveForceSqrMag = moveForceSqrMag; 
        if (forceFlat.sqrMagnitude > 0f) lastMoveForce = forceFlat;
    }

    //casts "whisker" rays outwards behind the follow target; returns a Vector3 pointing away from any geometry hit by the rays
    Vector3 CamWhiskersFromTarget()
    {
        int numWhiskers = 4;
        float maxAngle = 180f;
        float halfMaxAngle = maxAngle / 2;
        float angleInc = halfMaxAngle / (numWhiskers / 2); //(numWhiskers / 2) = whiskers per side of camera-target line
        float maxDistance = followTarget.GetComponent<Rigidbody>().velocity.sqrMagnitude * 0.1f;
        Vector3 dirs = Vector3.zero;

        for(float i = -halfMaxAngle; i <= halfMaxAngle; i+= angleInc)
        {
            Vector3 dir = Quaternion.AngleAxis(i, Vector3.up) * -pivot.transform.forward;
            Debug.DrawRay(followTarget.transform.position, dir * maxDistance, Color.HSVToRGB(Mathf.Abs(i) / maxAngle, 1f, 1f)); //visualise raycasts
            if (Physics.Raycast(followTarget.transform.position, dir, maxDistance)) dirs += new Vector3(dir.x, 0f, 0f);
        }

        return dirs;
    }

    //if there is geometry closer than behindCamCheckDistance behind the camera, returns its to the camera; returns 1 if no object 

    float GeomDistanceBehindCamera()
    {
        RaycastHit hit;
        if (Physics.Raycast(c.transform.position, -c.transform.right, out hit, behindCamCheckDistance)) return hit.distance;
        if (Physics.Raycast(c.transform.position, -c.transform.forward, out hit, behindCamCheckDistance)) return hit.distance;
        if (Physics.Raycast(c.transform.position, c.transform.right, out hit, behindCamCheckDistance)) return hit.distance;
        return 1f;
    }
    
}
