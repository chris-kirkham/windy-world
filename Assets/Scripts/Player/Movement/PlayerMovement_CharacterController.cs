using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement_CharacterController : PlayerMovement
{
    CharacterController cc;

    protected override void Start()
    {
        base.Start();

        /* components */
        cc = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        cc.Move(CalcMovement(GetMovementInput()) * Time.deltaTime);
    }

    //GETTERS
    public override Vector3 GetVelocity()
    {
        return cc.velocity;
    }
}
