using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtraGizmos 
{
    public static void DrawArrowHead(Vector3 start, Vector3 end, float headLength, float headAngle)
    {
        Gizmos.DrawLine(end, end + Quaternion.LookRotation(end - start, Vector3.up) * Quaternion.Euler(0, -headAngle, 0) * Vector3.forward * -headLength);
        Gizmos.DrawLine(end, end + Quaternion.LookRotation(end - start, Vector3.up) * Quaternion.Euler(0, +headAngle, 0) * Vector3.forward * -headLength);
    }
}
