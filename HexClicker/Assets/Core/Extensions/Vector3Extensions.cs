using UnityEngine;

public static class Vector3Extensions 
{
    public static Vector2 xz(this Vector3 vector) => new Vector2(vector.x, vector.z);

    public static Vector3 AddXZ(this Vector3 vector, float x, float z) => vector + new Vector3(x, 0, z);
    public static Vector3 AddXZ(this Vector3 vector, Vector2 xz) => vector + new Vector3(xz.x, 0, xz.y);
}
