using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement_Velocity : PlayerMovement_Rigidbody
{
    protected void FixedUpdate()
    {
        /* Update flags/trackers */
        isOnGround = CalcIsOnGround();

        /* Horizontal movement */
        Vector3 moveInput = GetMovementInput();
        Vector3 moveForce = CalcMovement(moveInput);

        /* Jump/fall */
        Vector3 jump = Vector3.up * CalcJump();
        Vector3 fall = Vector3.zero;
        if (!isOnGround && rb.velocity.y < 0) fall = Vector3.up * Physics.gravity.y * extraFallSpeed;

        /* Change velocity */
        rb.velocity = moveForce + fall; // + jump + fall

        /* Adjust player y for stair step traversal */
        float stepUpHeight;
        if (TryGetStepUpHeight(out stepUpHeight))
        {
            rb.transform.position = Vector3.Lerp(rb.transform.position, new Vector3(rb.transform.position.x, rb.transform.position.y + stepUpHeight, rb.transform.position.z), 0.5f);
        }

        /* Rotation */
        if (moveInput != Vector3.zero) lastNonZeroInput = moveInput;
        transform.rotation = Quaternion.LookRotation(lastNonZeroInput, Vector3.up);
    }
}
