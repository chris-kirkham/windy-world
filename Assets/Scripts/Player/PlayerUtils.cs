using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides utility functions pertaining to the player.
/// </summary>
public static class PlayerUtils
{
    private const float GROUND_RAYCAST_MAX_DISTANCE = 0.3f;

    //Performs raycasts towards the ground; returns true if any raycasts hit, and logs the average of the hit normal(s) in avgNormal (Vector3.zero if no hit)
    public static bool GroundRaycasts(Vector3 playerFloorPos, out Vector3 avgNormal, LayerMask groundLayer)
    {
        avgNormal = Vector3.zero;
        int successfulRaycasts = 0;

        float raycastOffset = 0.2f;
        playerFloorPos = playerFloorPos + (Vector3.up * 0.2f);
        RaycastHit hit;
        //centre
        if (Physics.Raycast(playerFloorPos, Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, groundLayer))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //forward
        if (Physics.Raycast(playerFloorPos + (Vector3.forward * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, groundLayer))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //back
        if (Physics.Raycast(playerFloorPos + (Vector3.back * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, groundLayer))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //left
        if (Physics.Raycast(playerFloorPos + (Vector3.left * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, groundLayer))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        //right
        if (Physics.Raycast(playerFloorPos + (Vector3.right * raycastOffset), Vector3.down, out hit, GROUND_RAYCAST_MAX_DISTANCE, groundLayer))
        {
            successfulRaycasts++;
            avgNormal += hit.normal;
        }

        if (successfulRaycasts > 1) avgNormal /= successfulRaycasts;

        return successfulRaycasts > 0;
    }
}
