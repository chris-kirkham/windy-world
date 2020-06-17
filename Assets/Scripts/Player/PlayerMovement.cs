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
    
    public float extraFallGravity = 0f;

    /* rotation tracker(s) */
    private Vector3 lastNonZeroInput = Vector3.forward; //used to keep player facing their last movement direction when stopped

    // Start is called before the first frame update
    void Start()
    {
        //assign components
        rb = GetComponent<Rigidbody>();

        sqrSpeed = speed * speed;
        isOnGround = IsOnGround();
    }

    private void OnValidate()
    {
        sqrSpeed = speed * speed;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /* Update flags/trackers */
        isOnGround = IsOnGround();
        Debug.Log(isOnGround);

        /* Horizontal movement */
        //calculate player movement
        Vector3 moveInput = GetMovementInput();
        Vector3 moveForce = CalcGroundMvmt(moveInput);

        //slow player if moving over max speed
        float speed = rb.velocity.sqrMagnitude;
        if (speed < sqrSpeed) moveForce *= acceleration;
        if (speed > sqrSpeed) moveForce -= moveForce.normalized * (speed - sqrSpeed);
        
        rb.AddForce(moveForce, ForceMode.Force);

        /* Jump */


        /* Falling gravity modifier */



        /* Rotation */
        if(moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        transform.rotation = Quaternion.LookRotation(lastNonZeroInput, Vector3.up);
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lastNonZeroInput), Time.deltaTime * 10);
        
        //transform.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(lastNonZeroInput), 1 / rb.velocity.sqrMagnitude);
    }

    Vector3 GetMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        //transform movement input so its direction is relative to the camera
        Vector3 moveInput = playerCamera.transform.TransformDirection(new Vector3(horizontal, 0f, vertical));
        moveInput.y = 0f; //zero y so player doesn't try to move up towards camera if it's above them

        return moveInput.normalized;
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

    private float CalcJump()
    {
        if(Input.GetKey(KeyCode.Space))
        {
            if(currJumpHoldTime < maxJumpHoldTime)
            {
                currJumpHoldTime += Time.deltaTime;
                return 1;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            currJumpHoldTime = 0;
            return 0;
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
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1f, LayerMask.GetMask("LevelGeometrySolid"))) avgAngle += Vector3.Angle(hit.normal, Vector3.up); hasHit = true;
        /*
        if (Physics.Raycast(transform.position + Vector3.forward, Vector3.down, out hit, 2f)) avgAngle += Vector3.Angle(hit.normal, Vector3.up); hasHit = true;
        if (Physics.Raycast(transform.position + Vector3.right, Vector3.down, out hit, 2f)) avgAngle += Vector3.Angle(hit.normal, Vector3.up); hasHit = true;
        if (Physics.Raycast(transform.position + Vector3.back, Vector3.down, out hit, 2f)) avgAngle += Vector3.Angle(hit.normal, Vector3.up); hasHit = true;
        if (Physics.Raycast(transform.position + Vector3.left, Vector3.down, out hit, 2f)) avgAngle += Vector3.Angle(hit.normal, Vector3.up); hasHit = true;
        avgAngle /= 5;
        */
        return hasHit && (avgAngle < maxWalkableAngle);
    }

}
