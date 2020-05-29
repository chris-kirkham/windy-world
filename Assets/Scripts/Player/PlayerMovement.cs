using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    /* components */
    private Rigidbody rb;
    public Camera playerCamera;

    /* misc variables (probably move these somewhere better) */
    private float maxWalkableAngle = 45f; //maximum angle (in degrees) for ground that the player can walk on
    
    /* status flags */
    private bool isOnGround = false;

    /* land speed attributes */
    public float walkSpeed = 100f;
    public float runSpeed = 150f;
    public float sprintSpeed = 200f; //also max ground speed
    private float sqrSprintSpeed; //used to check against player's velocity sqrMagnitude
    public float acceleration = 1f; //force multiplier to control ground acceleration 
    public float slowForce = 1f; //used to slow/stop character when not inputting a direction. Essentially manual drag for player movement 

    /* if true, while accelerating, character will remain at one speed (walk/run) for a set time before accelerating to the next-highest speed.
     * if false, character will continuously accelerate up to the max ground speed. */
    public bool steppedSpeed = true; 
    public int speedStayTimer = 200; //time in ms to stay at one speed before moving to the next when using steppedSpeed

    /* air speed attributes */
    public float maxAirSpeed = 10000f;

    /* rotation tracker(s) */
    private Vector3 lastNonZeroInput = Vector3.forward; //used to keep player facing their last movement direction when stopped

    // Start is called before the first frame update
    void Start()
    {
        //assign components
        rb = GetComponent<Rigidbody>();

        sqrSprintSpeed = sprintSpeed * sprintSpeed;
        isOnGround = IsOnGround();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        isOnGround = IsOnGround();
        
        //calculate player movement
        Vector3 moveInput = GetMovementInput();
        Vector3 moveForce = (moveInput * walkSpeed) + CalcSlowForce(moveInput, slowForce);
        rb.AddForce(moveForce, ForceMode.Force);
        if(moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        transform.rotation = Quaternion.LookRotation(lastNonZeroInput);
        //transform.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(lastNonZeroInput), 1 / rb.velocity.sqrMagnitude);
        //rb.AddForce(CalcMovement(moveInput), ForceMode.Force);

        //Debug.Log("Speed = " + rb.velocity.magnitude);

    }

    Vector3 GetMovementInput()
    {
        float inputHorizontal = Input.GetAxis("Horizontal");
        float inputVertical = Input.GetAxis("Vertical");
        
        Vector3 moveInput = new Vector3(inputHorizontal, 0.0f, inputVertical);
        moveInput = playerCamera.transform.TransformDirection(moveInput); //transform movement input so its direction is relative to the camera
        
        return moveInput;
    }
    
    Vector3 CalcMovement(Vector3 moveInput)
    {
        throw new NotImplementedException();

        //limit speed by not adding more force to player if already at max ground/air speed
        if (isOnGround)
        {
            if(rb.velocity.sqrMagnitude <= sqrSprintSpeed) return CalcGroundMvmt(moveInput); 
        }
        else
        {
        }

    }
    
    Vector3 CalcGroundMvmt(Vector3 moveInput)
    {
        throw new NotImplementedException();

        if (!steppedSpeed)
        {
            
            
        }
        else
        {
            
        }
        
    }
    
    Vector3 CalcAirMvmt(Vector3 moveInput)
    {
        throw new NotImplementedException();
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
        //check colliding with surface
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            //check surface angle
            if (Vector3.Angle(hit.normal, Vector3.down) < maxWalkableAngle) return true;
        }

        return false;
    }

}
