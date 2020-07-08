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
    [SerializeField] private bool useForceCurve = true;
    [SerializeField] private AnimationCurve forceCurve = new AnimationCurve();
    [SerializeField] private ForceMode forceMode = ForceMode.Force;
    [SerializeField] private float slowAmount = 1f; //used to slow/stop character when not inputting a direction. Essentially manual drag for player movement 

    /* Status flags */

    /* Trackers */
    protected float fallTime = 0f; //time the player has been falling for

    private const float MAX_ADDFORCE_MAGNITUDE = 100f;

    // Update is called once per frame
    protected void FixedUpdate()
    {
        /* Update flags/trackers */
        state.UpdatePlayerState();
        fallTime = state.IsFalling ? fallTime + Time.deltaTime : 0f;

        /* Input */
        Vector3 moveInput = GetMovementInput();

        /* Horizontal movement */
        Vector3 moveForce = CalcMovement(moveInput, acceleration) + CalcGroundStopForce(moveInput);
        float slopeMult = CalcSlopeSpeedAdjustment(2f, moveInput, -15, 15, 45);
        Vector3 totalMoveForce = Vector3.ClampMagnitude(moveForce * slopeMult, MAX_ADDFORCE_MAGNITUDE);
        rb.AddForce(totalMoveForce, forceMode);

        /* Jump forces */
        if(state.IsJumping) rb.AddForce(Vector3.up * jumpForce, forceMode);
        if (state.IsFalling) rb.AddForce(Physics.gravity * extraFallSpeed * Mathf.Pow(fallTime, 2), ForceMode.Impulse);

        /* Rotation */
        if (moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        //transform.rotation = Quaternion.LookRotation(lastNonZeroInput, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lastNonZeroInput, Vector3.up), 5f * Time.deltaTime);
    }

    protected void Update()
    {
        /* Clamp speed */
        float maxSpeed = GetMaxSpeedForCurrentState();
        //if (rb.velocity.magnitude > maxSpeed) rb.velocity = rb.velocity.normalized * maxSpeed;

        /* Stair step traversal */
        float stepUpHeight;
        if (TryGetStepUpHeight(out stepUpHeight))
        {
            rb.transform.position = Vector3.Lerp(rb.transform.position, new Vector3(rb.transform.position.x, rb.transform.position.y + stepUpHeight, rb.transform.position.z), 1f);
        }
    }

    protected override Vector3 CalcGroundMvmt(Vector3 moveInput, float speed)
    {
        float t = rb.velocity.magnitude / maxGroundSpeed;
        Debug.Log("t = " + t);
        return moveInput * speed * forceCurve.Evaluate(t);
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
