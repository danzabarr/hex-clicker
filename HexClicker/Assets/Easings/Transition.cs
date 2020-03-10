using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Animation
{
    public static class Transition
    {
        public delegate void Callback(float i);

        public delegate float SimpleEasing(float t);

        public delegate float AdvancedEasing(float t, float start, float change, float duration);

        public delegate float AdvancedEasing2(float t, float start, float change, float duration, float s);

        // no easing, no acceleration
        public static float Linear(float t) => t;
        // accelerating from zero velocity
        public static float EaseInQuad(float t) => t * t;
        // decelerating to zero velocity
        public static float EaseOutQuad(float t) => t * (2 - t);
        // acceleration until halfway, then deceleration
        public static float EaseInOutQuad(float t) => t < .5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        // accelerating from zero velocity 
        public static float EaseInCubic(float t) => t * t * t;
        // decelerating to zero velocity 
        public static float EaseOutCubic(float t) => (--t) * t * t + 1;
        // acceleration until halfway, then deceleration 
        public static float EaseInOutCubic(float t) => t < .5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
        // accelerating from zero velocity 
        public static float EaseInQuart(float t) => t * t * t * t;
        // decelerating to zero velocity 
        public static float EaseOutQuart(float t) => 1 - (--t) * t * t * t;
        // acceleration until halfway, then deceleration
        public static float EaseInOutQuart(float t) => t < .5 ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;
        // accelerating from zero velocity
        public static float EaseInQuint(float t) => t * t * t * t * t;
        // decelerating to zero velocity
        public static float EaseOutQuint(float t) => 1 + (--t) * t * t * t * t;
        // acceleration until halfway, then deceleration 
        public static float EaseInOutQuint(float t) => t < .5 ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;



        public static float EaseInQuad(float t, float b, float c, float d)
        {
            return c * (t /= d) * t + b;
        }
        public static float EaseOutQuad(float t, float b, float c, float d)
        {
            return -c * (t /= d) * (t - 2) + b;
        }
        public static float EaseInOutQuad(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t + b;
            return -c / 2 * ((--t) * (t - 2) - 1) + b;
        }
        public static float EaseInCubic(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t + b;
        }
        public static float EaseOutCubic(float t, float b, float c, float d)
        {
            return c * ((t = t / d - 1) * t * t + 1) + b;
        }
        public static float EaseInOutCubic(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t + 2) + b;
        }
        public static float EaseInQuart(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t * t + b;
        }
        public static float EaseOutQuart(float t, float b, float c, float d)
        {
            return -c * ((t = t / d - 1) * t * t * t - 1) + b;
        }
        public static float EaseInOutQuart(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t * t + b;
            return -c / 2 * ((t -= 2) * t * t * t - 2) + b;
        }
        public static float EaseInQuint(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t * t * t + b;
        }
        public static float EaseOutQuint(float t, float b, float c, float d)
        {
            return c * ((t = t / d - 1) * t * t * t * t + 1) + b;
        }
        public static float EaseInOutQuint(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;
        }
        public static float EaseInSine(float t, float b, float c, float d)
        {
            return -c * Mathf.Cos(t / d * (Mathf.PI / 2)) + c + b;
        }
        public static float EaseOutSine(float t, float b, float c, float d)
        {
            return c * Mathf.Sin(t / d * (Mathf.PI / 2)) + b;
        }
        public static float EaseInOutSine(float t, float b, float c, float d)
        {
            return -c / 2 * (Mathf.Cos(Mathf.PI * t / d) - 1) + b;
        }
        public static float EaseInExpo(float t, float b, float c, float d)
        {
            return (t <= 0) ? b : c * Mathf.Pow(2, 10 * (t / d - 1)) + b;
        }
        public static float EaseOutExpo(float t, float b, float c, float d)
        {
            return (t >= d) ? b + c : c * (-Mathf.Pow(2, -10 * t / d) + 1) + b;
        }
        public static float EaseInOutExpo(float t, float b, float c, float d)
        {
            if (t <= 0) return b;
            if (t >= d) return b + c;
            if ((t /= d / 2) < 1) return c / 2 * Mathf.Pow(2, 10 * (t - 1)) + b;
            return c / 2 * (-Mathf.Pow(2, -10 * --t) + 2) + b;
        }
        public static float EaseInCirc(float t, float b, float c, float d)
        {
            return -c * (Mathf.Sqrt(1 - (t /= d) * t) - 1) + b;
        }
        public static float EaseOutCirc(float t, float b, float c, float d)
        {
            return c * Mathf.Sqrt(1 - (t = t / d - 1) * t) + b;
        }
        public static float EaseInOutCirc(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return -c / 2 * (Mathf.Sqrt(1 - t * t) - 1) + b;
            return c / 2 * (Mathf.Sqrt(1 - (t -= 2) * t) + 1) + b;
        }
        public static float EaseInElastic(float t, float b, float c, float d)
        {
            if (t <= 0) return b;
            if ((t /= d) >= 1) return b + c;
            float p = d * .3f;
            float a = c;
            float s = a < Mathf.Abs(c) ? (p / 4) : (p / (2 * Mathf.PI) * Mathf.Asin(c / a));
            return -(a * Mathf.Pow(2, 10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p)) + b;
        }
        public static float EaseOutElastic(float t, float b, float c, float d)
        {
            if (t <= 0) return b;
            if ((t /= d) >= 1) return b + c;
            float p = d * .3f;
            float a = c;
            float s = a < Mathf.Abs(c) ? (p / 4) : (p / (2 * Mathf.PI) * Mathf.Asin(c / a));
            return a * Mathf.Pow(2, -10 * t) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p) + c + b;
        }
        public static float EaseInOutElastic(float t, float b, float c, float d)
        {
            if (t <= 0) return b;
            if ((t /= d / 2) >= 2) return b + c;
            float p = d * (.3f * 1.5f);
            float a = c;
            float s = a < Mathf.Abs(c) ? (p / 4) : (p / (2 * Mathf.PI) * Mathf.Asin(c / a));
            if (t < 1) return -.5f * (a * Mathf.Pow(2, 10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p)) + b;
            return a * Mathf.Pow(2, -10 * (t -= 1)) * Mathf.Sin((t * d - s) * (2 * Mathf.PI) / p) * .5f + c + b;
        }
        public static float EaseInBack(float t, float b, float c, float d, float s = 1.70158f)
        {
            return c * (t /= d) * t * ((s + 1) * t - s) + b;
        }
        public static float EaseOutBack(float t, float b, float c, float d, float s = 1.70158f)
        {
            return c * ((t = t / d - 1) * t * ((s + 1) * t + s) + 1) + b;
        }
        public static float EaseInOutBack(float t, float b, float c, float d, float s = 1.70158f)
        {
            if ((t /= d / 2) < 1) return c / 2 * (t * t * (((s *= (1.525f)) + 1) * t - s)) + b;
            return c / 2 * ((t -= 2) * t * (((s *= (1.525f)) + 1) * t + s) + 2) + b;
        }
        public static float EaseInBounce(float t, float b, float c, float d)
        {
            return c - EaseOutBounce(d - t, 0, c, d) + b;
        }
        public static float EaseOutBounce(float t, float b, float c, float d)
        {
            if ((t /= d) < (1 / 2.75f))
            {
                return c * (7.5625f * t * t) + b;
            }
            else if (t < (2 / 2.75f))
            {
                return c * (7.5625f * (t -= (1.5f / 2.75f)) * t + .75f) + b;
            }
            else if (t < (2.5 / 2.75))
            {
                return c * (7.5625f * (t -= (2.25f / 2.75f)) * t + .9375f) + b;
            }
            else
            {
                return c * (7.5625f * (t -= (2.625f / 2.75f)) * t + .984375f) + b;
            }
        }
        public static float EaseInOutBounce(float t, float b, float c, float d)
        {
            if (t < d / 2) return EaseInBounce(t * 2, 0, c, d) * .5f + b;
            return EaseOutBounce(t * 2 - d, 0, c, d) * .5f + c * .5f + b;
        }

        public static IEnumerator AnimateSimple(float start, float change, float duration, SimpleEasing easing, Callback callback)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                callback(start + easing(t / duration) * change);
                yield return null;
            }
        }

        public static IEnumerator AnimateAdvanced(float start, float change, float duration, float s, AdvancedEasing2 easing, Callback callback)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                callback(easing(t, start, change, duration, s));
                yield return null;
            }
        }
        public static IEnumerator AnimateAdvanced(float start, float change, float duration, AdvancedEasing easing, Callback callback)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                callback(easing(t, start, change, duration));
                yield return null;
            }
        }

        public static IEnumerator AnimateCurve(float start, float change, float duration, AnimationCurve curve, Callback callback)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                callback(start + curve.Evaluate(t / duration) * change);
                yield return null;
            }
        }
    }
}
