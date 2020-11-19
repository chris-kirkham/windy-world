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
    [Min(2)] public int numWhiskers = 4;
    public float whiskerLength = 1f;
    [Range(0, 360)] public float whiskerSectorAngle = 180;

    [Header("Collision avoidance")]
    public float collisionPreemptDistance = 1f;



    //Const params - stuff that's not exposed to the inspector because it doesn't affect artistic aspects of camera control
    //so much as basic functioning of collision/occluson avoidance etc.
    private const float REAR_COLLISION_AVOID_DIST = 0f;
    private const float COLLISION_SPHERECAST_RADIUS = 0.5f;
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
        Vector3 newCamPosition = GetFollowPosition(GetDesiredFollowPosition(desiredOffset), Time.fixedDeltaTime);
        newCamPosition = GetOcclusionAvoidResult(newCamPosition, Time.fixedDeltaTime);
        //newCamPosition = AvoidCollisions(newCamPosition);
        GetCamWhiskerResult(newCamPosition, Time.fixedDeltaTime);

        cam.transform.position = newCamPosition;
        cam.transform.rotation = GetTargetLookAtRotation();
    }

    //Returns the desired target follow position as specified by the target's position and the target follow offset params.
    //This is the "ideal" position the camera wants to be in, before taking into consideration obstacle/occlusion avoidance etc.
    private Vector3 GetDesiredFollowPosition(Vector3 desiredOffset)
    {
        return followTarget.transform.position + followTarget.transform.TransformDirection(desiredOffset);
    }

    //Returns the camera position after interpolating between the current camera position and the desired follow position
    private Vector3 GetFollowPosition(Vector3 desiredPosition, float deltaTime)
    {
        return Vector3.Lerp(cam.transform.position, desiredPosition, Time.fixedDeltaTime * followSpeed);
    }

    //Takes the new camera position (after interpolating between current and desired camera positions) and 
    //avoids obstacles, if any, by moving the camera in front of the closest obstacle.
    //NOTE: This is the emergency "don't go inside geometry" function; its result shouldn't be interpolated, 
    //nor should it be used as the primary means of avoiding collision/occlusion, since it's too "snappy"
    private Vector3 AvoidCollisions(Vector3 camPos)
    {
        Vector3 dir = camPos - followTarget.transform.position;
        if (Physics.SphereCast(followTarget.transform.position, COLLISION_SPHERECAST_RADIUS, dir, out RaycastHit hit, dir.magnitude, colliderLayers))
        {
            Debug.DrawLine(followTarget.transform.position, camPos, Color.red, 0.1f);
            return preserveCameraHeight ? new Vector3(hit.point.x, camPos.y, hit.point.z) : hit.point;
        }

        return camPos;
    }

    //Casts "whisker" rays from the follow target
    private Vector3 GetCamWhiskerResult(Vector3 camPos, float deltaTime)
    {
        Vector3 newCamPos = Vector3.zero;
        Vector3 followTargetPos = followTarget.transform.position;
        float halfWhiskerSectorAngle = whiskerSectorAngle / 2;
        float angleInc = (halfWhiskerSectorAngle / numWhiskers) * 2;

        for (float angle = -halfWhiskerSectorAngle; angle <= halfWhiskerSectorAngle; angle += angleInc)
        {
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * -followTarget.transform.forward;
            Debug.DrawRay(followTargetPos, dir * whiskerLength, Color.HSVToRGB(Mathf.Abs(angle) / whiskerSectorAngle, 0.2f, 1f)); //visualise raycasts
            if (Physics.Raycast(followTargetPos, dir, whiskerLength)) newCamPos -= new Vector3(dir.x, 0f, dir.z);
        }

        //zero local z (is there a better way to do this?)
        newCamPos = followTarget.transform.InverseTransformDirection(newCamPos);
        newCamPos.z = 0;
        newCamPos = followTarget.transform.TransformDirection(newCamPos);
        Debug.DrawRay(followTargetPos + Vector3.up, newCamPos, Color.yellow);
        
        return newCamPos;
    }

    //Takes the desired camera position and adjusts it so the follow target isn't occluded by objects in the scene.
    //If the follow target would be occluded if the camera moved to desiredPos, this function moves desiredPos towards the follow target.
    //If there are no occluders in the way of the desired position, returns desiredPos unmodified
    private Vector3 GetOcclusionAvoidResult(Vector3 camPos, float deltaTime)
    {
        foreach (Vector3 clipPaneCorner in cam.GetNearClipPaneCornersWorld())
        {
           Debug.DrawRay(clipPaneCorner, cam.transform.forward * Mathf.Abs(desiredOffset.z), Color.cyan);
        }

        if(cam.RaycastsFromNearClipPane(cam.transform.forward, out _, Mathf.Abs(desiredOffset.z), occluderLayers))
        {
            Debug.DrawLine(cam.transform.position, Vector3.Lerp(camPos, followTarget.transform.position, deltaTime * cameraPullInSpeed), Color.green, 0.2f);
            Vector3 targetPos = preserveCameraHeight ? 
                new Vector3(followTarget.transform.position.x, cam.transform.position.y, followTarget.transform.position.z) 
                : followTarget.transform.position;

            return Vector3.Lerp(camPos, targetPos, deltaTime * cameraPullInSpeed);
        }

        return camPos;
    }

    //Returns the rotation which will make the camera look at the lookAtTarget
    private Quaternion GetTargetLookAtRotation()
    {
        return Quaternion.LookRotation(lookAtTarget.transform.position - cam.transform.position);
    }


}
