using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class PlayerMovement_Rigidbody : PlayerMovement
{
    protected Rigidbody rb;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
    }

    public override Vector3 GetVelocity()
    {
        return rb.velocity;
    }
}
