using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Lerps 
{
    /* FLOAT */
    public static float Smoothstep(float a, float b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return a + ((b - a) * t);
    }
    
    public static float Smootherstep(float a, float b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * t * (t * (6f * t - 15f) + 10f);
        return a + ((b - a) * t);
    }

    /* DOUBLE */
    public static double Smoothstep(double a, double b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return a + ((b - a) * t);
    }

    public static double Smootherstep(double a, double b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * t * (t * (6f * t - 15f) + 10f);
        return a + ((b - a) * t);
    }

    /* VECTOR3 */
    //Smoothstep and Smootherstep from https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
    public static Vector3 Smoothstep(Vector3 a, Vector3 b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return a + ((b - a) * t);
    }

    public static Vector3 Smootherstep(Vector3 a, Vector3 b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * t * (t * (6f * t - 15f) + 10f);
        return a + ((b - a) * t);
    }

    /* QUATERNION */
    public static Quaternion Smoothstep(Quaternion a, Quaternion b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return Quaternion.Lerp(a, b, t);
    }

    public static Quaternion Smootherstep(Quaternion a, Quaternion b, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * t * (t * (6f * t - 15f) + 10f);
        return Quaternion.Lerp(a, b, t);
    }
}
