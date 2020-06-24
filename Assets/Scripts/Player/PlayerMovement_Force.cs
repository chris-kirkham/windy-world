using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Variant of PlayerMovement which exclusively uses AddForce to control movement.
/// Should work better with the physics system but will be harder to tweak since we can't set the velocity directly
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement_Force : PlayerMovement
{
    public ForceMode forceMode = ForceMode.Force;

    /* Drag parameters */
    [Range(0, 1)] public float groundDrag = 1f;
    [Range(0, 1)] public float airDragXZ = 1f;
    [Range(0, 1)] public float airDragPlusY = 0f;
    [Range(0, 1)] public float airDragMinusY = 0f;

    private const float MAX_ADDFORCE_MAGNITUDE = 100f;

    // Update is called once per frame
    protected override void FixedUpdate()
    {
        //Debug.DrawLine(transform.position, transform.position + new Vector3(0f, STEP_UP_MAX_STEP_HEIGHT, 0f));
        //Debug.Log("bottomTranslate = " + bottomTranslate);

        /* Update flags/trackers */
        isOnGround = IsOnGround();

        /* Horizontal movement */
        Vector3 moveInput = GetMovementInput();
        Vector3 moveForce = CalcMovement(moveInput);
        Vector3 dragForce = Vector3.zero; 
        
        Vector3 totalMoveForce = Vector3.ClampMagnitude(moveForce + dragForce, MAX_ADDFORCE_MAGNITUDE);
        rb.AddForce(totalMoveForce, forceMode);

        /* Jump forces */
        rb.AddForce(Vector3.up * CalcJump(), ForceMode.Impulse);
        if (!isOnGround && rb.velocity.y < 0) rb.AddForce(Physics.gravity * extraFallSpeed, ForceMode.Force);

        float stepUpHeight;
        TryGetStepUpHeight(out stepUpHeight);

        /* Rotation */
        if (moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        transform.rotation = Quaternion.LookRotation(lastNonZeroInput, Vector3.up);
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
