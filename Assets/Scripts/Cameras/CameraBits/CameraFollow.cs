using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonFollow))]
public class CameraFollow : MonoBehaviour
{
    private ThirdPersonFollow camMain;
    private GameObject followTarget;
    private PlayerInput input;

    public Vector3 desiredOffsetFromTarget = Vector3.zero;
    public bool worldSpaceOffset = false;

    public float minDistFromTarget = 1f;
    public float maxDistFromTarget = 2f;
    private float sqrMinDistFromTarget;
    private float sqrMaxDistFromTarget;

    public bool interpolate = true;
    public float followSpeed = 5f;
    public float orbitSpeed = 10f;

    private Vector3 smoothDampVelocity = Vector3.zero;

    private enum State { FollowingTarget, TargetMovingTowardsCamera, OrbitingTarget }

    private void Start()
    {
        followTarget = camMain.followTarget;
        input = GetComponent<PlayerInput>();

        sqrMinDistFromTarget = minDistFromTarget * minDistFromTarget;
        sqrMaxDistFromTarget = maxDistFromTarget * maxDistFromTarget;
    }

    private void OnValidate()
    {
        sqrMinDistFromTarget = minDistFromTarget * minDistFromTarget;
        sqrMaxDistFromTarget = maxDistFromTarget * maxDistFromTarget;
    }

    private Vector3 UpdatePosition(Vector3 camPos, Vector3 followTargetPos)
    {
        Vector3 desiredPos;
        float lerpSpeed; //different camera states (e.g. orbit vs follow) may have different lerp speeds

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
        float y = ClampMouseAngle(mouse.y * orbitSpeed);

        Quaternion rotation = Quaternion.Euler(y, x, 0f);

        return followTargetPos + (rotation * desiredOffsetFromTarget);
    }

    private State GetFollowState()
    {
        if(input.GetMouseAxis() != Vector2.zero)
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

    private float ClampMouseAngle(float mouseAngle)
    {
        if (mouseAngle < 0) return mouseAngle + 360;
        if (mouseAngle >= 360) return mouseAngle - 360;
        return mouseAngle;
    }
}
