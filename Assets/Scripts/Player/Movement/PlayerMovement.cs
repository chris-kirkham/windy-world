using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerState))]
public abstract class PlayerMovement : MonoBehaviour
{
    /* constants */
    protected const float STEP_UP_MAX_STEP_HEIGHT = 0.4f; //maximum step height at which player will walk up a stair step
    protected const float STEP_UP_MAX_FORWARD_RAYCAST_DIST = 0.2f;
    protected const float STEP_UP_SPHERECAST_HEIGHT = 1.5f; //height to cast sphere upwards when checking if step up is obstructed
    protected const float STEP_UP_SPHERECAST_RADIUS = 0.2f;

    /* components */
    [Header("Components")]
    [SerializeField] protected Camera playerCamera;
    protected PlayerState state;
    protected Animator animator;
    protected PlayerInput input;

    /* positions used for raycasts */
    [SerializeField] protected GameObject leftFoot;
    [SerializeField] protected GameObject rightFoot;
    [SerializeField] protected GameObject playerFloor;

    /* Layer masks */
    protected LayerMask levelGeometrySolid;

    /* trackers */
    protected Vector3 lastNonZeroInput = Vector3.forward; //used to keep player facing their last movement direction when stopped

    /* land speed attributes */
    [Header("Ground movement attributes")]
    [SerializeField] protected float maxGroundSpeed = 10f;
    protected float sqrSpeed;

    //public float runSpeed = 150f;
    //public float sprintSpeed = 200f; //also max ground speed
    //protected float sqrSprintSpeed; //used to check against player's velocity sqrMagnitude
    [SerializeField] protected float acceleration = 1f; //force multiplier to control ground acceleration 

    //if true, while accelerating, character will remain at one speed (walk/run) for a set time before accelerating to the next-highest speed.
    //if false, character will continuously accelerate up to the max ground speed.
    //public bool steppedSpeed = false; 
    //public int speedStayTimer = 200; //time in ms to stay at one speed before moving to the next when using steppedSpeed

    /* jump attributes */
    [Header("Jump attributes")]
    [SerializeField] protected float jumpForce = 1f;

    //The minimum time in seconds jump force will be applied when jumping.
    //Jump force will continue to be applied up to this time, even if the player has released the jump input 
    [SerializeField] protected float minJumpHoldTime = 0.0f;
    
    [SerializeField] protected float maxJumpHoldTime = 0.1f;

    protected float currJumpHoldTime = 0f;

    /* air speed attributes */
    [Header("Air movement attributes")]
    [SerializeField] protected float airControlAmount = 0.2f;
    [SerializeField] protected float maxAirSpeed = 100f;
    protected float sqrMaxAirSpeed;

    [SerializeField] protected float maxFallSpeed = 100f;
    protected float sqrMaxFallSpeed;
    [SerializeField] protected float extraFallSpeed = 1f;

    protected virtual void Start()
    {
        /* assign components */
        state = GetComponent<PlayerState>();
        input = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        /* layer masks */
        levelGeometrySolid = LayerMask.GetMask("LevelGeometrySolid");

        /* initialise parameters */
        sqrSpeed = maxGroundSpeed * maxGroundSpeed;
        sqrMaxAirSpeed = maxAirSpeed * maxAirSpeed;
        sqrMaxFallSpeed = maxFallSpeed * maxFallSpeed;
    }

    protected virtual void OnValidate()
    {
        sqrSpeed = maxGroundSpeed * maxGroundSpeed;
    }

    public Vector3 GetMovementInput()
    {
        Vector2 hv = input.GetHVAxis();

        //get camera facing vectors, ignoring up/down look
        Vector3 camFlatRight = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
        Vector3 camFlatFwd = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        Vector3 moveInput = (hv.x * camFlatRight) + (hv.y * camFlatFwd);
        
        //clamp magnitude if > 1 so diagonal movement isn't faster than movement on one axis
        return moveInput.sqrMagnitude > 1 ? moveInput.normalized : moveInput;
    }

    protected virtual Vector3 CalcMovement(Vector3 moveInput, float speed)
    {
        return state.IsOnGround ? CalcGroundMvmt(moveInput, speed) : CalcAirMvmt(moveInput, speed);
    }

