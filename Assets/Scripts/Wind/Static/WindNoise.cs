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
        //Debug.Log("noise = " + new Vector3(x, y, z) * 1000);
        return new Vector3(x, y, z);
    }
    

    public static Vector3 Directional2D(Vector2 pos, float frequency, float offset = 0)
    {
        Vector3 coord = new Vector2((pos.x * frequency) + offset, (pos.y * frequency) + offset);
        return Perlin.PointOnUnitCircle(coord);
    }

    public static Vector3 Directional3D(Vector3 pos, float frequency, float offset = 0)
    {
        Vector3 coord = new Vector3((pos.x * frequency) + offset, (pos.y * frequency) + offset, (pos.z * frequency) + offset);
        //Debug.Log("noise = " + Perlin.PointOnUnitSphere(coord) * 1000);
        return Perlin.PointOnUnitSphere(coord);
    }
}
