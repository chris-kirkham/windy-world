using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement_Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementAnimatorParamControl : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement mvmt;
    void Start()
    {
        animator = GetComponent<Animator>();
        mvmt = GetComponent<PlayerMovement>();
    }

    // Update animator params
    void Update()
    {
        Vector3 vel = mvmt.GetVelocity();
        bool isOnGround = mvmt.IsOnGround();
        animator.SetFloat("ForwardSpeed", new Vector3(vel.x, 0f, vel.z).magnitude);
        animator.SetBool("Grounded", isOnGround);
        if (!isOnGround) animator.SetFloat("AirborneVerticalSpeed", vel.y);
        animator.SetFloat("VerticalSpeed", vel.y);
        animator.SetBool("Jumping", mvmt.IsJumping());
    }
}
