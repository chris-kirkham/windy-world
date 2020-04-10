using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    /* ----- components ----- */
    private Rigidbody rb;

    /* ----- misc variables (probably move these somewhere better) ----- */
    private float maxWalkableAngle = 45f; //maximum angle (in degrees) for ground that the player can walk on
    
    /* ----- status flags ----- */
    private bool isOnGround = false;

    /* ----- land speed attributes ----- */
    public float walkSpeed = 100f;
    public float runSpeed = 150f;
    public float sprintSpeed = 200f; //also max ground speed
    private float sqrSprintSpeed; //used to check against player's velocity sqrMagnitude
    
    public float acceleration = 1f; //force multiplier to control ground acceleration 

    /* if true, while accelerating, character will remain at one speed (walk/run) for a set time before accelerating to the next-highest speed.
     * if false, character will continuously accelerate up to the max ground speed. */
    public bool steppedSpeed = true; 
    public int speedStayTimer = 200; //time in ms to stay at one speed before moving to the next when using steppedSpeed

    /* ---- air speed attributes ---- */
    public float maxAirSpeed = 10000f;

    /*
    // Start is called before the first frame update
    void Start()
    {
        //assign components
        rb = GetComponent<Rigidbody>();

        sqrSprintSpeed = sprintSpeed * sprintSpeed;
        isOnGround = IsOnGround();
    }

    // Update is called once per frame
    void Update()
    {
        isOnGround = IsOnGround();
        
        //calculate player movement
        Vector3 moveInput = GetMovementInput();
        rb.AddForce(CalcMovement(moveInput), ForceMode.Acceleration);
        
    }
    
    Vector3 GetMovementInput()
    {
        float inputHorizontal = Input.GetAxis("Horizontal");
        float inputVertical = Input.GetAxis("Vertical");
        
        Vector3 moveInput = new Vector3(inputHorizontal, 0.0f, inputVertical);
        moveInput = ballCamera.transform.TransformDirection(movement); //transform movement input so its direction is relative to the camera
        
        return moveInput;
    }
    
    Vector3 CalcMovement(Vector3 moveInput)
    {
        //limit speed by not adding more force to player if already at max ground/air speed
        if(isOnGround)
        {
            if(rb.velocity.sqrMagnitude <= sqrSprintSpeed) return CalcGroundMvmt(moveInput); 
        }
        else
        {
        }

    }
    
    Vector3 CalcGroundMvmt(Vector3 moveInput)
    {
        if(!steppedSpeed)
        {
            
            
        }
        else
        {
            
        }
        
    }
    
    Vector3 CalcAirMvmt(Vector3 moveInput)
    {
        
    }
    
    //check if player is standing on the ground
    private bool IsOnGround()
    {
        //check colliding with surface
        
        //check surface angle
        
        return false;
    }
    */
}
