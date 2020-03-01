using UnityEngine;

namespace HexClicker
{
    [System.Serializable]
    public struct Counter
    {
        [SerializeField] private int amount;
        [SerializeField] private int max;

        public Counter(int amount, int max)
        {
            this.amount = amount;
            this.max = max;
        }

        public int AmountUnclamped
        {
            get => amount;
            set => amount = value;
        }

        public int MaxUnclamped
        {
            get => max;
            set => max = value;
        }

        public int RemainingUnclamped
        {
            get => max - amount;
            set => amount = max - value;
        }

        public float FractionAmountUnclamped
        {
            get => (float)amount / max;
            set => amount = (int)(max * value);
        }

        public float FractionRemainingUnclamped
        {
            get => 1 - (float)amount / max;
            set => amount = (int)(max * (1 - value));
        }

        public int Amount
        {
            get => Mathf.Clamp(amount, 0, max);
            set => amount = Mathf.Clamp(value, 0, max);
        }

        public int Max
        {
            get => Mathf.Max(0, max);
            set
            {
                max = Mathf.Max(0, value);
                amount = Mathf.Clamp(this.amount, 0, max);
            }
        }

        public int Remaining
        {
            get => Mathf.Clamp(max - amount, 0, max);
            set => amount = Mathf.Clamp(max - value, 0, max);
        }

        public float FractionAmount
        {
            get => Mathf.Clamp((float)amount / max, 0, 1);
            set => amount = Mathf.Clamp((int)(max * value), 0, max);
        }

        public float FractionRemaining
        {
            get => Mathf.Clamp(1 - (float)amount / max, 0, 1);
            set => amount = Mathf.Clamp((int)(max * (1 - value)), 0, max);
        }

        public bool Maxed => amount >= max;

        public void Clear()
        {
            amount = 0;
        }

        public void MaxOut()
        {
            amount = max;
        }

        public override bool Equals(object obj)
        {
            return obj is Counter timer &&
                   amount == timer.amount &&
                   max == timer.max;
        }

        public override int GetHashCode()
        {
            var hashCode = -1708519984;
            hashCode = hashCode * -1521134295 + amount.GetHashCode();
            hashCode = hashCode * -1521134295 + max.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Counter a, Counter b)
        {
            return a.amount == b.amount && a.max == b.max;
        }

        public static bool operator !=(Counter a, Counter b)
        {
            return a.amount != b.amount || a.max != b.max;
        }

        public static Counter operator +(Counter c, int a)
        {
            c.amount += a;
            return c;
        }

        public static Counter operator -(Counter c, int a)
        {
            c.amount -= a;
            return c;
        }

        public static Counter operator *(Counter c, float f)
        {
            c.amount = (int)(c.amount * f);
            return c;
        }

        public static Counter operator /(Counter c, float f)
        {
            c.amount = (int)(c.amount / f);
            return c;
        }

        public override string ToString()
        {
            return FormatAmountMaxCent;
        }

        public string FormatAmountMaxCent => $"{Amount}/{Max} ({FractionAmount:p0})";

        public string FormatCent => $"{FractionAmount:p0}";
    }
}
