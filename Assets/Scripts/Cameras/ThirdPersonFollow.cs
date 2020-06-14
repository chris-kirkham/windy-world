using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A third-person follow camera with collision and occlusion avoidance
/// </summary>
[RequireComponent(typeof(Camera))]
public class ThirdPersonFollow : MonoBehaviour
{

    /* COMPONENTS */
    private Camera cam;
    private CameraShakeController camShakeController;
    public GameObject followTarget;

    /* MISC/CAMERA ATTRIBUTES */
    private Vector3 currentCamVelocity = Vector3.zero; //this just keeps track of the camera's velocity, changing it doesn't change the camera's velocity
    private Vector3 lastCamPosition;
    private Vector3[] nearClipPaneCornersLocal;


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

    //[Tooltip("If using interpolation, camera will not go beyond these offset values UNLESS it is avoiding geometry/occlusion")]
    //public Vector3 maxOffsetFromTarget = Vector3.zero;
    public float minDistanceFromTarget = 1f;
    public float maxDistanceFromTarget = 10f;
    private float sqrMinDistanceFromTarget, sqrMaxDistanceFromTarget;

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
    [Tooltip("Minimum camera-geometry distance to maintain")]
    public float minDistanceFromGeometry = 1f;

    /* OCCLUSION AVOIDANCE */
    [Header("Occlusion avoidance")]
    [Tooltip("Enables pre-emptive camera movement for occlusion avoidance (casts \"whisker\" rays backwards in a circle from follow target to find" +
        "any objects which may soon occlude target")]
    public bool preemptiveOcclusionAvoidance = true;

    [Tooltip("Number of rays to cast behind follow target to detect geometry")]
    public int numWhiskers = 4;

    public float whiskerLength = 1f;

    [Range(0, 360)] public float whiskerSectorAngle = 180;
    private float halfWhiskerSectorAngle;
    private float angleInc;

    [Tooltip("Pre-emptive occlusion avoidance lerp speed")]
    public float preemptiveLerpSpeed = 1f;

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
        /* COMPONENTS */
        cam = GetComponent<Camera>();
        camShakeController = GetComponent<CameraShakeController>();
        
        /* MISC/CAMERA ATTRIBUTES */
        lastCamPosition = transform.position;
        nearClipPaneCornersLocal = new Vector3[4];

        /* TARGET FOLLOW - POSITION */
        sqrMinDistanceFromTarget = minDistanceFromTarget * minDistanceFromTarget;
        sqrMaxDistanceFromTarget = maxDistanceFromTarget * maxDistanceFromTarget;

        /* OBSTACLE AVOIDANCE */

        /* OCCLUSION AVOIDANCE */
        halfWhiskerSectorAngle = whiskerSectorAngle / 2;
        angleInc = (halfWhiskerSectorAngle / numWhiskers) * 2;

