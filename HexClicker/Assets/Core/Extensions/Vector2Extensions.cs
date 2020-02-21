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

    public static Vector3 x0z(this Vector2 vector) => new Vector3(vector.x, 0, vector.y);
    public static Vector3 xyz(this Vector2 vector, float y) => new Vector3(vector.x, y, vector.y);
}
