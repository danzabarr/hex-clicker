using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XZPlane : MonoBehaviour
{
    private static Plane XZ0Plane = new Plane(Vector3.up, Vector3.zero);

    public static Vector3 RayPlaneIntersection(Ray ray, Plane plane)
    {

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 hit = ray.GetPoint(distance);
            return hit;
        }
        throw new System.Exception();
    }

    public static Vector3 ScreenPointPlaneIntersection(Camera camera, Vector3 screenPoint, Plane plane)
    {
        return RayPlaneIntersection(camera.ScreenPointToRay(screenPoint), plane);
    }

    public static Vector3 ScreenPointXZPlaneIntersection(Camera camera, Vector3 screenPoint, float y)
    {
        Plane plane = new Plane(Vector3.up, new Vector3(0, y, 0));
        Vector3 hit = ScreenPointPlaneIntersection(camera, screenPoint, plane);
        hit.y = y;

        return hit;
    }

    public static Vector3 ScreenPointXZ0PlaneIntersection(Camera camera, Vector3 screenPoint)
    {
        Vector3 hit = ScreenPointPlaneIntersection(camera, screenPoint, XZ0Plane);
        hit.y = 0;
        return hit;
    }

    public static Vector3[] ScreenCornersXZ0PlaneIntersection(Camera camera)
    {
        return new Vector3[] {

            ScreenPointXZ0PlaneIntersection(camera, new Vector3(0, 0)),
            ScreenPointXZ0PlaneIntersection(camera, new Vector3(0, Screen.height)),
            ScreenPointXZ0PlaneIntersection(camera, new Vector3(Screen.width, Screen.height)),
            ScreenPointXZ0PlaneIntersection(camera, new Vector3(Screen.width, 0)),
        };
    }
}