    protected virtual Vector3 CalcGroundMvmt(Vector3 moveInput, float speed)
    {
        /*
        //make move vector follow ground angle
        if(GroundRaycasts(out Vector3 avgNormal))
        {
            moveInput = Quaternion.LookRotation(Vector3.forward, avgNormal) * moveInput;
        }
        */

        return moveInput * speed;
    }

    protected virtual Vector3 CalcAirMvmt(Vector3 moveInput, float speed)
    {
        return moveInput * speed * airControlAmount;
    }

    protected float CalcSlopeSpeedAdjustment(float slopeSpeedMult, Vector3 playerInput, float speedUpEndAngle, float slowStartAngle, float maxWalkableAngle)
    {
        //get average slope normal from ground raycasts
        //if no successful raycasts, we're not on the ground and shouldn't adjust speed;
        //if no player input, also don't adjust speed (necessary?)
        if (!PlayerUtils.GroundRaycasts(playerFloor.transform.position, out Vector3 avgSlopeNormal, levelGeometrySolid) || playerInput.sqrMagnitude == 0f) return 1;

        //get angle between player input and slope normal; return speed multiplier based on this
        float angle = Vector3.Angle(playerInput, avgSlopeNormal) - 90f; //-90 so angle is between -90 and 90, with 0 being flat ground
        
        if (angle < speedUpEndAngle) return 1 + ((1 - Mathf.InverseLerp(-90, speedUpEndAngle, angle)) * slopeSpeedMult);
        if (angle > speedUpEndAngle && angle < slowStartAngle) return 1;
        if (angle < maxWalkableAngle) return 1 - Mathf.InverseLerp(slowStartAngle, maxWalkableAngle, angle);
        else return 0f; //angle > max walkable angle 
    }

    //Used when moving up stairs. Tries to find a valid next stair step;
    //returns true if found and sets stepUpHeight to the next step's height
    protected bool TryGetStepUpHeight(out float stepUpHeight)
    {
        stepUpHeight = 0f;

        if (!state.IsOnGround) return false; //don't let the player step up if they're not standing on anything
        
        //cast a ray forward from player's floor + max step height; if this hits something, it's obstructed
        Vector3 rayStart = playerFloor.transform.position + new Vector3(0f, STEP_UP_MAX_STEP_HEIGHT + 0.01f, 0f); //add 0.01f to height so rayEnd downwards ray doesn't hit flat ground
        Vector3 rayEnd = rayStart + (transform.forward * STEP_UP_MAX_FORWARD_RAYCAST_DIST);
        if (!Physics.Linecast(rayStart, rayEnd, levelGeometrySolid))
        {
            Debug.DrawLine(rayStart, rayEnd, Color.blue);
            
            //if first ray isn't obstructed, cast downwards to find height of geometry;
            //if it's higher than player's current floor height (which it will be if the ray hits and its maxDistance is max step height), it's considered a step up
            RaycastHit hit;
            if (Physics.Raycast(rayEnd, Vector3.down, out hit, STEP_UP_MAX_STEP_HEIGHT, levelGeometrySolid))
            {
                Debug.DrawRay(rayEnd, Vector3.down * STEP_UP_MAX_STEP_HEIGHT, Color.cyan);

                //if step up is found, sphere cast upwards to check if player can stand on step
                Ray sphereRay = new Ray(hit.point, Vector3.up);
                if(!Physics.Raycast(sphereRay, STEP_UP_SPHERECAST_HEIGHT, levelGeometrySolid))
                {
                    stepUpHeight = hit.point.y - playerFloor.transform.position.y;
                    return true;
                }
            }
        }

        return false;
    }
    
    //GETTERS
    public abstract Vector3 GetVelocity();

    public float GetMinJumpHoldTime()
    {
        return minJumpHoldTime;
    }

    public float GetMaxJumpHoldTime()
    {
        return maxJumpHoldTime;
    }

    private void OnDrawGizmos()
    {
        if(EditorApplication.isPlaying)
        {
            float height;
            if (TryGetStepUpHeight(out height))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(transform.position + (Vector3.up * height), 0.05f);
            }
        }
    }
}
