using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WindNoise
{
    public static Vector3 GetDirectionalNoise3D(Vector3 pos)
    {
        float x = Mathf.PerlinNoise(pos.x, pos.y);
        float y = Mathf.PerlinNoise(pos.y, pos.z);
        float z = Mathf.PerlinNoise(pos.x, pos.z);
        return new Vector3(x, y, z);
    }
}
