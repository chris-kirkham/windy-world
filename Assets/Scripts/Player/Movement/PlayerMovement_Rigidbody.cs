using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class PlayerMovement_Rigidbody : PlayerMovement
{
    /* components */
    protected Rigidbody rb;

    /* land speed attributes */
    public float slowForce = 1f; //used to slow/stop character when not inputting a direction. Essentially manual drag for player movement 

    protected override void Start()
    {
        base.Start();

        /* components */
        rb = GetComponent<Rigidbody>();
    }

    protected override Vector3 CalcGroundMvmt(Vector3 moveInput)
    {
        return base.CalcGroundMvmt(moveInput) + XZDrag(moveInput, slowForce);
    }

    protected override Vector3 CalcAirMvmt(Vector3 moveInput)
    {
        return base.CalcAirMvmt(moveInput) + XZDrag(moveInput, slowForce);
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

    protected bool CalcIsFalling()
    {
        return !IsOnGround() && rb.velocity.y < 0;
    }

    //GETTERS
    public override Vector3 GetVelocity()
    {
        return rb.velocity;
    }
}
