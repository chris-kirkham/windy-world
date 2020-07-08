using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementAnimatorParamControl : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement movement;
    private PlayerState state;

    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        state = GetComponent<PlayerState>();
    }

    //Update animator params
    void Update()
    {
        //floats
        animator.SetFloat("ForwardSpeed", state.HorizontalSpeed);
        float yVel = movement.GetVelocity().y;
        animator.SetFloat("VerticalSpeed", yVel);
        if (!state.IsOnGround) animator.SetFloat("AirborneVerticalSpeed", yVel);

        //bools
        animator.SetBool("Grounded", state.IsOnGround);
        animator.SetBool("Jumping", state.IsJumping);
        animator.SetBool("Falling", state.IsFalling);
        animator.SetBool("QuickTurning", state.IsQuickTurning);
        animator.SetBool("StoppingFromWalkRun", state.IsStopping);

        //triggers
        if (state.IsQuickTurningFromIdle) animator.SetTrigger("QuickTurningFromIdle");
    }
}
