using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A third-person follow camera with collision and occlusion avoidance
/// </summary>
[RequireComponent(typeof(Camera))]
public class ThirdPersonFollow : MonoBehaviour
{
    private Camera cam;
    private CameraShakeController camShakeController;
    public GameObject followTarget;

    /* TARGET FOLLOW - POSITION */
    [Header("Target position following")]
    [Tooltip("The desired camera offset from the follow target, in local space by default." +
        "If using interpolation, the camera may deviate from this offset depending on target speed and lerp speed")]
    public Vector3 desiredOffsetFromTarget = Vector3.zero;

    [Tooltip("Use offset as world space")]
    public bool worldSpaceOffset = false;
    
    [Tooltip("Interpolate between the camera's current offset and desired offset by deltaTime and lerp speed")]
    public bool lerpOffset = true;

    [Tooltip("Desired offset lerp speed multiplier")]
    public float offsetLerpSpeed = 1f;
    
    [Tooltip("If using interpolation, camera will not go beyond these offset values UNLESS it is avoiding geometry/occlusion")]
    public Vector3 maxOffsetFromTarget = Vector3.zero;

    /* TARGET FOLLOW - ROTATION */
    public bool lookAtTarget = true;

    [Tooltip("The offs")]
    public Vector3 lookOffset = Vector3.zero;

    public bool worldSpaceLookOffset = false;

    public bool lookAtLerp = true;

    public float lookAtLerpSpeed = 1f;

    /* CAMERA SHAKE */
    [Header("Camera shake")]
    public bool useCameraShake = false;

    /* OBSTACLE AVOIDANCE */
    [Header("Obstacle avoidance")]
    [Tooltip("Distance at which camera begins to move away from geometry (interpolated) - if not desired, set to equal hard avoid distance")]
    public float softAvoidDistance = 0f;

    [Tooltip("Soft obstacle avoidance lerp speed multiplier")]
    public float softAvoidLerpSpeed = 1f;
    
    [Tooltip("Minimum camera-geometry distance to maintain - cannot be more than soft avoid distance")]
    public float hardAvoidDistance = 0f;

    [Tooltip("Number of rays to cast around camera to detect geometry")]
    public int avoidRays = 4;

    /* OCCLUSION AVOIDANCE */
    [Header("Occlusion avoidance")]
    [Tooltip("Interpolate occlusion avoidance")]
    public bool lerpOcclusionAvoidance = true;

    [Tooltip("Occlusion avoidance lerp speed multiplier")]
    public float occlusionAvoidLerpSpeed = 1f;

    [Tooltip("Max distance at which to cast rays from the camera in order to find an unoccluded position")]
    public float occlusionMaxAvoidSearchDistance = 1f;

    [Tooltip("Number of points to sample in each direction - each sample increases in distance from camera by (max search distance / num samples)")]
    public int occlusionNumAvoidSearchSamples = 4;

    //distance of one occlusion-avoidance search point from the last point (or from the camera, for the first sample) 
    private float occlusionAvoidSearchSampleDist; 


    void Start()
    {
        cam = GetComponent<Camera>();
        camShakeController = GetComponent<CameraShakeController>();

        occlusionAvoidSearchSampleDist = occlusionMaxAvoidSearchDistance / occlusionNumAvoidSearchSamples;
    }

    void LateUpdate()
    {
        Vector3 newPos = cam.transform.position;

        //each of these updates the new camera position given as a reference
        OffsetFromTarget(ref newPos);
        ShakeCamera(ref newPos);
        AvoidObstacles(ref newPos);
        AvoidOcclusion(ref newPos);
        cam.transform.position = newPos;

        cam.transform.rotation = GetLookAtTargetRotation();
    }

    private void OnValidate()
    {
        /* TARGET FOLLOW */
        //clamp desired target offset to max target offset
        if (desiredOffsetFromTarget.x > maxOffsetFromTarget.x) desiredOffsetFromTarget.x = maxOffsetFromTarget.x;
        if (desiredOffsetFromTarget.y > maxOffsetFromTarget.y) desiredOffsetFromTarget.y = maxOffsetFromTarget.x;
        if (desiredOffsetFromTarget.z > maxOffsetFromTarget.z) desiredOffsetFromTarget.z = maxOffsetFromTarget.x;

        /* CAMERA SHAKE */
        if (useCameraShake && camShakeController == null)
        {
            if (!TryGetComponent(out camShakeController)) Debug.LogError("To use camera shake, you need to attach a CameraShakeController to this object!");
        }

        /* OBSTACLE AVOIDANCE */
        if (hardAvoidDistance > softAvoidDistance) hardAvoidDistance = softAvoidDistance;

        /* OCCLUSION AVOIDANCE */
        occlusionAvoidSearchSampleDist = occlusionMaxAvoidSearchDistance / occlusionNumAvoidSearchSamples;
    }

