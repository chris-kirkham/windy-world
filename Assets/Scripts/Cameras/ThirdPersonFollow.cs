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

    /* CAMERA "MODULES" */
    private CameraFollow follow;
    private CameraShake shake;
    private CameraOcclusion occlusion;
    private CameraCollision collision;

    /* CAMERA STATE */
    private enum State
    {
        FollowingTarget,
        TargetMovingTowardsCamera
    }
    private State state;

    private const float STATE_MOVINGTOWARDSCAMERA_DOT_MAX = -0.75f; 

    /* MISC/CAMERA ATTRIBUTES/CONVENIENCE VARIABLES*/
    private Vector3 currentCamVelocity = Vector3.zero; //this just keeps track of the camera's velocity, changing it doesn't change the camera's velocity
    private Vector3 smoothdampVelocity = Vector3.zero;
    private Vector3 lastCamPosition;
    private Vector3[] nearClipPaneCornersLocal;
    private Vector3 camPos;
    private Vector3 followTargetPos;
    private float desiredOffsetDistance; //distance from target at desired offset

    /* TARGET FOLLOW - POSITION */
    [Header("Target position following")]
    [Tooltip("The desired camera offset from the follow target, in local space by default." +
        "If using interpolation, the camera may deviate from this offset depending on target speed and lerp speed")]
    public Vector3 desiredOffset = Vector3.zero;

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

    /*
    [Tooltip("Screen deadzone for position following. If the follow target is less than the deadzone value from the edge of the camera's view," +
        " the camera will remain stationary (0 is centre of screen, +-0.5 are the edges")]
    [Range(0, 0.5f)] public float followDeadzoneX = 0f;
    [Range(0, 0.5f)] public float followDeadzonePlusY = 0f;
    [Range(0, 0.5f)] public float followDeadzoneNegativeY = 0f;
    */

    /* TARGET FOLLOW - MOUSE ORBIT */
    [Header("Target orbit")]
    public bool orbit = true;

    public float orbitSpeed = 10f;
    
    [Tooltip("Min and max orbit y angles")]
    public float minOrbitY, maxOrbitY;

    [Tooltip("When the mouse is not moving (i.e. the player is not actively controlling the camera orbit)," +
        " the camera will linger on the current orbit angle for orbitIdleStayTime seconds before returning to the default offset.")]
    public float orbitIdleStayTime = 1f;
    private float orbitStayCounter = 0f;

    //tracks current mouse x and y angles
    private float mouseX = 0f;
    private float mouseY = 0f;

    /* TARGET FOLLOW - ROTATION */
    [Header("Camera rotation")]
    public LookAtMode lookAtMode = LookAtMode.LookAtTarget;
    public enum LookAtMode
    {
        FaceCameraHeading,
        LookAtTarget,
        FaceTargetHeading
    }

    public Vector3 lookOffset = Vector3.zero;

    public bool worldSpaceLookOffset = false;

    public bool lookAtLerp = true;

    public float lookAtLerpSpeed = 1f;

    [Tooltip("Screen deadzone for rotation following. If the follow target is less than the deadzone value from the edge of the camera's view," +
    " the camera will remain stationary (0 is centre of screen, +-0.5 are the edges")]
    [Range(0, 0.5f)] public float rotationDeadzoneX = 0f;
    [Range(0, 0.5f)] public float rotationDeadzonePositiveY = 0f;
    [Range(0, 0.5f)] public float rotationDeadzoneNegativeY = 0f;

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
    public bool useCamWhiskers = true;

    [Tooltip("Number of rays to cast behind follow target to detect geometry")]
    public int numWhiskers = 4;

    public float whiskerLength = 1f;

    [Range(0, 360)] public float whiskerSectorAngle = 180;
    private float halfWhiskerSectorAngle;
    private float angleInc;

    [Tooltip("Pre-emptive occlusion avoidance lerp speed")]
    public float preemptiveLerpSpeed = 1f;

    public enum OcclusionAvoidMode
    {
        MoveTowardsTarget,
        FindAvoidVector
    }
    public OcclusionAvoidMode occlusionAvoidMode = OcclusionAvoidMode.MoveTowardsTarget;

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


    void Awake()
    {
        /* MAIN COMPONENTS */
        cam = GetComponent<Camera>();
        camShakeController = GetComponent<CameraShakeController>();

        /* CAMERA "MODULES" */
        follow = GetComponent<CameraFollow>();
        shake = GetComponent<CameraShake>();
        occlusion = GetComponent<CameraOcclusion>();
        collision = GetComponent<CameraCollision>();

        /* MISC/CAMERA ATTRIBUTES */
        state = State.FollowingTarget;
        lastCamPosition = transform.position;
        nearClipPaneCornersLocal = new Vector3[4];

        /* CONVENIENCE VARIABLES */
        camPos = cam.transform.position;
        followTargetPos = followTarget.transform.position;
        desiredOffsetDistance = Vector3.Distance(followTargetPos, followTargetPos + desiredOffset);

        /* TARGET FOLLOW - POSITION */
        sqrMinDistanceFromTarget = minDistanceFromTarget * minDistanceFromTarget;
        sqrMaxDistanceFromTarget = maxDistanceFromTarget * maxDistanceFromTarget;

        /* OBSTACLE AVOIDANCE */

        /* OCCLUSION AVOIDANCE */
        halfWhiskerSectorAngle = whiskerSectorAngle / 2;
        angleInc = (halfWhiskerSectorAngle / numWhiskers) * 2;

        occlusionAvoidSearchSampleDist = occlusionMaxAvoidSearchDistance / occlusionNumAvoidSearchSamples;
    }

    private void OnValidate()
    {
        /* TARGET FOLLOW - POSITION */
        if (maxDistanceFromTarget < minDistanceFromTarget) minDistanceFromTarget = maxDistanceFromTarget;
        if (minDistanceFromTarget > maxDistanceFromTarget) maxDistanceFromTarget = minDistanceFromTarget;

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

    void FixedUpdate()
    {
        UpdateState();

        //update convenience variables
        camPos = cam.transform.position;
        followTargetPos = followTarget.transform.position;
        desiredOffsetDistance = Vector3.Distance(followTargetPos, followTargetPos + desiredOffset);
        nearClipPaneCornersLocal = cam.GetNearClipPaneCornersLocal();

        //update camera rotation - do this first as a different near clip pane position will affect some of the cam collision/occlusion calculations
        cam.transform.rotation = GetLookAtTargetRotation();

        //each of these updates the new camera position given as a reference
        Vector3 newPos = camPos;
        OffsetFromTarget(ref newPos);
        if(orbit) Orbit(ref newPos);
        ShakeCamera(ref newPos);

        AvoidOcclusion(ref newPos);
        //AvoidCollision(ref newPos);

        //update camera position, as well as its last position/velocity trackers
        lastCamPosition = camPos;
        cam.transform.position = newPos;
        currentCamVelocity = (cam.transform.position - lastCamPosition) * Time.deltaTime;
    }

    //updates the camera's state enum based on certain camera/follow target conditions
    private void UpdateState()
    {
        if(Vector3.Dot(transform.forward, followTarget.transform.forward) > STATE_MOVINGTOWARDSCAMERA_DOT_MAX)
        {
            state = State.FollowingTarget;
        }
        else
        {
            state = State.TargetMovingTowardsCamera;
        }
    }

    private void OffsetFromTarget(ref Vector3 newPos)
    {
        Vector3 desiredPos;

        //Get desired camera position based on state
        switch(state)
        {
            case State.TargetMovingTowardsCamera:
                Vector3 frontOffset = new Vector3(desiredOffset.x, desiredOffset.y, Mathf.Abs(desiredOffset.z));
                desiredPos = followTargetPos + (worldSpaceOffset ? frontOffset : followTarget.transform.TransformDirection(frontOffset));
                break;
            case State.FollowingTarget:
            default:
                if(worldSpaceOffset)
                {
                    desiredPos = AvoidCollision(followTargetPos + desiredOffset);
                }
                else
                {
                    desiredPos = AvoidCollision(followTargetPos + followTarget.transform.TransformDirection(desiredOffset));
                }
                
                break;
        }

        //Add cam whisker offset, if using
        if(useCamWhiskers) desiredPos += CamWhiskersFromTarget();

        //do not move camera if target is within desired distance and within screen deadzones
        /*
        float camTargetDistance = Vector3.Distance(camPos, followTargetPos);
        if (camTargetDistance > minDistanceFromTarget && camTargetDistance < desiredOffsetDistance
            && cam.IsWithinDeadzone(followTargetPos, followDeadzoneX, followDeadzoneX, followDeadzonePlusY, followDeadzoneNegativeY))
        {
            Debug.Log("target within desired distance and screen deadzones!");
            desiredPos = camPos;
        }
        */

        //Interpolate between current camera position and desired position to find new position; clamp if outside min/max distances
        if (camPos != desiredPos)
        {
            //newPos = lerpOffset ? Vector3.SmoothDamp(camPos, desiredPos, ref smoothdampVelocity, 1f / offsetLerpSpeed) : desiredPos;
            float smoothTime = 1 / offsetLerpSpeed;
            newPos = lerpOffset ? Lerps.SmoothDampSeparateAxisSeparateY(camPos, desiredPos, ref smoothdampVelocity, smoothTime, smoothTime * 5, smoothTime, smoothTime, Time.deltaTime) : desiredPos;

            //Clamp newPos to min and max distances.
            //This causes camera to orbit around target at min distance, which is cool
            float newPosTargetSqrDist = (followTargetPos - newPos).sqrMagnitude;
            Vector3 targetToNewPosUnit = (newPos - followTargetPos).normalized;
            if (newPosTargetSqrDist < sqrMinDistanceFromTarget)
            {
                newPos = followTargetPos + (targetToNewPosUnit * minDistanceFromTarget);
            }
            else if (newPosTargetSqrDist > sqrMaxDistanceFromTarget)
            {
                newPos = followTargetPos + (targetToNewPosUnit * maxDistanceFromTarget);
            }
        }
    }

    private void Orbit(ref Vector3 newPos)
    {
        mouseX += Input.GetAxis("Mouse X") * orbitSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * orbitSpeed;
        mouseX = ClampMouseAngle(mouseX);
        mouseY = Mathf.Clamp(ClampMouseAngle(mouseY), minOrbitY, maxOrbitY);

        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0f);
        newPos = Vector3.SmoothDamp(camPos, followTargetPos + (rotation * desiredOffset), ref smoothdampVelocity, 1 / orbitSpeed);
    }

    private float ClampMouseAngle(float mouseAngle)
    {
        if (mouseAngle < -360) return mouseAngle + 360;
        if (mouseAngle >= 360) return mouseAngle - 360;
        return mouseAngle;
    }

    private void ShakeCamera(ref Vector3 newPos)
    {
    }

    //Checks collisions in between current pos and desired pos; set desired pos to between collision point and target position if hit
    private Vector3 AvoidCollision(Vector3 desiredPos)
    {
        Debug.DrawLine(camPos, desiredPos, Color.green, 0.1f);
        if (Physics.Linecast(camPos, desiredPos, out RaycastHit hit, LayerMask.GetMask("LevelGeometrySolid")))
        {
            desiredPos = Vector3.Lerp(hit.point, followTargetPos + (Vector3.up * desiredPos.y), 0.5f);
            Debug.DrawLine(camPos, desiredPos, Color.red, 0.1f);
        }

        return desiredPos;
    }

    /*
    private void AvoidCollision(ref Vector3 newPos)
    {
        LayerMask solid = LayerMask.GetMask("LevelGeometrySolid");
        RaycastHit hit;
        foreach(Vector3 clipPaneCorner in nearClipPaneCornersLocal)
        {
            Vector3 clipPaneCornerWorld = cam.transform.TransformPoint(clipPaneCorner);
            Vector3 newPosOffsetByClipCorner = newPos + cam.transform.TransformDirection(clipPaneCorner);
            Debug.DrawLine(clipPaneCornerWorld, newPosOffsetByClipCorner, Color.red);
            if (Physics.Linecast(clipPaneCornerWorld, newPosOffsetByClipCorner, out hit, solid))
            {
                newPos = camPos;
                break;
            }
        }

        /*
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
                float overlapDist = ((ray.direction * minDistanceFromGeometry * 1.01f) - (hit.point - newPosNearClipCorner)).magnitude / 4; 
                newPos += hit.normal * overlapDist;
            }
        }
    }
    */

    private Vector3 CamWhiskersFromTarget()
    {
        Vector3 avoidDir = Vector3.zero;

        for (float i = -halfWhiskerSectorAngle; i <= halfWhiskerSectorAngle; i += angleInc)
        {
            Vector3 dir = Quaternion.AngleAxis(i, Vector3.up) * -followTarget.transform.forward;
            Debug.DrawRay(followTargetPos, dir * whiskerLength, Color.HSVToRGB(Mathf.Abs(i) / whiskerSectorAngle, 0.2f, 1f)); //visualise raycasts
            if (Physics.Raycast(followTargetPos, dir, whiskerLength)) avoidDir -= new Vector3(dir.x, 0f, dir.z);
        }

        //zero local z (is there a better way to do this?)
        avoidDir = followTarget.transform.InverseTransformDirection(avoidDir);
        Debug.DrawRay(followTargetPos + (Vector3.up * 2), avoidDir, Color.blue);
        avoidDir.z = 0;
        avoidDir = followTarget.transform.TransformDirection(avoidDir);

        Debug.DrawRay(followTargetPos + Vector3.up, avoidDir, Color.yellow);
        return avoidDir;
    }

    private void AvoidOcclusion(ref Vector3 newPos)
    {
        //FORWARD OCCLUSION AVOIDANCE - cast rays from follow target to (padded) near clip pane corners; move camera towards player if any hit
        LayerMask solid = LayerMask.GetMask("LevelGeometrySolid");
        Vector3 targetPos = followTargetPos + (Vector3.up * desiredOffset.y);
        float cornerPadding = 1.5f; //amount to extend the near clip pane corners by
        RaycastHit hit;
        foreach (Vector3 clipPaneCorner in nearClipPaneCornersLocal)
        {
            if (Physics.Linecast(targetPos, transform.TransformPoint(clipPaneCorner * cornerPadding), out hit, solid))
            {
                Debug.DrawLine(targetPos, transform.TransformPoint(clipPaneCorner * cornerPadding), Color.cyan, 0.1f);
                float smoothDampTime = Mathf.Min(1 / occlusionAvoidLerpSpeed, Vector3.Distance(hit.point, camPos));
                newPos = Vector3.SmoothDamp(camPos, targetPos, ref smoothdampVelocity, smoothDampTime);
            }
        }

        /*
        float maxDist = (followTargetPos - transform.position).sqrMagnitude;
        LayerMask occluders = LayerMask.GetMask("LevelGeometrySolid");
        RaycastHit[] hits;
        if(cam.RaycastsFromNearClipPane(followTargetPos, out hits, maxDist, occluders)) //if camera occluded by geometry
        {
            switch(occlusionAvoidMode)
            {
                case OcclusionAvoidMode.FindAvoidVector:
                    AvoidOcclusion_UnoccludedVectorSearch(ref newPos, occluders);
                    break;
                case OcclusionAvoidMode.MoveTowardsTarget:
                default:
                    AvoidOcclusion_MoveTowardsTarget(ref newPos, followTargetPos);
                    break; 
            }
        }
        */
    }

    private void AvoidOcclusion_MoveTowardsTarget(ref Vector3 newPos, Vector3 targetPos)
    {
        newPos = Vector3.SmoothDamp(newPos, targetPos, ref smoothdampVelocity, 1 / occlusionAvoidLerpSpeed);
    }

    private void UnoccludedPositionSearch(ref Vector3 newPos, LayerMask occluders)
    {
        /* This searches out in rays at 90 degree increments from the camera's current velocity (if non-zero), 
         * the idea being that it will pick the avoid vector closest to direction the camera is already moving in and so
         * minimally disturb the player.
         * If it doesn't find a non-occluded position within the given parameters, it just jumps to the other side of the occluding object.
         */

        //search for non-occluded positions in cardinal directions relative to camera's velocity; return first non-occluded position
        Vector3 searchDir = currentCamVelocity == Vector3.zero ? -transform.right : currentCamVelocity.normalized;
        Vector3 searchDir90 = Vector3.Cross(searchDir, followTargetPos - transform.position); //search direction rotated 90 degrees
        Vector3 avoidPos;
        if (UnoccludedPositionSearchRaycasts(transform.position, searchDir, out avoidPos, occluders)
            || UnoccludedPositionSearchRaycasts(transform.position, searchDir90, out avoidPos, occluders)
            || UnoccludedPositionSearchRaycasts(transform.position, -searchDir, out avoidPos, occluders)
            || UnoccludedPositionSearchRaycasts(transform.position, -searchDir90, out avoidPos, occluders))
        {
            newPos = lerpOcclusionAvoidance ? Vector3.SmoothDamp(transform.position, avoidPos, ref smoothdampVelocity, 1 / occlusionAvoidLerpSpeed) : avoidPos;
        }
        else //no valid position found; jump to other side of occluding geometry
        {
            //find other side of occluding geometry by trying to hit it from the other side
            //this ray assumes the follow target is on the other side of the occluding geometry (i.e. not inside it, which it should never be)
            Ray backwards = new Ray(followTargetPos, transform.position);
            RaycastHit farSideHit;
            if (Physics.Raycast(backwards, out farSideHit, (followTargetPos - transform.position).sqrMagnitude, occluders))
            {
                newPos = farSideHit.point - (backwards.direction * minDistanceFromGeometry); //jump to min avoid distance in front of occluding geometry
            }
            else
            {
                Debug.LogError("Other side of occluding geometry not found!");
            }
        }
    }

    private bool UnoccludedPositionSearchRaycasts(Vector3 start, Vector3 dir, out Vector3 firstValidPos, LayerMask occluders)
    {
        Vector3 inc = dir * occlusionAvoidSearchSampleDist;
        Vector3 pos = start + inc;
        for(int i = 0; i < occlusionNumAvoidSearchSamples; i++)
        {
            //cast a ray from current sample position to follow target; if it hits something, it's occluded, so move to next sample position
            //maybe use SphereCast here to ensure a certain amount of unoccluded space? If using, add a public radius variable
            if(Physics.Linecast(pos, followTargetPos, occluders))
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
        Vector3 offset = worldSpaceLookOffset ? lookOffset : followTarget.transform.TransformDirection(lookOffset);
        Quaternion lookAt;
        
        switch(state)
        {
            case State.TargetMovingTowardsCamera:
                lookAt = Quaternion.LookRotation((followTargetPos + offset) - camPos, Vector3.up);
                break;
            case State.FollowingTarget:
            default:
                switch (lookAtMode)
                {
                    case LookAtMode.LookAtTarget:
                        lookAt = Quaternion.LookRotation((followTargetPos + offset) - camPos, Vector3.up);
                        break;
                    case LookAtMode.FaceTargetHeading:
                        lookAt = Quaternion.LookRotation(followTarget.transform.forward + offset);
                        break;
                    case LookAtMode.FaceCameraHeading:
                    default:
                        lookAt = Quaternion.LookRotation(currentCamVelocity, Vector3.up);
                        break;
                }
                
                break;
        }

        //check deadzones and keep current cam rotation if within them
        if(cam.IsWithinDeadzone(followTargetPos, rotationDeadzoneX, rotationDeadzoneX, rotationDeadzonePositiveY, rotationDeadzoneNegativeY))
        {
            return cam.transform.rotation;
        }

        //Vector2 followTargetScreenOffset = cam.GetOffsetFromCentreOfView(followTargetPos);
        //float distanceFromCentre = Vector2.Distance(Vector2.zero, followTargetScreenOffset);
        return lookAtLerp ? Quaternion.Slerp(cam.transform.rotation, lookAt, Time.deltaTime * lookAtLerpSpeed) : lookAt;
    }

    //Returns the input position clamped to the min and max distances from the follow target.
    private Vector3 GetDistanceClampedPos(Vector3 pos)
    {
        float newPosTargetSqrDist = (followTargetPos - pos).sqrMagnitude;
        Vector3 targetToNewPosUnit = (pos - followTargetPos).normalized;
        if (newPosTargetSqrDist < sqrMinDistanceFromTarget)
        {
            return followTargetPos + (targetToNewPosUnit * minDistanceFromTarget);
        }
        else if (newPosTargetSqrDist > sqrMaxDistanceFromTarget)
        {
            return followTargetPos + (targetToNewPosUnit * maxDistanceFromTarget);
        }
        else
        {
            return pos;
        }
    }

}
