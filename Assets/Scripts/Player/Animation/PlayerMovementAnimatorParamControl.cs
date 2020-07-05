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

    // Update animator params
    void Update()
    {
        bool isOnGround = state.IsOnGround;
        animator.SetFloat("ForwardSpeed", state.HorizontalSpeed);
        animator.SetBool("Grounded", isOnGround);
        float yVel = movement.GetVelocity().y;
        if (!isOnGround) animator.SetFloat("AirborneVerticalSpeed", yVel);
        animator.SetFloat("VerticalSpeed", yVel);
        animator.SetBool("Jumping", state.IsJumping);
    }
}
