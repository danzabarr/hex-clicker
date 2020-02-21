using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XZPlane
{
    private static Plane XZ0Plane = new Plane(Vector3.up, Vector3.zero);

    public static bool RayPlaneIntersection(Ray ray, Plane plane, out Vector3 intersection)
    {
        intersection = default;
        if (plane.Raycast(ray, out float distance))
        {
            intersection = ray.GetPoint(distance);
            return true;
        }
        return false;
    }

    public static bool ScreenPointPlaneIntersection(Camera camera, Vector3 screenPoint, Plane plane, out Vector3 intersection)
    {
        return RayPlaneIntersection(camera.ScreenPointToRay(screenPoint), plane, out intersection);
    }

    public static bool ScreenPointXZPlaneIntersection(Camera camera, Vector3 screenPoint, float y, out Vector3 intersection)
    {
        Plane plane = new Plane(Vector3.up, new Vector3(0, y, 0));
        if (ScreenPointPlaneIntersection(camera, screenPoint, plane, out intersection))
        {
            intersection.y = y;
            return true;
        }

        return false;
    }

    public static bool ScreenPointXZ0PlaneIntersection(Camera camera, Vector3 screenPoint, out Vector3 intersection)
    {
        if (ScreenPointPlaneIntersection(camera, screenPoint, XZ0Plane, out intersection))
        {
            intersection.y = 0;
            return true;
        }
        return false;
    }

    public static Vector3[] ScreenCornersXZ0PlaneIntersection(Camera camera)
    {
        Vector3[] corners = new Vector3[4];

        ScreenPointXZ0PlaneIntersection(camera, new Vector3(0, 0), out corners[0]);
        ScreenPointXZ0PlaneIntersection(camera, new Vector3(0, Screen.height), out corners[1]);
        ScreenPointXZ0PlaneIntersection(camera, new Vector3(Screen.width, Screen.height), out corners[2]);
        ScreenPointXZ0PlaneIntersection(camera, new Vector3(Screen.width, 0), out corners[3]);

        return corners;
    }
}
