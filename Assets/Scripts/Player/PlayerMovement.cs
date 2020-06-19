﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    /* components */
    public Camera playerCamera;
    private Animator animator;
    private Rigidbody rb;
    private PlayerInput input;

    //used for raycasts to check if player is on ground
    private GameObject leftFoot;
    private GameObject rightFoot;

    /* misc variables (probably move these somewhere better) */
    private float maxWalkableAngle = 45f; //maximum angle (in degrees) for ground that the player can walk on
    
    /* status flags */
    private bool isOnGround = false;

    /* land speed attributes */
    public float speed = 10f;
    
    private float sqrSpeed;

    //public float runSpeed = 150f;
    //public float sprintSpeed = 200f; //also max ground speed
    //private float sqrSprintSpeed; //used to check against player's velocity sqrMagnitude
    public float acceleration = 1f; //force multiplier to control ground acceleration 
    
    public float slowForce = 1f; //used to slow/stop character when not inputting a direction. Essentially manual drag for player movement 

    //if true, while accelerating, character will remain at one speed (walk/run) for a set time before accelerating to the next-highest speed.
    //if false, character will continuously accelerate up to the max ground speed.
    //public bool steppedSpeed = false; 
    //public int speedStayTimer = 200; //time in ms to stay at one speed before moving to the next when using steppedSpeed

    /* jump attributes */
    public float jumpForce = 1f;
    
    public float maxJumpHoldTime = 0.5f;
    private float currJumpHoldTime = 0f;

    /* air speed attributes */
    public float maxAirSpeed = 100f;
    
    public float fallSpeed = 1f;

    /* rotation tracker(s) */
    private Vector3 lastNonZeroInput = Vector3.forward; //used to keep player facing their last movement direction when stopped

    // Start is called before the first frame update
    void Start()
    {
        /* assign components */
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        //this is obviously not robust
        leftFoot = GameObject.Find("B-foot_L");
        rightFoot = GameObject.Find("B-foot_R");

        /* initialise parameters */
        sqrSpeed = speed * speed;
        isOnGround = IsOnGround();
    }

    private void OnValidate()
    {
        sqrSpeed = speed * speed;
    }

    private void Update()
    {
        animator.SetFloat("ForwardSpeed", new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude);
        animator.SetBool("Grounded", isOnGround);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
        if (!isOnGround) animator.SetFloat("AirborneVerticalSpeed", rb.velocity.y);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /* Update flags/trackers */
        isOnGround = IsOnGround();

        /* Horizontal movement */
        //calculate player movement
        Vector3 moveInput = GetMovementInput();
        Vector3 moveForce = CalcGroundMvmt(moveInput);

        //slow player if moving over max speed
        //float xzSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).sqrMagnitude;
        //if (xzSpeed < sqrSpeed) moveForce *= acceleration;
        //if (xzSpeed > sqrSpeed) moveForce -= moveForce.normalized * (xzSpeed - sqrSpeed);

        /* Jump/fall velocity */
        float yVelocity = 0f;
        if(CalcJump())
        {
            yVelocity += jumpForce;
        }
        else if(!isOnGround)
        {
            yVelocity -= fallSpeed;
        }

        //rb.AddForce(moveForce, ForceMode.Force);
        rb.velocity = new Vector3(moveForce.x, yVelocity, moveForce.z);

        /* Falling gravity modifier */
        if (!isOnGround) rb.AddForce(Physics.gravity * fallSpeed);

        /* Rotation */
        if(moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        transform.rotation = Quaternion.LookRotation(lastNonZeroInput, Vector3.up);
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lastNonZeroInput), Time.deltaTime * 10);
        
        //transform.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(lastNonZeroInput), 1 / rb.velocity.sqrMagnitude);
    }

    Vector3 GetMovementInput()
    {
        Vector2 hv = input.GetHVAxis();

        //get camera facing vectors, ignoring up/down look
        Vector3 camFlatRight = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
        Vector3 camFlatFwd = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        
        Vector3 moveInput = (hv.x * camFlatRight) + (hv.y * camFlatFwd);
        
        //clamp magnitude if > 1 so diagonal movement isn't faster than movement on one axis
        return moveInput.sqrMagnitude > 1 ? moveInput.normalized : moveInput;
    }

    private Vector3 CalcMovement(Vector3 moveInput)
    {
        return isOnGround ? CalcGroundMvmt(moveInput) : CalcAirMvmt(moveInput);
    }

    private Vector3 CalcGroundMvmt(Vector3 moveInput)
    {
        return (moveInput * speed) + CalcSlowForce(moveInput, slowForce);
    }

    private Vector3 CalcAirMvmt(Vector3 moveInput)
    {
        return (moveInput * speed * 0.2f);
    }

    private bool CalcJump()
    {
        if(input.GetJump())
        {
            if(currJumpHoldTime < maxJumpHoldTime)
            {
                currJumpHoldTime += Time.deltaTime;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            currJumpHoldTime = 0;
            return false;
        }
    }
    
    //Calculates an opposing force to directions player is moving in but not inputting - 
    //intended to act as increased drag to stop player sooner (and be more controllable than Unity's rigidbody drag setting)
    Vector3 CalcSlowForce(Vector3 moveInput, float slowMultiplier)
    {
        Vector3 slowForce = Vector3.zero;
        if (moveInput.x == 0) slowForce.x = -(rb.velocity.x * slowMultiplier); 
        if (moveInput.z == 0) slowForce.z = -(rb.velocity.z * slowMultiplier);
        return slowForce;
    }

    //Check if player is standing on the ground
    private bool IsOnGround()
    {
        //Fire rays down from positions around player
        RaycastHit hit;
        bool hasHit = false;
        float avgAngle = 0f; 
        
        if(Physics.Raycast(leftFoot.transform.position, Vector3.down, out hit, 0.2f, LayerMask.GetMask("LevelGeometrySolid")))
        {
            hasHit = true;
            avgAngle = Vector3.Angle(hit.normal, Vector3.up);
        }

        if (Physics.Raycast(rightFoot.transform.position, Vector3.down, out hit, 0.2f, LayerMask.GetMask("LevelGeometrySolid")))
        {
            hasHit = true;
            avgAngle = Vector3.Angle(hit.normal, Vector3.up);
        }

        Debug.Log("hasHit = " + hasHit);
        return hasHit && (avgAngle < maxWalkableAngle);
    }

}
