
using UnityEngine;
using System;


public static class MathUtil
{
    public const float FLOAT_EPSILON = 0.00001f;

    
    public static float Normalize(ref Vector3 vec)
    {
        float magnitude = vec.magnitude;
        if (magnitude > 0.0f)
        {
            vec.x /= magnitude;
            vec.y /= magnitude;
            vec.z /= magnitude;
        }
        return magnitude;
    }
}
