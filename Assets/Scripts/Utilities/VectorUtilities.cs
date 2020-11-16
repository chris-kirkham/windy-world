using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorUtilities
{
    /* Vectors to array - useful for passing vectors to compute shaders (which take vectors as arrays) */
    
    public static float[] ToArray(this Vector2 vector)
    {
        return new float[2] { vector.x, vector.y }; 
    }

    public static float[] ToArray(this Vector3 vector)
    {
        return new float[3] { vector.x, vector.y, vector.z };
    }

    public static float[] ToArray(this Vector4 vector)
    {
        return new float[4] { vector.x, vector.y, vector.z, vector.w };
    }

    public static int[] ToArray(this Vector2Int vector)
    {
        return new int[2] { vector.x, vector.y };
    }

    public static int[] ToArray(this Vector3Int vector)
    {
        return new int[3] { vector.x, vector.y, vector.z };
    }
}
