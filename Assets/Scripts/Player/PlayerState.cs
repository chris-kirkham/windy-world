using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks different player states during gameplay
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerState : MonoBehaviour
{
    /* constants */
    private const float MAX_WALKABLE_ANGLE = 45f; //maximum angle (in degrees) for ground that the player can walk on
    private const float QUICK_TURN_DOT_THRESHOLD = -0.5f; //maximum dot product of normalised input and velocity vectors to be considered a "quick turn"

    /* components */
    [SerializeField] private GameObject playerFloor;
    private PlayerMovement movement;
    private PlayerInput input;

    /* states */
    public bool IsOnGround { get; private set; } = false;
    public bool IsJumping { get; private set; } = false;
    public bool IsFalling { get; private set; } = false;
    public bool IsStopping { get; private set; } = false;
    public bool IsQuickTurning { get; private set; } = false;
    public bool IsQuickTurningFromIdle { get; private set; } = false;

    /* other info */
    public float Speed { get; private set; } = 0f;
    public float HorizontalSpeed { get; private set; } = 0f; //speed of player discounting y axis velocity

    /* internal trackers */
    float currJumpHoldTime = 0f;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInput>();
    }

    public void UpdatePlayerState()
    {
        SetIsOnGround();
        SetIsJumping();
        SetIsFalling();
        SetIsStopping();
        SetIsQuickTurning();
        SetIsQuickTurningFromIdle();
        SetSpeed();
        SetHorizontalSpeed();
    }

    private void SetIsOnGround()
    {
        if (PlayerUtils.GroundRaycasts(playerFloor.transform.position, out Vector3 avgNormal, LayerMask.GetMask("LevelGeometrySolid")))
        {
            float avgGroundAngle = Vector3.Angle(avgNormal, Vector3.up);
            IsOnGround = avgGroundAngle < MAX_WALKABLE_ANGLE;
        }
        else
        {
            IsOnGround = false;
        }
    }

    private void SetIsJumping()
    {
        if (input.GetJump())
        {
            if ((IsOnGround || IsJumping) && currJumpHoldTime < 0.1f)
            {
                currJumpHoldTime += Time.deltaTime;
                IsJumping = true;
            }
            else
            {
                IsJumping = false;
            }
        }
        else
        {
            currJumpHoldTime = 0f;
            IsJumping = false;
        }
    }

    private void SetIsFalling()
    {
        IsFalling = !IsOnGround && movement.GetVelocity().y < 0;
    }

    private void SetIsStopping()
    {
        IsStopping = movement.GetVelocity().sqrMagnitude > 0 && input.GetHVAxis().magnitude == 0;
    }

    private void SetIsQuickTurning()
    {
        IsQuickTurning = Vector3.Dot(movement.GetMovementInput().normalized, movement.GetVelocity().normalized) < QUICK_TURN_DOT_THRESHOLD;
    }

    private void SetIsQuickTurningFromIdle()
    {
        IsQuickTurningFromIdle = movement.GetVelocity().sqrMagnitude < 0.01f
                                 && Vector3.Dot(movement.GetMovementInput().normalized, movement.transform.forward) < QUICK_TURN_DOT_THRESHOLD;
    }

    private void SetSpeed()
    {
        Speed = movement.GetVelocity().magnitude;
    }

    private void SetHorizontalSpeed()
    {
        Vector3 vel = movement.GetVelocity();
        HorizontalSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
    }

}
