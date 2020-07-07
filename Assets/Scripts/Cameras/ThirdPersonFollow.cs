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

    /* MISC/CAMERA ATTRIBUTES */
    private enum State
    {
        FollowingTarget,
        TargetMovingTowardsCamera
    }
    private State state;
    
    private Vector3 currentCamVelocity = Vector3.zero; //this just keeps track of the camera's velocity, changing it doesn't change the camera's velocity
    private Vector3 smoothdampVelocity = Vector3.zero;
    private Vector3 lastCamPosition;
    private Vector3[] nearClipPaneCornersLocal;

    /* CONVENIENCE VARIABLES */
    Vector3 camPos;

    /* TARGET FOLLOW - POSITION */
    [Header("Target position following")]
    [Tooltip("The desired camera offset from the follow target, in local space by default." +
        "If using interpolation, the camera may deviate from this offset depending on target speed and lerp speed")]
    public Vector3 desiredOffset = Vector3.zero;

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

    [Tooltip("Screen deadzone for position following. If the follow target is less than the deadzone value from the edge of the camera's view," +
        " the camera will remain stationary (0 is centre of screen, +-0.5 are the edges")]
    [Range(0, 0.5f)] public float followDeadzoneX = 0f;
    [Range(0, 0.5f)] public float followDeadzonePlusY = 0f;
    [Range(0, 0.5f)] public float followDeadzoneNegativeY = 0f;

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
    public enum LookAtMode
    {
        FaceCameraHeading,
        LookAtTarget,
        FaceTargetHeading
    }
    public LookAtMode lookAtMode = LookAtMode.LookAtTarget;

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

        //update clip pane corner points
        nearClipPaneCornersLocal = ClipPaneUtils.GetNearClipPaneCornersLocal(cam);

        //update camera rotation - do this first as a different near clip pane position will affect some of the other calculations
        cam.transform.rotation = GetLookAtTargetRotation();

        //each of these updates the new camera position given as a reference
        Vector3 newPos = cam.transform.position;
        OffsetFromTarget(ref newPos);
        //if(orbit) Orbit(ref newPos);
        //newPos = follow.UpdatePosition(newPos, follow.transform.position);
        ShakeCamera(ref newPos);

        if (preemptiveOcclusionAvoidance) newPos += CamWhiskersFromTarget();
        //AvoidOcclusion(ref newPos);
        //AvoidCollision(ref newPos);

        //update camera position, as well as its last position/velocity trackers
        lastCamPosition = camPos;
        cam.transform.position = newPos;
        currentCamVelocity = (cam.transform.position - lastCamPosition) * Time.deltaTime;
    }

    //updates the camera's state enum based on certain camera/follow target conditions
    private void UpdateState()
    {
        if(Vector3.Dot(transform.forward, followTarget.transform.forward) > -0.75f)
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
        Vector3 followTargetPos = followTarget.transform.position;
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
                    desiredPos = followTargetPos + desiredOffset;
                }
                else
                {
                    desiredPos = followTargetPos + followTarget.transform.TransformDirection(desiredOffset);
                }

                break;
        }

        //Check collisions in between current pos and desired pos; set desired pos to between collision point and target position if hit
        Debug.DrawLine(cam.transform.position, desiredPos, Color.green, 0.1f);
        if (Physics.Linecast(cam.transform.position, desiredPos, out RaycastHit hit, LayerMask.GetMask("LevelGeometrySolid")))
        {
            desiredPos = Vector3.Lerp(hit.point, followTargetPos + (Vector3.up * desiredPos.y), 0.5f);
            Debug.DrawLine(cam.transform.position, desiredPos, Color.red, 0.1f);
        }

        //Check deadzones and ignore position change in axes that are within deadzone
        Vector2 targetScreenCentreOffset = cam.GetOffsetFromCentreOfScreen(followTargetPos);
        if (Mathf.Abs(targetScreenCentreOffset.x) < followDeadzoneX) desiredPos = new Vector3(camPos.x, desiredPos.y, camPos.z); 
        //if (targetScreenCentreOffset.y > -followDeadzonePlusY || targetScreenCentreOffset.y < followDeadzoneNegativeY) desiredPos.y = camPos.y; //top half of screen is [-0.5, 0]; bottom is [0, 0.5]

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
        Vector3 followTargetPos = followTarget.transform.position;
        mouseX += Input.GetAxis("Mouse X") * orbitSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * orbitSpeed;
        mouseX = ClampMouseAngle(mouseX);
        mouseY = Mathf.Clamp(ClampMouseAngle(mouseY), minOrbitY, maxOrbitY);

        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0f);

        newPos = Vector3.SmoothDamp(camPos, followTargetPos + (rotation * desiredOffset), ref smoothdampVelocity, 1 / orbitSpeed);

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

    private float ClampMouseAngle(float mouseAngle)
    {
        if (mouseAngle < -360) return mouseAngle + 360;
        if (mouseAngle >= 360) return mouseAngle - 360;
        return mouseAngle;
    }

    private void ShakeCamera(ref Vector3 newPos)
    {
        if(camShakeController != null)
        {

        }
    }

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
                //EditorApplication.isPaused = true;
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
        */
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
        //FORWARD OCCLUSION AVOIDANCE - cast rays from follow target to (padded) near clip pane corners; move camera towards player if any hit
        LayerMask solid = LayerMask.GetMask("LevelGeometrySolid");
        Vector3 targetPos = followTarget.transform.position + (Vector3.up * desiredOffset.y);
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
        float maxDist = (followTarget.transform.position - transform.position).sqrMagnitude;
        LayerMask occluders = LayerMask.GetMask("LevelGeometrySolid");
        RaycastHit[] hits;
        if(cam.RaycastsFromNearClipPane(followTarget.transform.position, out hits, maxDist, occluders)) //if camera occluded by geometry
        {
            switch(occlusionAvoidMode)
            {
                case OcclusionAvoidMode.FindAvoidVector:
                    AvoidOcclusion_UnoccludedVectorSearch(ref newPos, occluders);
                    break;
                case OcclusionAvoidMode.MoveTowardsTarget:
                default:
                    AvoidOcclusion_MoveTowardsTarget(ref newPos, followTarget.transform.position);
                    break; 
            }
        }
        */
    }

    private void AvoidOcclusion_MoveTowardsTarget(ref Vector3 newPos, Vector3 targetPos)
    {
        newPos = Vector3.SmoothDamp(newPos, targetPos, ref smoothdampVelocity, 1 / occlusionAvoidLerpSpeed);
    }

    private void AvoidOcclusion_UnoccludedVectorSearch(ref Vector3 newPos, LayerMask occluders)
    {
        /* This searches out in rays at 90 degree increments from the camera's current velocity (if non-zero), 
         * the idea being that it will pick the avoid vector closest to direction the camera is already moving in and so
         * minimally disturb the player.
         * If it doesn't find a non-occluded position within the given parameters, it just jumps to the other side of the occluding object.
         */

        //search for non-occluded positions in cardinal directions relative to camera's velocity; return first non-occluded position
        Vector3 searchDir = currentCamVelocity == Vector3.zero ? -transform.right : currentCamVelocity.normalized;
        Vector3 searchDir90 = Vector3.Cross(searchDir, followTarget.transform.position - transform.position); //search direction rotated 90 degrees
        Vector3 avoidPos;
        if (OcclusionAvoidRaycasts(transform.position, searchDir, out avoidPos, occluders)
            || OcclusionAvoidRaycasts(transform.position, searchDir90, out avoidPos, occluders)
            || OcclusionAvoidRaycasts(transform.position, -searchDir, out avoidPos, occluders)
            || OcclusionAvoidRaycasts(transform.position, -searchDir90, out avoidPos, occluders))
        {
            newPos = lerpOcclusionAvoidance ? Vector3.SmoothDamp(transform.position, avoidPos, ref smoothdampVelocity, 1 / occlusionAvoidLerpSpeed) : avoidPos;
        }
        else //no valid position found; jump to other side of occluding geometry
        {
            //find other side of occluding geometry by trying to hit it from the other side
            //this ray assumes the follow target is on the other side of the occluding geometry (i.e. not inside it, which it should never be)
            Ray backwards = new Ray(followTarget.transform.position, transform.position);
            RaycastHit farSideHit;
            if (Physics.Raycast(backwards, out farSideHit, (followTarget.transform.position - transform.position).sqrMagnitude, occluders))
            {
                newPos = farSideHit.point - (backwards.direction * minDistanceFromGeometry); //jump to min avoid distance in front of occluding geometry
            }
            else
            {
                Debug.LogError("Other side of occluding geometry not found!");
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
        Vector3 offset = worldSpaceLookOffset ? lookOffset : followTarget.transform.TransformDirection(lookOffset);
        Quaternion lookAt;
        
        switch(state)
        {
            case State.TargetMovingTowardsCamera:
                lookAt = Quaternion.LookRotation((followTarget.transform.position + offset) - camPos, Vector3.up);
                break;
            case State.FollowingTarget:
            default:
                switch (lookAtMode)
                {
                    case LookAtMode.LookAtTarget:
                        lookAt = Quaternion.LookRotation((followTarget.transform.position + offset) - camPos, Vector3.up);
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
        
        return lookAtLerp ? Quaternion.Slerp(cam.transform.rotation, lookAt, Time.deltaTime * lookAtLerpSpeed) : lookAt;
    }

}
