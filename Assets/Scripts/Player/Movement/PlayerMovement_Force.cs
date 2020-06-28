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
    public ForceMode forceMode = ForceMode.Force;

    /* Drag parameters */
    [Range(0, 1)] public float groundDrag = 1f;
    [Range(0, 1)] public float airDragXZ = 1f;
    [Range(0, 1)] public float airDragPlusY = 0f;
    [Range(0, 1)] public float airDragMinusY = 0f;

    /* Status flags */
    protected bool isFalling = false;

    /* Trackers */
    protected float fallTime = 0f; //time the player has been falling for

    private const float MAX_ADDFORCE_MAGNITUDE = 100f;

    // Update is called once per frame
    protected void FixedUpdate()
    {
        /* Update flags/trackers */
        isOnGround = CalcIsOnGround();
        isFalling = CalcIsFalling();
        fallTime = isFalling ? fallTime + Time.deltaTime : 0f;

        /* Horizontal movement */
        Vector3 moveInput = GetMovementInput();
        Vector3 moveForce = CalcMovement(moveInput);
        float slopeMult = CalcSlopeSpeedAdjustment(2f, moveInput, -15, 15, 45);
        Vector3 totalMoveForce = Vector3.ClampMagnitude(moveForce * slopeMult, MAX_ADDFORCE_MAGNITUDE);
        rb.AddForce(totalMoveForce, forceMode);

        /* Jump forces */
        rb.AddForce(Vector3.up * CalcJump(), forceMode);
        if (isFalling) rb.AddForce(Physics.gravity * extraFallSpeed * Mathf.Pow(fallTime, 2), ForceMode.Impulse);

        /* Stair step traversal */
        float stepUpHeight;
        if(TryGetStepUpHeight(out stepUpHeight))
        {
            rb.transform.position = Vector3.Lerp(rb.transform.position, new Vector3(rb.transform.position.x, rb.transform.position.y + stepUpHeight, rb.transform.position.z), 1f);
        }

        /* Rotation */
        if (moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        transform.rotation = Quaternion.LookRotation(lastNonZeroInput, Vector3.up);

        Debug.Log("speed = " + rb.velocity.magnitude); 
    }

    //Returns the appropriate drag vector for the player's current state
    private Vector3 CalcDrag()
    {
        if (isOnGround)
        {
            return GetDrag(groundDrag);
        }
        else
        {
            return rb.velocity.y > 0 ? GetDrag(airDragPlusY) : GetDrag(airDragMinusY);
        }
    }

    //Calculates a drag vector based on the rigidbody's velocity and a given drag factor (not physically accurate but works for gameplay)
    private Vector3 GetDrag(float dragAmt)
    {
        return -(rb.velocity * rb.velocity.magnitude * dragAmt);
    }

}