    private void OffsetFromTarget(ref Vector3 newPos)
    {
        Vector3 camPos = transform.position;
        Vector3 desiredPos = worldSpaceOffset ? followTarget.transform.position + desiredOffsetFromTarget 
            : followTarget.transform.position + followTarget.transform.TransformDirection(desiredOffsetFromTarget);
    
        if(camPos != desiredPos)
        {
            newPos = lerpOffset ? Vector3.Lerp(camPos, desiredPos, Time.deltaTime * offsetLerpSpeed) : desiredPos;
        }
    }

    private void ShakeCamera(ref Vector3 newPos)
    {
        if(camShakeController != null)
        {

        }
    }

    private void AvoidObstacles(ref Vector3 newPos)
    {

    }

    private void AvoidOcclusion(ref Vector3 newPos)
    {
        LayerMask occluders = LayerMask.GetMask("LevelGeometrySolid");
        RaycastHit hit;
        if(Physics.Linecast(transform.position, followTarget.transform.position, out hit, occluders)) //if camera occluded by geometry
        {
            //search left/right/up/down (local to camera) for non-occluded positions; return first non-occluded position
            //MAYBE hold all valid positions and pick shortest one, otherwise it will just pick the first one (i.e. will favour one direction if multiple valid)
            //List<Vector3> validAvoidPositions = new List<Vector3>(); 
            Vector3 avoidPos;
            if (OcclusionAvoidRaycasts(transform.position, -transform.right, out avoidPos, occluders)) newPos = avoidPos;
            else if (OcclusionAvoidRaycasts(transform.position, transform.right, out avoidPos, occluders)) newPos = avoidPos;
            else if (OcclusionAvoidRaycasts(transform.position, transform.up, out avoidPos, occluders)) newPos = avoidPos;
            else if (OcclusionAvoidRaycasts(transform.position, -transform.up, out avoidPos, occluders)) newPos = avoidPos;
            else //no valid position found; jump to other side of occluding geometry
            {
                //find other side of occluding geometry by trying to hit it from the other side
                //this ray assumes the follow target is on the other side of the occluding geometry (i.e. not inside it, which it should never be)
                Ray backwards = new Ray(followTarget.transform.position, transform.position - hit.point);
                RaycastHit farSideHit;
                if(hit.collider.Raycast(backwards, out farSideHit, (followTarget.transform.position - transform.position).sqrMagnitude)) 
                {
                    newPos = farSideHit.point - (backwards.direction * hardAvoidDistance); //jump to min avoid distance in front of occluding geometry
                }
                else
                {
                    Debug.LogError("Other side of occluding geometry not found!");
                }
            }
        }
    }

    private bool OcclusionAvoidRaycasts(Vector3 start, Vector3 dir, out Vector3 firstValidPos, LayerMask occluders)
    {
        Vector3 inc = dir * occlusionAvoidSearchSampleDist;
        Vector3 pos = start + inc;
        for(int i = 0; i < occlusionNumAvoidSearchSamples; i++)
        {
            //cast a ray from current sample position to follow target; if it hits something, it's occluded, so move to next sample position
            //maybe use SphereCast here to ensure a certain amount of unoccluded space? If using, add a public radius variable
            if(Physics.Raycast(pos, followTarget.transform.position - pos, occluders))
            {
                pos += inc;
            }
            else
            {
                firstValidPos = pos;
                return true;
            }
        }

        firstValidPos = Vector3.zero;
        return false;
    }

    private Quaternion GetLookAtTargetRotation()
    {
        if(lookAtTarget)
        {
            Vector3 offset = worldSpaceLookOffset ? lookOffset : followTarget.transform.TransformDirection(lookOffset);
            Quaternion lookAt = Quaternion.LookRotation((followTarget.transform.position + offset) - cam.transform.position, Vector3.up);
            return lookAtLerp ? Quaternion.Slerp(cam.transform.rotation, lookAt, Time.deltaTime * lookAtLerpSpeed) : lookAt;
        }
        else
        {
            return cam.transform.rotation;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, followTarget.transform.position);
    }
}
