using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class PathIterator
    {
        private readonly Vector3[] points;
        private readonly float[] distances;
        private int i;
        private float d0, d1;
        public float TotalDistance { get; private set; }
        public float CurrentDistance { get; private set; }
        public float T => Mathf.Clamp(CurrentDistance / TotalDistance, 0, 1);
        public Vector3 CurrentPosition { get; private set; }
        public PathIterator(List<PathFinding.Point> path)
        {
            points = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++)
                points[i] = path[i].Node.Position;

            distances = new float[path.Count - 1];
            for (int i = 0; i < path.Count - 1; i++)
            {
                distances[i] = Node.Distance(path[i].Node, path[i + 1].Node);
                TotalDistance += distances[i];
            }

            CurrentPosition = points.Length > 0 ? points[0] : Vector3.zero;

            if (distances.Length > 0)
                d1 = distances[0];
        }
        public void SetTime(float t) => SetDistance(t * TotalDistance);
        public void SetDistance(float distance)
        {
            if (distance == CurrentDistance)
                return;

            if (points.Length == 0)
            {
                i = 0;
                d0 = 0;
                d1 = 0;
                CurrentDistance = 0;
                CurrentPosition = Vector3.zero;
                return;
            }

            if (points.Length == 1 || distance < 0)
            {
                i = 0;
                d0 = 0;
                d1 = 0;
                CurrentDistance = 0;
                CurrentPosition = points[0];
                return;
            }

            if (distance >= TotalDistance)
            {
                i = points.Length - 1;
                d0 = TotalDistance - distances[distances.Length - 1];
                d1 = TotalDistance;
                CurrentDistance = TotalDistance;
                CurrentPosition = points[points.Length - 1];
                return;
            }

            CurrentDistance = distance;
            float sum = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                float d = distances[i];
                d0 = sum;
                d1 = sum + d;
                if (distance < sum + d)
                {
                    float t = (distance - sum) / d;
                    CurrentPosition = Vector3.Lerp(points[i], points[i + 1], t);
                    this.i = i;
                    return;
                }
                sum += d;
            }

            CurrentPosition = points[points.Length - 1];
        }
        public float AdvanceDistance(float amount)
        {
            if (points == null)
                return 0;

            if (points.Length <= 1)
                return 0;

            float distance = Mathf.Clamp(CurrentDistance + amount, 0, TotalDistance);
            amount = distance - CurrentDistance;
            CurrentDistance = distance;

            if (amount > 0)
            {
                float sum = d0;
                for (; i < distances.Length; i++)
                {
                    float d = distances[i];
                    d0 = sum;
                    d1 = sum + d;
                    if (distance < sum + d)
                    {
                        float t = (distance - sum) / d;
                        CurrentPosition = Vector3.Lerp(points[i], points[i + 1], t);
                        return amount;
                    }
                    sum += d;
                }
                i--;
                CurrentPosition = points[points.Length - 1];

                return amount;
            }

            else if (amount < 0)
            {
                float sum = d1;
                for (; i >= 0; i--)
                {
                    float d = distances[i];
                    d0 = sum - d;
                    d1 = sum;
                    if (distance >= sum - d)
                    {
                        float t = (sum - distance) / d;
                        CurrentPosition = Vector3.Lerp(points[i], points[i + 1], 1 - t);
                        return amount;
                    }
                    sum -= d;
                }
                CurrentPosition = points[0];

                return amount;
            }
            else
                return 0;
        }
        public Vector3 CalculatePosition(float distance)
        {
            if (points.Length == 0)
                return Vector3.zero;

            if (points.Length == 1)
                return points[0];

            if (distance < 0)
                return points[0];

            if (distance >= TotalDistance)
                return points[points.Length - 1];

            float sum = 0;

            for (int i = 0; i < points.Length - 1; i++)
            {
                float d = distances[i];
                if (distance < sum + d)
                {
                    float t = (distance - sum) / d;

                    return Vector3.Lerp(points[i], points[i + 1], t);
                }
                sum += d;
            }

            return points[points.Length - 1];
        }
    }
}
