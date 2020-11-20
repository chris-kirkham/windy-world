using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonFollowNew : MonoBehaviour
{
    //Components
    private Camera cam;
    public GameObject followTarget;
    public GameObject lookAtTarget;
    
    //Inspector parameters
    [Header("Target following")]
    public Vector3 desiredOffset = Vector3.back;
    public Vector3 minOffset = Vector3.back;
    public Vector3 maxOffset = Vector3.back;
    public float followSpeed = 1f;

    [Header("Occlusion avoidance")]
    public float cameraPullInSpeed = 1f;
    public bool preserveCameraHeight = true;

    [Header("Camera whiskers")]
    public bool useCamWhiskers = true;
    public float whiskerPushStrength = 1f;
    [Min(2)] public int numWhiskers = 4;
    public float whiskerLength = 1f;
    [Range(0, 360)] public float whiskerSectorAngle = 180;

    //[Header("Collision avoidance")]

    [Header("Target orbit")]
    public bool orbit = true;
    public float orbitSpeed = 1f;
    public float minOrbitYAngle, maxOrbitYAngle;

    //tracks current mouse x and y angles
    private float mouseX = 0f;
    private float mouseY = 0f;


    //Const params - stuff that's not exposed to the inspector because it doesn't affect artistic aspects of camera control
    //so much as basic functioning of collision/occluson avoidance etc.
    private const float COLLISION_SPHERECAST_RADIUS = 1f;
    private const float OCCLUSION_CLIP_PANE_PADDING = 0.5f;

    //Layer masks
    private int occluderLayers;
    private int colliderLayers;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        occluderLayers = LayerMask.GetMask("LevelGeometrySolid");
        colliderLayers = LayerMask.GetMask("LevelGeometrySolid");
    }

    //all camera movement is done here
    private void FixedUpdate()
    {
        Vector3 newCamPosition;

        if(orbit)
        {
            newCamPosition = OrbitTarget(cam.transform.position, Time.fixedDeltaTime);
        }
        else
        {
            newCamPosition = GetFollowPosition(Time.fixedDeltaTime);
        }
        
        newCamPosition = GetOcclusionAvoidResult(newCamPosition, Time.fixedDeltaTime);

        //if(useCamWhiskers) newCamPosition = GetCamWhiskerResult(newCamPosition, GetCamWhiskerOffset(), Time.fixedDeltaTime);
        newCamPosition = AvoidCollisions(newCamPosition);

        cam.transform.position = newCamPosition;
        cam.transform.rotation = GetTargetLookAtRotation(lookAtTarget.transform.position);
        //cam.transform.rotation = GetTargetLookAtRotation(GetCamWhiskerResult(lookAtTarget.transform.position, GetCamWhiskerOffset(), Time.fixedDeltaTime));
    }

    //Returns the desired target follow position as specified by the target's position and the target follow offset params.
    //This is the "ideal" position the camera wants to be in, before taking into consideration obstacle/occlusion avoidance etc.
    private Vector3 GetDesiredFollowPosition(Vector3 desiredOffset)
    {
        return followTarget.transform.position + followTarget.transform.TransformDirection(desiredOffset);
    }

    //Returns the camera position after interpolating between the current camera position and the desired follow position, 
    //shortening the desired follow offset if it would be occluded
    private Vector3 GetFollowPosition(float deltaTime)
    {
        Vector3 newCamPos;
        //if moving to desired orbit position would cause the camera to move into occlusion, shorten offset to before that happens
        if (Physics.SphereCast(followTarget.transform.position, COLLISION_SPHERECAST_RADIUS, desiredOffset, out RaycastHit hit, desiredOffset.magnitude, occluderLayers))
        {
            newCamPos = followTarget.transform.position + (followTarget.transform.TransformDirection(desiredOffset).normalized * Vector3.Distance(followTarget.transform.position, hit.point));
        }
        else
        {
            newCamPos = followTarget.transform.position + followTarget.transform.TransformDirection(desiredOffset);
        }

        return Vector3.Lerp(cam.transform.position, newCamPos, deltaTime * followSpeed);
    }

    //Takes the new camera position (after interpolating between current and desired camera positions) and 
    //avoids obstacles, if any, by moving the camera in front of the closest obstacle.
    //NOTE: This is the emergency "don't go inside geometry" function; its result shouldn't be interpolated, 
    //nor should it be used as the primary means of avoiding collision/occlusion, since it's too "snappy"
    private Vector3 AvoidCollisions(Vector3 camPos)
    {
        //if camera is colliding with an obstacle
        if(Physics.OverlapSphere(camPos, COLLISION_SPHERECAST_RADIUS, colliderLayers).Length > 0)
        {
            //cast a line from the follow target to the camera and move the camera in front of the first hit obstacle, if any
            if(Physics.Linecast(followTarget.transform.position, camPos, out RaycastHit hit, colliderLayers))
            {
                return preserveCameraHeight ? new Vector3(hit.point.x, camPos.y, hit.point.z) : hit.point;
            }
        }

        return camPos;
    }

    //Casts "whisker" rays from the follow target
    private Vector3 GetCamWhiskerOffset()
    {
        Vector3 whiskerPushDir = Vector3.zero;
        Vector3 followTargetPos = followTarget.transform.position;
        float halfWhiskerSectorAngle = whiskerSectorAngle / 2;
        float angleInc = (halfWhiskerSectorAngle / numWhiskers) * 2;

        for (float angle = -halfWhiskerSectorAngle; angle <= halfWhiskerSectorAngle; angle += angleInc)
        {
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * -followTarget.transform.forward;
            Debug.DrawRay(followTargetPos, dir * whiskerLength, Color.HSVToRGB(Mathf.Abs(angle) / whiskerSectorAngle, 0.2f, 1f)); //visualise raycasts
            if (Physics.Raycast(followTargetPos, dir, whiskerLength)) whiskerPushDir -= new Vector3(dir.x, 0f, dir.z);
        }

        //zero local z (is there a better way to do this?)
        whiskerPushDir = cam.transform.InverseTransformDirection(whiskerPushDir);
        whiskerPushDir.z = 0;
        whiskerPushDir = cam.transform.TransformDirection(whiskerPushDir);
        Debug.DrawRay(followTargetPos, whiskerPushDir, Color.yellow);

        return whiskerPushDir;
    }

    private Vector3 GetCamWhiskerResult(Vector3 camPos, Vector3 whiskerOffset, float deltaTime)
    {
        return Vector3.Lerp(camPos, camPos + whiskerOffset, whiskerPushStrength * deltaTime);
    }

    //Takes the desired camera position and adjusts it so the follow target isn't occluded by objects in the scene.
    //If the follow target would be occluded if the camera moved to desiredPos, this function moves desiredPos towards the follow target.
    //If there are no occluders in the way of the desired position, returns desiredPos unmodified
    private Vector3 GetOcclusionAvoidResult(Vector3 camPos, float deltaTime)
    {
        float maxDistance = Vector3.Distance(followTarget.transform.position, camPos);
        /*
        if(cam.RaycastsFromNearClipPane(cam.transform.forward, out _, maxDistance, occluderLayers, OCCLUSION_CLIP_PANE_PADDING))
        {
            foreach (Vector3 clipPaneCorner in cam.GetNearClipPaneCornersWorld(OCCLUSION_CLIP_PANE_PADDING))
            {
                Debug.DrawRay(clipPaneCorner, cam.transform.forward * maxDistance, Color.cyan);
            }

            Debug.DrawLine(cam.transform.position, Vector3.Lerp(camPos, followTarget.transform.position, deltaTime * cameraPullInSpeed), Color.green, 0.2f);
            Vector3 targetPos = preserveCameraHeight ? 
                new Vector3(followTarget.transform.position.x, cam.transform.position.y, followTarget.transform.position.z) 
                : followTarget.transform.position;

            return Vector3.Lerp(camPos, targetPos, deltaTime * cameraPullInSpeed);
        }
        */

        if(Physics.SphereCast(camPos, COLLISION_SPHERECAST_RADIUS, followTarget.transform.position - camPos, out _, maxDistance, occluderLayers))
        {
            Debug.DrawRay(camPos, (followTarget.transform.position - camPos).normalized * maxDistance, Color.cyan, 0.2f);
            
            Vector3 targetPos = preserveCameraHeight ?
                new Vector3(followTarget.transform.position.x, cam.transform.position.y, followTarget.transform.position.z)
                : followTarget.transform.position;

            Debug.DrawLine(camPos, targetPos, Color.green, 0.2f);
            return Vector3.Lerp(camPos, targetPos, deltaTime * cameraPullInSpeed);
        }

        return camPos;
    }

    //Takes the camera's desired move direction and checks if moving to it would cause the follow target to be occluded.
    //If so, returns the move direction shortened to stop before the occluding object, else returns the same move direction vector
    private Vector3 ShortenMoveDirectionIfMovingIntoOcclusion(Vector3 camMoveDirection)
    {
        //if moving to desired orbit position would cause the camera to move into occlusion, shorten offset to before that happens
        if (Physics.SphereCast(followTarget.transform.position, COLLISION_SPHERECAST_RADIUS, camMoveDirection, out RaycastHit hit, camMoveDirection.magnitude, occluderLayers))
        {
            return followTarget.transform.position + (camMoveDirection.normalized * Vector3.Distance(followTarget.transform.position, hit.point));
        }
    
        return followTarget.transform.position + camMoveDirection;
    }

    private Vector3 OrbitTarget(Vector3 camPos, float deltaTime)
    {
        mouseX += Input.GetAxis("Mouse X") * orbitSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * orbitSpeed;
        mouseX = Orbit_ClampMouseAngle(mouseX);
        mouseY = Mathf.Clamp(Orbit_ClampMouseAngle(mouseY), minOrbitYAngle, maxOrbitYAngle);

        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0f);

        Vector3 newCamPos;
        
        //if moving to desired orbit position would cause the camera to move into occlusion, shorten offset to before that happens
        if(Physics.SphereCast(followTarget.transform.position, COLLISION_SPHERECAST_RADIUS, rotation * desiredOffset, out RaycastHit hit, desiredOffset.magnitude, occluderLayers))
        {
            newCamPos = followTarget.transform.position + ((rotation * desiredOffset).normalized * Vector3.Distance(followTarget.transform.position, hit.point));
        }
        else
        {
            newCamPos = followTarget.transform.position + (rotation * desiredOffset);
        }

        return Vector3.Lerp(camPos, newCamPos, deltaTime * followSpeed);
    }

    private float Orbit_ClampMouseAngle(float mouseAngle)
    {
        if (mouseAngle < -360) return mouseAngle + 360;
        if (mouseAngle >= 360) return mouseAngle - 360;
        return mouseAngle;
    }

    //Returns the rotation which will make the camera look at the lookAtTarget
    private Quaternion GetTargetLookAtRotation(Vector3 lookAtTargetPos)
    {
        return Quaternion.LookRotation(lookAtTargetPos - cam.transform.position);
    }

}
