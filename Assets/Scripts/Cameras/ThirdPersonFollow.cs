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
    //private CameraShakeController camShakeController;
    public GameObject followTarget;

    /* TARGET FOLLOW */
    [Header("Target following")]
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

    /* OBSTACLE AVOIDANCE */
    [Header("Obstacle avoidance")]
    [Tooltip("Distance at which camera begins to move away from geometry (interpolated)")]
    public float softAvoidDistance = 0f;

    [Tooltip("Soft obstacle avoidance lerp speed multiplier")]
    public float softAvoidLerpSpeed = 1f;
    
    [Tooltip("Minimum camera-geometry distance to maintain")]
    public float hardAvoidDistance = 0f;

    [Tooltip("Number of rays to cast around camera to detect geometry")]
    public int avoidRays = 4;

    /* OCCLUSION AVOIDANCE */
    [Header("Occlusion avoidance")]
    public bool lerpOcclusionAvoidance = true;
    public float occlusionAvoidLerpSpeed = 1f;
    public float occlusionMaxAvoidSearchDistance = 1f;
    public int occlusionNumAvoidSearchSamples = 4;
    private float occlusionAvoidSearchSampleDist; //distance of one occlusion-avoidance search point from the last point/from the camera (for the first sample) 

    void Start()
    {
        cam = GetComponent<Camera>();
        //camShakeController = GetComponent<CameraShakeController>();

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
    }

    private void OnValidate()
    {
        //clamp desired target offset to max target offset
        if (desiredOffsetFromTarget.x > maxOffsetFromTarget.x) desiredOffsetFromTarget.x = maxOffsetFromTarget.x;
        if (desiredOffsetFromTarget.y > maxOffsetFromTarget.y) desiredOffsetFromTarget.y = maxOffsetFromTarget.x;
        if (desiredOffsetFromTarget.z > maxOffsetFromTarget.z) desiredOffsetFromTarget.z = maxOffsetFromTarget.x;


        occlusionAvoidSearchSampleDist = occlusionMaxAvoidSearchDistance / occlusionNumAvoidSearchSamples;
    }

    private void OffsetFromTarget(ref Vector3 targetPos)
    {

    }

    private void ShakeCamera(ref Vector3 targetPos)
    {
        if(camShakeController != null)
        {

        }
    }

    private void AvoidObstacles(ref Vector3 targetPos)
    {

    }

    private void AvoidOcclusion(ref Vector3 targetPos)
    {
        if(Physics.Linecast(transform.position, followTarget.transform.position)) //if camera occluded by geometry
        {
            //search left/right/up/down (local to camera) for non-occluded positions; return first non-occluded position
            //MAYBE hold all valid positions and pick shortest one, otherwise it will just pick the first one (i.e. will favour one direction if multiple valid)
            //List<Vector3> validAvoidPositions = new List<Vector3>(); 
            Vector3 avoidPos;
            if (OcclusionAvoidRaycasts(transform.position, -transform.right, out avoidPos)) targetPos = avoidPos;
            else if (OcclusionAvoidRaycasts(transform.position, transform.right, out avoidPos)) targetPos = avoidPos;
            else if (OcclusionAvoidRaycasts(transform.position, transform.up, out avoidPos)) targetPos = avoidPos;
            else if (OcclusionAvoidRaycasts(transform.position, -transform.up, out avoidPos)) targetPos = avoidPos;
            else //no valid position found; jump to in front of occluding geometry
            {
                //cast rays forward until not inside geometry?
                throw new NotImplementedException();
            }
        }
    }

    private bool OcclusionAvoidRaycasts(Vector3 start, Vector3 dir, out Vector3 firstValidPos)
    {
        Vector3 inc = dir * occlusionAvoidSearchSampleDist;
        Vector3 pos = start + inc;
        for(int i = 0; i < occlusionNumAvoidSearchSamples; i++)
        {
            //cast a ray from current sample position to follow target; if it hits something, it's occluded, so move to next sample position
            //maybe use SphereCast here to ensure a certain amount of unoccluded space? If using, add a public radius variable
            if(Physics.Raycast(pos, followTarget.transform.position - pos))
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
}
