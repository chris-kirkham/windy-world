using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonFollow))]
public class CameraFollow : MonoBehaviour
{
    private ThirdPersonFollow camMain;
    [SerializeField] private GameObject followTarget;
    [SerializeField] private PlayerInput input;
    [SerializeField] private bool useFollowTargetPlayerInput;

    public Vector3 desiredOffsetFromTarget = Vector3.zero;
    [SerializeField] private bool worldSpaceOffset = false;

    [SerializeField] private float minDistFromTarget = 1f;
    [SerializeField] private float maxDistFromTarget = 2f;
    private float sqrMinDistFromTarget;
    private float sqrMaxDistFromTarget;

    [SerializeField] private bool interpolate = true;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float orbitSpeed = 10f;
    [Range(-360, 360)] [SerializeField] private float minOrbitYAngle = 0f;
    [Range(-360, 360)] [SerializeField] private float maxOrbitYAngle = 90f;

    private Vector3 smoothDampVelocity = Vector3.zero;

    private enum State { FollowingTarget, TargetMovingTowardsCamera, OrbitingTarget }

    private void Start()
    {
        //followTarget = camMain.followTarget;
        if(useFollowTargetPlayerInput && !followTarget.TryGetComponent(out input))
        {
            Debug.LogError("No PlayerInput component on followTarget!");
        }
        
        sqrMinDistFromTarget = minDistFromTarget * minDistFromTarget;
        sqrMaxDistFromTarget = maxDistFromTarget * maxDistFromTarget;
    }

    private void OnValidate()
    {
        sqrMinDistFromTarget = minDistFromTarget * minDistFromTarget;
        sqrMaxDistFromTarget = maxDistFromTarget * maxDistFromTarget;
    }

    public Vector3 UpdatePosition(Vector3 camPos, Vector3 followTargetPos)
    {
        Vector3 desiredPos;
        float lerpSpeed; //different camera states (e.g. orbit vs follow) may have different lerp speeds
        Debug.Log("State: " + GetFollowState());

        //get desired camera position and lerp speed based on follow state
        switch (GetFollowState())
        {
            case State.OrbitingTarget:
                desiredPos = GetOrbitPos(followTargetPos);
                lerpSpeed = orbitSpeed;
                break;
            case State.TargetMovingTowardsCamera:
                desiredPos = GetFrontFollowPos(followTargetPos);
                lerpSpeed = followSpeed;
                break;
            case State.FollowingTarget:
            default:
                desiredPos = GetFollowPos(followTargetPos);
                lerpSpeed = followSpeed;
                break;
        }

        if (camPos == desiredPos)
        {
            return camPos;
        }
        else //move camera towards desired position
        {
            //Vector3 newPos = interpolate ? Vector3.Slerp(camPos, desiredPos, Time.deltaTime * lerpSpeed) : desiredPos;
            Vector3 newPos = interpolate ? Vector3.SmoothDamp(camPos, desiredPos, ref smoothDampVelocity, 1f / lerpSpeed) : desiredPos;

            //Clamp newPos to min and max distances
            /*
            float newPosTargetSqrDist = (followTargetPos - newPos).sqrMagnitude;
            Vector3 targetToNewPosUnit = (newPos - followTargetPos).normalized;
            if (newPosTargetSqrDist < sqrMinDistFromTarget)
            {
                newPos = followTargetPos + (targetToNewPosUnit * minDistFromTarget);
            }
            else if (newPosTargetSqrDist > sqrMaxDistFromTarget)
            {
                newPos = followTargetPos + (targetToNewPosUnit * maxDistFromTarget);
            }
            */
            return newPos;
        }
    }

    private Vector3 GetFollowPos(Vector3 followTargetPos)
    {
        return worldSpaceOffset ? followTargetPos + desiredOffsetFromTarget : followTargetPos + followTarget.transform.TransformDirection(desiredOffsetFromTarget);
    }

    private Vector3 GetFrontFollowPos(Vector3 followTargetPos)
    {
        Vector3 frontOffset = new Vector3(desiredOffsetFromTarget.x, desiredOffsetFromTarget.y, Mathf.Abs(desiredOffsetFromTarget.z));
        return followTargetPos + (worldSpaceOffset ? frontOffset : followTarget.transform.TransformDirection(frontOffset));
    }

    private Vector3 GetOrbitPos(Vector3 followTargetPos)
    {
        Vector2 mouse = input.GetMouseAxis();
        float x = ClampMouseAngle(mouse.x * orbitSpeed);
        float y = ClampMouseAngle(mouse.y * orbitSpeed, minOrbitYAngle, maxOrbitYAngle);

        Quaternion rotation = Quaternion.Euler(y, x, 0f);

        return followTargetPos + (rotation * desiredOffsetFromTarget);
    }

    private State GetFollowState()
    {
        if(input.GetMouseAxisChange() != Vector2.zero)
        {
            return State.OrbitingTarget;
        }
        else if (Vector3.Dot(transform.forward, followTarget.transform.forward) > -0.75f)
        {
            return State.FollowingTarget;
        }
        else
        {
            return State.TargetMovingTowardsCamera;
        }
    }

    //Clamps mouse angle between 0 and 360, or between min and max where -360 <= min <= max <= 360
    private float ClampMouseAngle(float mouseAngle, float min = -360f, float max = 360f)
    {
        if (mouseAngle < -360f) mouseAngle += 360f;
        else if (mouseAngle >= 360f) mouseAngle -= 360f;
        return Mathf.Clamp(mouseAngle, min, max);
    }
}
