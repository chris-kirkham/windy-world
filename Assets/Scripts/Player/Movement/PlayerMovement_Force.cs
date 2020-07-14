using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Variant of PlayerMovement which exclusively uses AddForce to control movement.
/// Should work better with the physics system but will be harder to tweak since we can't set the velocity directly
/// </summary>
public class PlayerMovement_Force : PlayerMovement_Rigidbody
{
    [Header("Force attributes")]
    [SerializeField] private bool useForceCurve = true;
    [SerializeField] private AnimationCurve forceCurve = new AnimationCurve();
    [SerializeField] private ForceMode forceMode = ForceMode.Force;
    [SerializeField] private float slowAmount = 1f; //used to slow/stop character when not inputting a direction. Essentially manual drag for player movement 

    /* Constants */
    protected const float MAX_ADDFORCE_MAGNITUDE = 100f;

    /* Trackers */
    protected float fallTime = 0f; //time the player has been falling for
    protected float yRotationSinceLastUpdate = 0f;
    protected Vector3 lastForward = Vector3.forward;

    protected override void Start()
    {
        base.Start();
        lastForward = rb.transform.forward;
    }

    // Update is called once per frame
    protected void FixedUpdate()
    {
        /* Update flags/trackers */
        state.UpdatePlayerState();
        fallTime = state.IsFalling ? fallTime + Time.deltaTime : 0f;

        //update y rotation tracker
        yRotationSinceLastUpdate = Vector3.SignedAngle(lastForward, rb.transform.forward, Vector3.up);
        lastForward = rb.transform.forward;
        if(Mathf.Abs(yRotationSinceLastUpdate) > 0) Debug.Log("yRotationSpeed = " + yRotationSinceLastUpdate);

        /* Input */
        Vector3 moveInput = GetMovementInput();

        /* Horizontal movement */
        Vector3 moveForce = CalcMovement(moveInput, acceleration) + CalcGroundStopForce(moveInput);
        float slopeMult = CalcSlopeSpeedAdjustment(2f, moveInput, -15, 15, 45);
        Vector3 totalMoveForce = Vector3.ClampMagnitude(moveForce * slopeMult, MAX_ADDFORCE_MAGNITUDE);
        rb.AddForce(totalMoveForce, forceMode);

        /* Jump forces */
        if (state.IsJumping) rb.AddForce(Vector3.up * jumpForce, forceMode);
        if (state.IsFalling) rb.AddForce(Physics.gravity * extraFallSpeed, ForceMode.Impulse);

        /* Rotation */
        if (moveInput != Vector3.zero) lastNonZeroInput = moveInput;

        //Turn lean
        Vector3 turnLeanUp = Vector3.up + (rb.transform.right * yRotationSinceLastUpdate * 0.05f);
        Debug.DrawRay(transform.position, turnLeanUp * 10, Color.magenta);

        Quaternion look = Quaternion.LookRotation(rb.velocity.sqrMagnitude > 0 ? new Vector3(rb.velocity.x, 0f, rb.velocity.z) : lastNonZeroInput);
        look = Quaternion.LookRotation(lastNonZeroInput);
        //transform.rotation = look;
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 5f);
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lastNonZeroInput, turnLeanUp), 1);
    }

    //Perform direct player translation/velocity changes here, after physics step is done
    protected void Update()
    {
        /* Clamp speed */
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float flatSpeed = flatVel.magnitude;
        if (flatSpeed > maxGroundSpeed) rb.velocity = (flatVel.normalized * maxGroundSpeed) + (Vector3.up * rb.velocity.y);
        
        /* Stair step traversal */
        float stepUpHeight;
        if (TryGetStepUpHeight(out stepUpHeight))
        {
            rb.transform.position = Vector3.Lerp(rb.transform.position, new Vector3(rb.transform.position.x, rb.transform.position.y + stepUpHeight, rb.transform.position.z), 1f);
        }
    }

    //Calculates ground movement force by multiplying (input * speed) by the point on the force curve
    //given by dividing current speed by max speed. This should allow control over initial acceleration etc.
    //and will probably prevent the player from going faster than max speed if the force curve tapers towards zero as speed reaches max
    protected override Vector3 CalcGroundMvmt(Vector3 moveInput, float speed)
    {
        float t = rb.velocity.magnitude / maxGroundSpeed;
        if(useForceCurve)
        {
            return moveInput * speed * forceCurve.Evaluate(t);
        }
        else
        {
            return moveInput * speed;
        }
    }

    protected override Vector3 CalcAirMvmt(Vector3 moveInput, float speed)
    {
        float t = rb.velocity.magnitude / maxAirSpeed;
        if (useForceCurve)
        {
            return moveInput * speed * forceCurve.Evaluate(t) * airControlAmount;
        }
        else
        {
            return moveInput * speed * airControlAmount;
        }
    }

    //Calculates an opposing force to directions player is moving in but not inputting - 
    //intended to act as increased drag to stop player sooner (and be more controllable than Unity's rigidbody drag setting)
    protected Vector3 CalcGroundStopForce(Vector3 moveInput)
    {
        if (!state.IsOnGround) return Vector3.zero;

        Vector3 slowForce = Vector3.zero;
        if (moveInput.x == 0) slowForce.x = -(rb.velocity.x * slowAmount);
        if (moveInput.z == 0) slowForce.z = -(rb.velocity.z * slowAmount);
        return slowForce;
    }

    protected float GetMaxSpeedForCurrentState()
    {
        return state.IsOnGround ? maxGroundSpeed : maxAirSpeed;
    }

}