        occlusionAvoidSearchSampleDist = occlusionMaxAvoidSearchDistance / occlusionNumAvoidSearchSamples;
    }

    void LateUpdate()
    {
        //update clip pane corner points
        nearClipPaneCornersLocal = ClipPaneUtils.GetNearClipPaneCornersLocal(cam);

        //update camera rotation - do this first as a different near clip pane position will affect some of the other calculations
        cam.transform.rotation = GetLookAtTargetRotation();

        //each of these updates the new camera position given as a reference
        Vector3 newPos = cam.transform.position;
        OffsetFromTarget(ref newPos);
        ShakeCamera(ref newPos);
        //AvoidOcclusion(ref newPos);
        AvoidObstacles(ref newPos);

        //update camera position, as well as its last position/velocity trackers
        lastCamPosition = cam.transform.position;
        cam.transform.position = newPos;
        currentCamVelocity = cam.transform.position - lastCamPosition;

        
    }

    private void OnValidate()
    {
        /* TARGET FOLLOW - POSITION */
        //clamp desired target offset to max target offset
        /*
        if (desiredOffsetFromTarget.x > maxOffsetFromTarget.x) desiredOffsetFromTarget.x = maxOffsetFromTarget.x;
        if (desiredOffsetFromTarget.y > maxOffsetFromTarget.y) desiredOffsetFromTarget.y = maxOffsetFromTarget.x;
        if (desiredOffsetFromTarget.z > maxOffsetFromTarget.z) desiredOffsetFromTarget.z = maxOffsetFromTarget.x;
        */
        sqrMinDistanceFromTarget = minDistanceFromTarget * minDistanceFromTarget;
        sqrMaxDistanceFromTarget = maxDistanceFromTarget * maxDistanceFromTarget;

        /* CAMERA SHAKE */
        if (useCameraShake && camShakeController == null)
        {
            if (!TryGetComponent(out camShakeController)) Debug.LogError("To use camera shake, you need to attach a CameraShakeController to this object!");
        }

        /* OCCLUSION AVOIDANCE */
        halfWhiskerSectorAngle = whiskerSectorAngle / 2;
        angleInc = (halfWhiskerSectorAngle / numWhiskers) * 2;

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

        /*
        float sqrNewPosDistance = (newPos - followTarget.transform.position).sqrMagnitude;
        if (sqrNewPosDistance < sqrMinDistanceFromTarget)
        {
            newPos *= sqrMinDistanceFromTarget / sqrNewPosDistance;
        }
        else if (sqrNewPosDistance > sqrMaxDistanceFromTarget) 
        {
            newPos *= sqrMaxDistanceFromTarget / sqrNewPosDistance;
        }
        */

    }

    private void ShakeCamera(ref Vector3 newPos)
    {
        if(camShakeController != null)
        {

        }
    }

    private void AvoidObstacles(ref Vector3 newPos)
    {
        //newPos = Vector3.Lerp(newPos, newPos + (CamWhiskersFromTarget() * softAvoidDistance), Time.deltaTime * softAvoidLerpSpeed);
        /*
        LayerMask occluders = LayerMask.GetMask("LevelGeometrySolid");
        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.left, out hit, hardAvoidDistance, occluders)
            || Physics.Raycast(transform.position, Vector3.right, out hit, hardAvoidDistance, occluders)
            || Physics.Raycast(transform.position, Vector3.up, out hit, hardAvoidDistance, occluders)
            || Physics.Raycast(transform.position, Vector3.down, out hit, 0, occluders)
            || Physics.Raycast(transform.position, Vector3.forward, out hit, hardAvoidDistance, occluders)
            || Physics.Raycast(transform.position, Vector3.back, out hit, hardAvoidDistance, occluders))
        {
            Debug.Log("avoiding collision!");
            newPos = new Vector3(hit.point.x + (hit.normal.x * (hardAvoidDistance + 0.01f)), newPos.y, hit.point.z + (hit.normal.z * (hardAvoidDistance + 0.01f)));
        }
        */

        LayerMask solid = LayerMask.GetMask("LevelGeometrySolid");

        foreach (Vector3 p in nearClipPaneCornersLocal)
        {
            Vector3 newPosNearClipCorner = transform.TransformDirection(p) + newPos; //get newPos near clip pane position
            Ray ray = new Ray(newPosNearClipCorner, newPosNearClipCorner - transform.TransformPoint(p)); //camera's move direction (new clip corner - current clip corner)
            Debug.DrawRay(newPosNearClipCorner, newPosNearClipCorner - transform.TransformPoint(p), Color.cyan, 1f);
            
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, minDistanceFromGeometry, solid))
            {
                //move newPos back by the distance the collision ray travels inside the obstacle
                //(max-distance ray - ray from start to obstacle hit point)
                newPos -= (ray.direction * minDistanceFromGeometry * 1.01f) - (hit.point - newPosNearClipCorner);
            }
        }
    }

    private Vector3 CamWhiskersFromTarget()
    {
        Vector3 avoidDir = Vector3.zero;

        for (float i = -halfWhiskerSectorAngle; i <= halfWhiskerSectorAngle; i += angleInc)
        {
            Vector3 dir = Quaternion.AngleAxis(i, Vector3.up) * -followTarget.transform.forward;
            Debug.DrawRay(followTarget.transform.position, dir * whiskerLength, Color.HSVToRGB(Mathf.Abs(i) / whiskerSectorAngle, 1f, 1f)); //visualise raycasts
            if (Physics.Raycast(followTarget.transform.position, dir, whiskerLength)) avoidDir -= new Vector3(dir.x, 0f, dir.z);
        }

        Debug.DrawRay(followTarget.transform.position + Vector3.up, avoidDir, Color.red);
        return avoidDir;
    }

    private void AvoidOcclusion(ref Vector3 newPos)
    {
        float maxDist = (followTarget.transform.position - transform.position).sqrMagnitude;
        LayerMask occluders = LayerMask.GetMask("LevelGeometrySolid");
        RaycastHit[] hits;
        if(ClipPaneUtils.RaycastsFromNearClipPane(cam, followTarget.transform.position, out hits, maxDist, occluders).Contains(true)) //if camera occluded by geometry
        {
            /* This searches out in rays at 90 degree increments from the camera's current velocity (if non-zero), 
             * the idea being that it will pick the avoid vector closest to direction the camera is already moving in and so
             * minimally disturb the player.
             * If it doesn't find a non-occluded position within the given parameters, it just jumps to the other side of the occluding object.
             */
            /*
            //search for non-occluded positions in cardinal directions relative to camera's velocity; return first non-occluded position
            Vector3 searchDir = currentCamVelocity == Vector3.zero ? -transform.right : currentCamVelocity.normalized;
            Vector3 searchDir90 = Vector3.Cross(searchDir, followTarget.transform.position - transform.position); //search direction rotated 90 degrees
            Vector3 avoidPos;
            if (OcclusionAvoidRaycasts(transform.position, searchDir, out avoidPos, occluders)
                || OcclusionAvoidRaycasts(transform.position, searchDir90, out avoidPos, occluders)
                || OcclusionAvoidRaycasts(transform.position, -searchDir, out avoidPos, occluders)
                || OcclusionAvoidRaycasts(transform.position, -searchDir90, out avoidPos, occluders))
            {
                newPos = lerpOcclusionAvoidance ? Vector3.Lerp(transform.position, avoidPos, Time.deltaTime * occlusionAvoidLerpSpeed) : avoidPos;
            }
            else //no valid position found; jump to other side of occluding geometry
            {
                //find other side of occluding geometry by trying to hit it from the other side
                //this ray assumes the follow target is on the other side of the occluding geometry (i.e. not inside it, which it should never be)
                Ray backwards = new Ray(followTarget.transform.position, transform.position); 
                RaycastHit farSideHit;
                if(hits[0].collider.Raycast(backwards, out farSideHit, (followTarget.transform.position - transform.position).sqrMagnitude)) 
                {
                    newPos = farSideHit.point - (backwards.direction * hardAvoidDistance); //jump to min avoid distance in front of occluding geometry
                }
                else
                {
                    Debug.LogError("Other side of occluding geometry not found!");
                }
            }
            */
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
            if(Physics.Linecast(pos, followTarget.transform.position, occluders))
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
            Quaternion lookAt = Quaternion.LookRotation((followTarget.transform.position + (followTarget.transform.forward * lookAtLerpSpeed) + offset) - cam.transform.position, Vector3.up);
            return lookAtLerp ? Quaternion.Slerp(cam.transform.rotation, lookAt, Time.deltaTime * lookAtLerpSpeed) : lookAt;
        }
        else
        {
            return cam.transform.rotation;
        }
    }

    private void OnDrawGizmos()
    {
    }
}
