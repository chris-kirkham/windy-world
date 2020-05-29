using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Camera_Fixed_PanToMoveDirection : MonoBehaviour
{
    /*
    public GameObject target;
    public PlayerMovementInfo targetMoveInfo;
    public float lerpSpeedXZ = 1f;
    public float lerpSpeedY = 1f;

    //lerp buildup with continuous (similar) move force
    public bool useLerpBuildup = false;
    private float lerpBuildupStart = 0.2f;
    private float lerpBuildupInc = 0.01f;
    private float currentLerpBuildup;
    private Vector3 lastMoveForce; //used in dot product with current move force to check similarity

    private Camera c;
    private Vector3 offset;

    void Start()
    {
        c = GetComponent<Camera>();
        offset = c.transform.position - target.transform.position;

        //lerp buildup variables
        currentLerpBuildup = 1f;
        lastMoveForce = Vector3.zero;
    }

    void LateUpdate()
    {
        float lerpAmtXZ = Time.deltaTime * lerpSpeedXZ;
        if (!targetMoveInfo.isOnGround) lerpAmtXZ *= targetMoveInfo.airControlMultiplier;
        float lerpAmtY = Time.deltaTime * lerpSpeedY;

        if (useLerpBuildup)
        {
            if(Vector3.Dot(lastMoveForce.normalized, targetMoveInfo.moveForce.normalized) > 0f)
            {
                currentLerpBuildup = Mathf.Min(1, currentLerpBuildup + lerpBuildupInc);
            }
            else
            {
                currentLerpBuildup = lerpBuildupStart;
            }

            lerpAmtXZ *= currentLerpBuildup;
            lerpAmtY *= currentLerpBuildup;

            lastMoveForce = targetMoveInfo.moveForce;
            //Debug.Log(currentLerpBuildup);
        }

        Vector3 newPos = target.transform.position + offset + targetMoveInfo.moveForce;
        c.transform.position = new Vector3(Mathf.Lerp(c.transform.position.x, newPos.x, lerpAmtXZ),
                                           Mathf.Lerp(c.transform.position.y, newPos.y, lerpAmtY),
                                           Mathf.Lerp(c.transform.position.z, newPos.z, lerpAmtXZ));

        //TODO: scale lerp (pan) amount(?) for screen axes by screen width/height, so movement to screen up/down will pan the object back
        //the same screen proportion as movement left/right
    }
    */
}
