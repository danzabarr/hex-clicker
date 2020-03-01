using System;
using UnityEngine;

namespace HexClicker
{
    [System.Serializable]
    public struct Timer
    {
        [SerializeField] private float elapsed;
        [SerializeField] private float total;

        public Timer(float total)
        {
            elapsed = 0;
            this.total = total;
        }

        public Timer(float elapsed, float total)
        {
            this.elapsed = elapsed;
            this.total = total;
        }

        public float ElapsedUnclamped
        {
            get => elapsed;
            set => elapsed = value;
        }

        public float TotalUnclamped
        {
            get => total;
            set => total = value;
        }

        public float RemainingUnclamped
        {
            get => total - elapsed;
            set => elapsed = total - value;
        }

        public float FractionElapsedUnclamped
        {
            get => elapsed / total;
            set => elapsed = total * value;
        }

        public float FractionRemainingUnclamped
        {
            get => 1 - elapsed / total;
            set => elapsed = total * (1 - value);
        }

        public float Elapsed
        {
            get => Mathf.Clamp(elapsed, 0, total);
            set => elapsed = Mathf.Clamp(value, 0, total);
        }

        public float Total
        {
            get => Mathf.Max(0, total);
            set
            {
                total = Mathf.Max(0, value);
                elapsed = Mathf.Clamp(elapsed, 0, total);
            }
        }

        public float Remaining
        {
            get => Mathf.Clamp(total - elapsed, 0, total);
            set => elapsed = Mathf.Clamp(total - value, 0, total);
        }

        public float FractionElapsed
        {
            get => Mathf.Clamp(elapsed / total, 0, 1);
            set => elapsed = Mathf.Clamp(total * value, 0, total);
        }

        public float FractionRemaining
        {
            get => Mathf.Clamp(1 - elapsed / total, 0, 1);
            set => elapsed = Mathf.Clamp(total * (1 - value), 0, total);
        }

        public bool Completed => elapsed >= total;

        public void Advance(float time)
        {
            elapsed += time;
        }

        public void Reset()
        {
            elapsed = 0;
        }

        public void Complete()
        {
            elapsed = total;
        }

        public override bool Equals(object obj)
        {
            return obj is Timer timer &&
                   elapsed == timer.elapsed &&
                   total == timer.total;
        }

        public override int GetHashCode()
        {
            var hashCode = -1708519984;
            hashCode = hashCode * -1521134295 + elapsed.GetHashCode();
            hashCode = hashCode * -1521134295 + total.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Timer a, Timer b)
        {
            return a.elapsed == b.elapsed && a.total == b.total;
        }

        public static bool operator !=(Timer a, Timer b)
        {
            return a.elapsed != b.elapsed || a.total != b.total;
        }

        public override string ToString()
        {
            return FormatElapsedTotalCent;
        }

        public string FormatElapsedTotalCent => FormatMSP(Elapsed, Total);

        public string FormatElapsed => FormatMS(Remaining);

        public string FormatTotal => FormatMS(Total);

        public string FormatRemaining => FormatMS(Mathf.Ceil(Remaining));

        public string FormatCent => $"{FractionElapsed:p0}";

        public static string FormatMSP(float elapsed, float total)
        {
            TimeSpan e = TimeSpan.FromSeconds(elapsed);
            TimeSpan t = TimeSpan.FromSeconds(total);
            return
                $"{(int)e.TotalMinutes:00}:{e.Seconds:00}/{(int)t.TotalMinutes:00}:{t.Seconds:00} ({elapsed / total:p0})";
        }

        public static string FormatMS(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return
                $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
        }
    }
}
