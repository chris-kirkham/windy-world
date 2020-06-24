using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    /* constants */
    protected const float GROUND_RAYCAST_MAX_DISTANCE = 0.15f;
    protected const float MAX_WALKABLE_ANGLE = 45f; //maximum angle (in degrees) for ground that the player can walk on

    protected const float STEP_UP_MAX_STEP_HEIGHT = 0.4f; //maximum step height at which player will walk up a stair step
    protected const float STEP_UP_MAX_FORWARD_RAYCAST_DIST = 0.5f;
    protected const float STEP_UP_SPHERECAST_HEIGHT = 1.5f; //height to cast sphere upwards when checking if step up is obstructed
    protected const float STEP_UP_SPHERECAST_RADIUS = 0.2f;

    /* components */
    protected Rigidbody rb;
    public Camera playerCamera;
    protected Animator animator;
    protected PlayerInput input;

    /* positions used for raycasts */
    protected GameObject leftFoot;
    protected GameObject rightFoot;
    protected GameObject playerFloor;

    /* Layer masks */
    protected LayerMask levelGeometrySolid;

    /* status flags */
    protected bool isOnGround = false;
    protected bool isJumping = false;

    /* land speed attributes */
    public float speed = 10f;
    protected float sqrSpeed;

    //public float runSpeed = 150f;
    //public float sprintSpeed = 200f; //also max ground speed
    //protected float sqrSprintSpeed; //used to check against player's velocity sqrMagnitude
    public float acceleration = 1f; //force multiplier to control ground acceleration 
    
    public float slowForce = 1f; //used to slow/stop character when not inputting a direction. Essentially manual drag for player movement 

    //if true, while accelerating, character will remain at one speed (walk/run) for a set time before accelerating to the next-highest speed.
    //if false, character will continuously accelerate up to the max ground speed.
    //public bool steppedSpeed = false; 
    //public int speedStayTimer = 200; //time in ms to stay at one speed before moving to the next when using steppedSpeed

    /* jump attributes */
    public float jumpForce = 1f;
    
    public float maxJumpHoldTime = 0.5f;
    protected float currJumpHoldTime = 0f;

    /* air speed attributes */
    public float airControlAmount = 0.2f; 
    public float maxAirSpeed = 100f;
    protected float sqrMaxAirSpeed;

    public float maxFallSpeed = 100f;
    protected float sqrMaxFallSpeed;
    public float extraFallSpeed = 1f;

    /* rotation tracker(s) */
    protected Vector3 lastNonZeroInput = Vector3.forward; //used to keep player facing their last movement direction when stopped


    protected void Start()
    {
        /* assign components */
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        /* layer masks */
        levelGeometrySolid = LayerMask.GetMask("LevelGeometrySolid");

        //this is obviously not robust
        leftFoot = GameObject.Find("B-foot_L");
        rightFoot = GameObject.Find("B-foot_R");
        playerFloor = GameObject.Find("PlayerFloor");

        /* initialise parameters */
        sqrSpeed = speed * speed;
        sqrMaxAirSpeed = maxAirSpeed * maxAirSpeed;
        sqrMaxFallSpeed = maxFallSpeed * maxFallSpeed;
        isOnGround = IsOnGround();
    }

    protected void OnValidate()
    {
        sqrSpeed = speed * speed;
    }

    protected void Update()
    {
        animator.SetFloat("ForwardSpeed", new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude);
        animator.SetBool("Grounded", isOnGround);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
        if (!isOnGround) animator.SetFloat("AirborneVerticalSpeed", rb.velocity.y);
    }

    // Update is called once per frame
    protected virtual void FixedUpdate()
    {
        /* Update flags/trackers */
        isOnGround = IsOnGround();

        /* Movement */
        Vector3 moveInput = GetMovementInput();
        Vector3 moveForce = CalcMovement(moveInput);
        Vector3 jump = new Vector3(0f, CalcJump(), 0f);
        
        rb.velocity += moveForce + jump;
        if (!isOnGround && rb.velocity.y < 0) rb.velocity += Physics.gravity * extraFallSpeed;

        /* Clamp horizontal (xz) and vertical (y) speed to maximums */
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float maxSpeed = isOnGround ? speed : maxAirSpeed;
        if (flatVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            Vector3 clampedFlatVel = flatVelocity.normalized * maxSpeed;
            rb.velocity = new Vector3(clampedFlatVel.x, rb.velocity.y, clampedFlatVel.z); 
        }

        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
        }

        /* Rotation */
        if(moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        transform.rotation = Quaternion.LookRotation(lastNonZeroInput, Vector3.up);
        //transform.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(lastNonZeroInput), 1 / rb.velocity.sqrMagnitude);
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

    protected Vector3 CalcMovement(Vector3 moveInput)
    {
        return isOnGround ? CalcGroundMvmt(moveInput) : CalcAirMvmt(moveInput);
    }

    protected Vector3 CalcGroundMvmt(Vector3 moveInput)
    {
        return (moveInput * speed) + XZDrag(moveInput, slowForce);
    }

    protected Vector3 CalcAirMvmt(Vector3 moveInput)
    {
        return (moveInput * speed * 0.2f) + XZDrag(moveInput, slowForce);
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

    //Calculates an opposing force to directions player is moving in but not inputting - 
    //intended to act as increased drag to stop player sooner (and be more controllable than Unity's rigidbody drag setting)
    protected Vector3 XZDrag(Vector3 moveInput, float slowMultiplier)
    {
        Vector3 slowForce = Vector3.zero;
        if (moveInput.x == 0) slowForce.x = -(rb.velocity.x * slowMultiplier); 
        if (moveInput.z == 0) slowForce.z = -(rb.velocity.z * slowMultiplier);
        return slowForce;
    }

    //Check if player is standing on the ground
    protected bool IsOnGround()
    {
        RaycastHit hit;
        bool hasHit = false;
        float avgAngle = 0f;

        Debug.DrawRay(leftFoot.transform.position, Vector3.down * GROUND_RAYCAST_MAX_DISTANCE, Color.green);
        Debug.DrawRay(rightFoot.transform.position, Vector3.down * GROUND_RAYCAST_MAX_DISTANCE, Color.green);

        if (Physics.Raycast(leftFoot.transform.position, Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, levelGeometrySolid))
        {
            hasHit = true;
            avgAngle = Vector3.Angle(hit.normal, Vector3.up);
        }

        if (Physics.Raycast(rightFoot.transform.position, Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, levelGeometrySolid))
        {
            hasHit = true;
            avgAngle = Vector3.Angle(hit.normal, Vector3.up);
        }

        return hasHit && (avgAngle < MAX_WALKABLE_ANGLE);
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
