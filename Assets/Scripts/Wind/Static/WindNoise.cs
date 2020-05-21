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
    
    public static Vector3 Directional2D(Vector2 pos, float frequency, float offset = 0)
    {
        Vector3 coord = new Vector2((pos.x + offset) * frequency , (pos.y + offset) * frequency + offset);
        return Perlin.PointOnUnitCircle(coord);
    }

    public static Vector3 Directional3D(Vector3 pos, float frequency, float offset = 0)
    {
        Vector3 coord = new Vector3((pos.x + offset) * frequency, (pos.y + offset) * frequency, (pos.z + offset) * frequency);
        return Perlin.PointOnUnitSphere(coord);
    }
}
