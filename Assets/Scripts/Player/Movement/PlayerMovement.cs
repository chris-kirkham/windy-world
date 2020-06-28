using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public abstract class PlayerMovement : MonoBehaviour
{
    /* constants */
    protected const float GROUND_RAYCAST_MAX_DISTANCE = 0.3f;
    protected const float MAX_WALKABLE_ANGLE = 45f; //maximum angle (in degrees) for ground that the player can walk on

    protected const float STEP_UP_MAX_STEP_HEIGHT = 0.4f; //maximum step height at which player will walk up a stair step
    protected const float STEP_UP_MAX_FORWARD_RAYCAST_DIST = 0.2f;
    protected const float STEP_UP_SPHERECAST_HEIGHT = 1.5f; //height to cast sphere upwards when checking if step up is obstructed
    protected const float STEP_UP_SPHERECAST_RADIUS = 0.2f;

    /* components */
    [SerializeField] protected Camera playerCamera;
    protected Animator animator;
    protected PlayerInput input;

    /* positions used for raycasts */
    [SerializeField] protected GameObject leftFoot;
    [SerializeField] protected GameObject rightFoot;
    [SerializeField] protected GameObject playerFloor;

    /* Layer masks */
    protected LayerMask levelGeometrySolid;

    /* status flags */
    protected bool isOnGround = false;
    protected bool isJumping = false;

    /* trackers */
    protected Vector3 lastNonZeroInput = Vector3.forward; //used to keep player facing their last movement direction when stopped

    /* land speed attributes */
    [SerializeField] protected float maxSpeed = 10f;
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
    [SerializeField] protected float jumpForce = 1f;

    [SerializeField] protected float maxJumpHoldTime = 0.1f;
    protected float currJumpHoldTime = 0f;

    /* air speed attributes */
    [SerializeField] protected float airControlAmount = 0.2f;
    [SerializeField] protected float maxAirSpeed = 100f;
    protected float sqrMaxAirSpeed;

    [SerializeField] protected float maxFallSpeed = 100f;
    protected float sqrMaxFallSpeed;
    [SerializeField] protected float extraFallSpeed = 1f;


    protected virtual void Start()
    {
        /* assign components */
        input = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        /* layer masks */
        levelGeometrySolid = LayerMask.GetMask("LevelGeometrySolid");

        /* initialise parameters */
        sqrSpeed = maxSpeed * maxSpeed;
        sqrMaxAirSpeed = maxAirSpeed * maxAirSpeed;
        sqrMaxFallSpeed = maxFallSpeed * maxFallSpeed;
        isOnGround = CalcIsOnGround();
    }

    protected virtual void OnValidate()
    {
        sqrSpeed = maxSpeed * maxSpeed;
    }

    protected Vector3 GetMovementInput()
    {
        Vector2 hv = input.GetHVAxis();

        //get camera facing vectors, ignoring up/down look
        Vector3 camFlatRight = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
        Vector3 camFlatFwd = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        
        Vector3 moveInput = (hv.x * camFlatRight) + (hv.y * camFlatFwd);
        
        //clamp magnitude if > 1 so diagonal movement isn't faster than movement on one axis
        return moveInput.sqrMagnitude > 1 ? moveInput.normalized : moveInput;
    }

    protected virtual Vector3 CalcMovement(Vector3 moveInput)
    {
        return isOnGround ? CalcGroundMvmt(moveInput) : CalcAirMvmt(moveInput);
    }

    protected virtual Vector3 CalcGroundMvmt(Vector3 moveInput)
    {
        /*
        //make move vector follow ground angle
        if(GroundRaycasts(out Vector3 avgNormal))
        {
            moveInput = Quaternion.LookRotation(Vector3.forward, avgNormal) * moveInput;
        }
        */

        return moveInput * maxSpeed;
    }

    protected virtual Vector3 CalcAirMvmt(Vector3 moveInput)
    {
        return moveInput * maxSpeed * airControlAmount;
    }

    protected float CalcJump()
    {
        if(input.GetJump())
        {
            if((isOnGround || isJumping) && currJumpHoldTime < maxJumpHoldTime)
            {
                isJumping = true;
                currJumpHoldTime += Time.deltaTime;
                return jumpForce;
            }
            else
            {
                return 0f;
            }
        }
        else
        {
            isJumping = false;
            currJumpHoldTime = 0f;
            return 0f;
        }
    }

    //Check if player is standing on the ground
    protected bool CalcIsOnGround()
    {
        if(GroundRaycasts(out Vector3 avgNormal))
        {
            float avgGroundAngle = Vector3.Angle(avgNormal, Vector3.up);
            return avgGroundAngle < MAX_WALKABLE_ANGLE;
        }
        else
        {
            return false;
        }
    }

    //Performs raycasts towards the ground; returns true if any raycasts hit, and logs the average of the hit normal(s) in avgNormal (Vector3.zero if no hit)
    protected bool GroundRaycasts(out Vector3 avgNormal)
    {
        avgNormal = Vector3.zero;
        int successfulRaycasts = 0;

        float raycastOffset = 0.2f;
        Vector3 playerFloorPos = playerFloor.transform.position + (Vector3.up * 0.2f);
        RaycastHit hit;
        //centre
        if (Physics.Raycast(playerFloorPos, Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, levelGeometrySolid))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //forward
        if (Physics.Raycast(playerFloorPos + (Vector3.forward * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, levelGeometrySolid))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //back
        if (Physics.Raycast(playerFloorPos + (Vector3.back * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, levelGeometrySolid))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //left
        if (Physics.Raycast(playerFloorPos + (Vector3.left * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, levelGeometrySolid))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //right
        if (Physics.Raycast(playerFloorPos + (Vector3.right * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, levelGeometrySolid))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        if (successfulRaycasts > 1) avgNormal /= successfulRaycasts;

        return successfulRaycasts > 0;
    }

    protected float CalcSlopeSpeedAdjustment(float slopeSpeedMult, Vector3 playerInput, float speedUpEndAngle, float slowStartAngle, float maxWalkableAngle)
    {
        //get average slope normal from ground raycasts
        //if no successful raycasts, we're not on the ground and shouldn't adjust speed;
        //if no player input, also don't adjust speed (necessary?)
        if (!GroundRaycasts(out Vector3 avgSlopeNormal) || playerInput.sqrMagnitude == 0f) return 1;

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

        stepUpHeight = 0f;
        return false;
    }

    //GETTERS

    public bool IsOnGround()
    {
        return isOnGround;
    }

    public bool IsJumping()
    {
        return isJumping;
    }
    
    public abstract Vector3 GetVelocity();
    
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
