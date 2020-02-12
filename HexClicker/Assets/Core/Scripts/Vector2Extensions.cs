using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2Extensions
{
    public static float Cross(this Vector2 vec, Vector2 other)
    {
        return (vec.x * other.y) - (vec.y * other.x);
    }

    public static float Dot(this Vector2 vec, Vector2 other)
    {
        return Vector2.Dot(vec, other);
    }
}
