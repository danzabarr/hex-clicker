using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public struct Polygon2 : IEnumerable<Vector2>
    {
        private Vector2[] points;

        public Polygon2(Vector2[] points)
        {
            this.points = points;
            Bounds = CalculateBounds(points);
        }
        public Vector2 this[int i] => points[i];
        public int Length => points.Length;
        public IEnumerator<Vector2> GetEnumerator() => ((IEnumerable<Vector2>)points).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => points.GetEnumerator();
        public Bounds2 Bounds { get; private set; }

        public void ApplyTransform(Transform transform, bool useXZ)
        {
            points = TransformedPoints(points, transform, useXZ);
            Bounds = CalculateBounds(points);
        }

        public static void DrawPolygon(Vector2[] poly, bool useXZ)
        {
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 p0 = poly[i];
                Vector2 p1 = poly[(i + 1) % poly.Length];
                if (useXZ)
                    Gizmos.DrawLine(p0.x0z(), p1.x0z());
                else
                    Gizmos.DrawLine(p0, p1);
            }
        }

        public static void DrawPolygon(Vector3[] poly)
        {
            for (int i = 0; i < poly.Length; i++)
            {
                Vector3 p0 = poly[i];
                Vector3 p1 = poly[(i + 1) % poly.Length];
                Gizmos.DrawLine(p0, p1);
            }
        }

        public static void DrawPolygon(Polygon2 poly, bool useXZ) => DrawPolygon(poly.points, useXZ);

        public static Vector2[] TransformedPoints(Vector2[] points, Transform transform, bool useXZ)
        {
            Vector2[] p = new Vector2[points.Length];
            for (int i = 0; i < p.Length; i++)
            {
                if (useXZ)
                    p[i] = transform.TransformPoint(points[i].x0z()).xz();
                else
                    p[i] = transform.TransformPoint(points[i]);
            }
            return p;
        }

        public static Vector3[] TransformedPoints3(Vector2[] points, Transform transform, bool useXZ)
        {
            Vector3[] p = new Vector3[points.Length];
            for (int i = 0; i < p.Length; i++)
            {
                if (useXZ)
                    p[i] = transform.TransformPoint(points[i].x0z());
                else
                    p[i] = transform.TransformPoint(points[i]);
            }
            return p;
        }

        public static Vector2[] InverseTransformedPoints(Vector2[] points, Transform transform, bool useXZ)
        {

            Vector2[] p = new Vector2[points.Length];
            for (int i = 0; i < p.Length; i++)
            {
                if (useXZ)
                    p[i] = transform.InverseTransformPoint(points[i].x0z()).xz();
                else
                    p[i] = transform.InverseTransformPoint(points[i]);
            }
            return p;
        }

        public static Bounds2 CalculateBounds(Vector2[] points)
        {
            if (points == null)
                return default;

            if (points.Length == 1)
                return new Bounds2(points[0].x, points[0].y);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (Vector2 p in points)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            return new Bounds2
            {
                minX = minX,
                maxX = maxX,
                minY = minY,
                maxY = maxY
            };
        }

        public static List<float> ScanRayIntersections(Vector2[] points, Vector2 p, bool sort)
        {
            List<float> intersections = new List<float>();

            Vector2 rayDirection = new Vector2(1, 0);

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p0 = points[i];
                Vector2 p1 = points[(i + 1) % points.Length];

                if (p0.y > p.y && p1.y > p.y)
                    continue;

                if (p0.y <= p.y && p1.y <= p.y)
                    continue;

                float t1 = (p1.x - p0.x) * (p.y - p0.y) / (p1.y - p0.y) - (p.x - p0.x);
                float t2 = (p.y - p0.y) / (p1.y - p0.y);

                if (t1 >= 0.0 && (t2 >= 0.0 && t2 <= 1.0))
                {
                    intersections.Add(p.x + t1);
                }
            }

            if (sort)
                intersections.Sort((a, b) => a.CompareTo(b));

            return intersections;
        }

        public static List<float> ScanLineIntersections(Vector2[] points, float y, bool sort)
        {
            List<float> intersections = new List<float>();

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p0 = points[i];
                Vector2 p1 = points[(i + 1) % points.Length];

                if (p0.y > y && p1.y > y)
                    continue;

                if (p0.y <= y && p1.y <= y)
                    continue;

                float t2 = (y - p0.y) / (p1.y - p0.y);
                if ((t2 >= 0.0 && t2 <= 1.0))
                {
                    float x = (p1.x - p0.x) * (y - p0.y) / (p1.y - p0.y) + p0.x;
                    intersections.Add(x);
                }
            }

            if (sort)
                intersections.Sort((a, b) => a.CompareTo(b));

            return intersections;
        }

        public static bool ScanLineIntersection(Vector2 p0, Vector2 p1, out float x, float y)
        {
            if (p0.y > y && p1.y > y)
            {
                x = 0;
                return false;
            }

            if (p0.y <= y && p1.y <= y)
            {
                x = 0;
                return false;
            }

            float t2 = (y - p0.y) / (p1.y - p0.y);
            if ((t2 >= 0.0 && t2 <= 1.0))
            {
                x = (p1.x - p0.x) * (y - p0.y) / (p1.y - p0.y) + p0.x;
                return true;
            }
            x = 0;
            return false;
        }

        public static bool PolygonContainsPoint(Vector2[] poly, Vector2 point)
        {
            List<float> intersections = ScanRayIntersections(poly, point, false);
            return intersections.Count % 2 == 0;
        }

        public static bool LineRayIntersection(Vector2 rayOrigin, Vector2 rayDirection, Vector2 point1, Vector2 point2, out Vector2 intersection, out float sqDistance)
        {
            Vector2 v1 = rayOrigin - point1;
            Vector2 v2 = point2 - point1;
            Vector2 v3 = new Vector2(-rayDirection.y, rayDirection.x);

            float dot = v2.Dot(v3);
            if (Mathf.Abs(dot) < 0.0000001f)
            {
                intersection = Vector2.zero;
                sqDistance = -1;
                return false;
            }

            float t1 = v2.Cross(v1) / dot;
            float t2 = v1.Dot(v3) / dot;

            if (t1 >= 0.0 && (t2 >= 0.0 && t2 <= 1.0))
            {
                intersection = rayOrigin + rayDirection * t1;
                sqDistance = t1;
                return true;
            }

            intersection = Vector2.zero;
            sqDistance = -1;
            return false;
        }

        public static bool LineSegmentIntersection(Vector2 l1Start, Vector2 l1End, Vector2 l2Start, Vector2 l2End, out Vector2 intersection)
        {
            intersection = Vector3.zero;
            float deltaACy = l1Start.y - l2Start.y;
            float deltaDCx = l2End.x - l2Start.x;
            float deltaACx = l1Start.x - l2Start.x;
            float deltaDCy = l2End.y - l2Start.y;
            float deltaBAx = l1End.x - l1Start.x;
            float deltaBAy = l1End.y - l1Start.y;

            float denominator = deltaBAx * deltaDCy - deltaBAy * deltaDCx;
            float numerator = deltaACy * deltaDCx - deltaACx * deltaDCy;

            if (denominator == 0)
            {
                //return false;
                if (numerator == 0)
                {
                    // collinear. Potentially infinite intersection points.
                    // Check and return one of them.
                    if (l1Start.x >= l2Start.x && l1Start.x <= l2End.x)
                    {
                        intersection = l1Start;
                        return true;
                    }
                    else if (l2Start.x >= l1Start.x && l2Start.x <= l1End.x)
                    {
                        intersection = l2Start;
                        return true;
                    }
                    else
                    {
                        //    return false;
                    }
                }
                else
                { // parallel
                    return false;
                }
            }

            float r = numerator / denominator;
            if (r <= 0 || r >= 1)
            {
                return false;
            }

            float s = (deltaACy * deltaBAx - deltaACx * deltaBAy) / denominator;
            if (s <= 0 || s >= 1)
            {
                return false;
            }

            intersection = new Vector2((float)(l1Start.x + r * deltaBAx), (float)(l1Start.y + r * deltaBAy));
            return true;
        }

        public static List<Vector2Int> VoxelTraverse(Vector2 p0, Vector2 p1, Vector2 voxelSize, Vector2 voxelOffset)
        {
            List<Vector2Int> line = new List<Vector2Int>();
            float Step(float x, float y) => y > x ? 1 : 0;
            Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

            voxelOffset -= new Vector2(.5f, .5f);

            p0 /= voxelSize;
            p1 /= voxelSize;

            p0 -= voxelOffset;
            p1 -= voxelOffset;

            Vector2 rd = p1 - p0;
            Vector2 p = new Vector2(Mathf.Floor(p0.x), Mathf.Floor(p0.y));
            Vector2 rdinv = Vector2.one / rd;
            Vector2 stp = new Vector2(Mathf.Sign(rd.x), Mathf.Sign(rd.y));
            Vector2 delta = Vector2.Min(rdinv * stp, Vector2.one);
            Vector2 t_max = Vector2Abs((p + Vector2.Max(stp, Vector2.zero) - p0) * rdinv);
            int i = 0;
            while (i < 1000)
            {
                i++;
                Vector2Int square = Vector2Int.RoundToInt(p);
                line.Add(square);

                float next_t = Mathf.Min(t_max.x, t_max.y);
                if (next_t > 1.0) break;
                //Vector2 intersection = p0 + next_t * rd;

                Vector2 cmp = new Vector2(Step(t_max.x, t_max.y), Step(t_max.y, t_max.x));
                t_max += delta * cmp;
                p += stp * cmp;
            }

            return line;
        }

        public static List<Vector2Int> VoxelTraverseOutline(Vector2[] poly, Vector2 voxelSize, Vector2 voxelOffset, bool removeDuplicates)
        {
            List<Vector2Int> outline = new List<Vector2Int>();
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 p0 = poly[i];
                Vector2 p1 = poly[(i + 1) % poly.Length];
                if (removeDuplicates)
                {
                    foreach (Vector2Int p in VoxelTraverse(p0, p1, voxelSize, voxelOffset))
                        if (!outline.Contains(p)) outline.Add(p);
                }
                else
                {
                    outline.AddRange(VoxelTraverse(p0, p1, voxelSize, voxelOffset));
                }
            }
            return outline;
        }

        public static List<Vector2Int> ScanLineFill(Vector2[] poly, Vector2 voxelSize, Vector2 voxelOffset) => ScanLineFill(poly, CalculateBounds(poly), voxelSize, voxelOffset);

        public static List<Vector2Int> ScanLineFill(Vector2[] poly, Bounds2 bounds, Vector2 voxelSize, Vector2 voxelOffset)
        {
            int xMin = Mathf.RoundToInt((bounds.minX) / voxelSize.x - voxelOffset.x);
            int yMin = Mathf.RoundToInt((bounds.minY) / voxelSize.y - voxelOffset.y);

            int xMax = Mathf.RoundToInt((bounds.maxX) / voxelSize.x - voxelOffset.x);
            int yMax = Mathf.RoundToInt((bounds.maxY) / voxelSize.y - voxelOffset.y);

            List<Vector2Int> fill = new List<Vector2Int>();

            for (int y = yMin; y <= yMax; y++)
            {
                float voxelY = (y + voxelOffset.y) * voxelSize.y;

                List<float> intersections = ScanLineIntersections(poly, voxelY, true);

                int i = 0;
                bool filling = false;
                for (int x = xMin; x <= xMax; x++)
                {
                    float voxelX = (x + voxelOffset.x) * voxelSize.x;

                    for (; i < intersections.Count; i++)
                    {
                        if (voxelX >= intersections[i])
                            filling = !filling;
                        else
                            break;
                    }

                    if (filling)
                        fill.Add(new Vector2Int(x, y));
                }
            }

            return fill;
        }
        public static List<Vector2Int> ScanLineFill(Polygon2 poly, Vector2 voxelSize, Vector2 voxelOffset) => ScanLineFill(poly.points, poly.Bounds, voxelSize, voxelOffset);

        public static Vector2 NearestPointOnLineSegment(Vector2 lineStart, Vector2 lineEnd, Vector2 target, out float sqDistance)
        {
            Vector2 v = lineEnd - lineStart;
            Vector2 u = lineStart - target;
            float vu = v.x * u.x + v.y * u.y;
            float vv = v.x * v.x + v.y * v.y;
            float t = -vu / vv;

            if (t >= 0 && t <= 1)
            {
                Vector2 p = VectorToSegment2D(t, Vector2.zero, lineStart, lineEnd);
                sqDistance = SqLength(p - target);
                return p;
            }

            float g0 = SqLength(VectorToSegment2D(0, target, lineStart, lineEnd));
            float g1 = SqLength(VectorToSegment2D(1, target, lineStart, lineEnd));

            if (g0 <= g1)
            {
                sqDistance = g0;
                return lineStart;
            }
            else
            {
                sqDistance = g1;
                return lineEnd;
            }

            Vector2 VectorToSegment2D(float T, Vector2 P, Vector2 A, Vector2 B) => new Vector2(
                (1 - T) * A.x + T * B.x - P.x,
                (1 - T) * A.y + T * B.y - P.y
            );
        }

        public static Vector2 NearestPointOnPolyEdge(Vector2[] poly, Vector2 target, out float sqDistance)
        {
            Vector2 nearestPoint = target;
            sqDistance = float.MaxValue;

            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 p0 = poly[i];
                Vector2 p1 = poly[(i + 1) % poly.Length];

                Vector2 n = NearestPointOnLineSegment(p0, p1, target, out float d);

                if (d < sqDistance)
                {
                    nearestPoint = n;
                    sqDistance = d;
                }
            }

            return nearestPoint;
        }

        public static Vector2 NearestPointOnPolyEdge(Polygon2 poly, Vector2 target, out float sqDistance) => NearestPointOnPolyEdge(poly.points, target, out sqDistance);

        public static bool Raycast(Vector2[] poly, Vector2 rayOrigin, Vector2 rayDirection, out Vector2 intersection, out float sqDistance)
        {
            intersection = default;
            sqDistance = float.MaxValue;

            if (RaycastAll(poly, rayOrigin, rayDirection, out Vector2[] intersections, out float[] sqDistances))
            {
                intersection = intersections[0];
                sqDistance = sqDistances[0];
                return true;
            }
            return false;
        }

        public static bool RaycastAny(Vector2[] poly, Vector2 rayOrigin, Vector2 rayDirection, out Vector2 intersection, out float sqDistance)
        {
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 p0 = poly[i];
                Vector2 p1 = poly[(i + 1) % poly.Length];
                if (LineRayIntersection(rayOrigin, rayDirection, p0, p1, out intersection, out sqDistance))
                {
                    return true;
                }
            }
            intersection = default;
            sqDistance = float.MaxValue;
            return false;
        }

        public static bool RaycastAll(Vector2[] poly, Vector2 rayOrigin, Vector2 rayDirection, out Vector2[] intersections, out float[] sqDistances)
        {
            List<Vector2> list = new List<Vector2>();

            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 p0 = poly[i];
                Vector2 p1 = poly[(i + 1) % poly.Length];
                if (LineRayIntersection(rayOrigin, rayDirection, p0, p1, out Vector2 inter, out float dist))
                {
                    list.Add(inter);
                }
            }

            list.Sort((Vector2 a, Vector2 b) => SqLength(a - rayOrigin).CompareTo(SqLength(b - rayOrigin)));

            intersections = list.ToArray();
            sqDistances = new float[intersections.Length];
            for (int i = 0; i < intersections.Length; i++)
                sqDistances[i] = SqLength(intersections[i] - rayOrigin);

            return intersections.Length > 0;
        }

        public static float SqLength(Vector2 p) => p.x * p.x + p.y * p.y;

        public static bool Raycast(Polygon2 poly, Vector2 rayOrigin, Vector2 rayDirection, out Vector2 intersection, out float sqDistance) => Raycast(poly.points, rayOrigin, rayDirection, out intersection, out sqDistance);

        public static bool RaycastAny(Polygon2 poly, Vector2 rayOrigin, Vector2 rayDirection, out Vector2 intersection, out float sqDistance) => RaycastAny(poly.points, rayOrigin, rayDirection, out intersection, out sqDistance);

        public static bool RaycastAll(Polygon2 poly, Vector2 rayOrigin, Vector2 rayDirection, out Vector2[] intersections, out float[] sqDistances) => RaycastAll(poly.points, rayOrigin, rayDirection, out intersections, out sqDistances);
    }
}
