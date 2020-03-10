using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Animation
{
    public enum Easing
    {
        Linear,

        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,

        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,

        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,

        EaseInQuint,
        EaseOutQuint,
        EaseInOutQuint,

        EaseInSine,
        EaseOutSine,
        EaseInOutSine,

        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,

        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,

        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,

        EaseInBack,
        EaseOutBack,
        EaseInOutBack,

        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce

    }
    public delegate void Callback(float i);

    public delegate float EasingFunction(float t, float start, float change, float duration);

    public static class Transition 
    {
        public static float Linear(float t, float b, float c, float d)
        {
            return c * (t / d) + b;
        }
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
        public static float EaseInBack(float t, float b, float c, float d)
        {
            float s = 1.70158f;
            return c * (t /= d) * t * ((s + 1) * t - s) + b;
        }
        public static float EaseOutBack(float t, float b, float c, float d)
        {
            float s = 1.70158f;
            return c * ((t = t / d - 1) * t * ((s + 1) * t + s) + 1) + b;
        }
        public static float EaseInOutBack(float t, float b, float c, float d)
        {
            float s = 1.70158f;
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

        public static EasingFunction Function(Easing easing)
        {
            switch (easing)
            {
                case Easing.Linear:
                    return Linear;
                case Easing.EaseInQuad:
                    return EaseInQuad;
                case Easing.EaseOutQuad:
                    return EaseOutQuad;
                case Easing.EaseInOutQuad:
                    return EaseInOutQuad;
                case Easing.EaseInCubic:
                    return EaseInCubic;
                case Easing.EaseOutCubic:
                    return EaseOutCubic;
                case Easing.EaseInOutCubic:
                    return EaseInOutCubic;
                case Easing.EaseInQuart:
                    return EaseInQuart;
                case Easing.EaseOutQuart:
                    return EaseOutQuart;
                case Easing.EaseInOutQuart:
                    return EaseInOutQuart;
                case Easing.EaseInQuint:
                    return EaseInQuint;
                case Easing.EaseOutQuint:
                    return EaseOutQuint;
                case Easing.EaseInOutQuint:
                    return EaseInOutQuint;
                case Easing.EaseInSine:
                    return EaseInSine;
                case Easing.EaseOutSine:
                    return EaseOutSine;
                case Easing.EaseInOutSine:
                    return EaseInOutSine;
                case Easing.EaseInExpo:
                    return EaseInExpo;
                case Easing.EaseOutExpo:
                    return EaseOutExpo;
                case Easing.EaseInOutExpo:
                    return EaseInOutExpo;
                case Easing.EaseInCirc:
                    return EaseInCirc;
                case Easing.EaseOutCirc:
                    return EaseOutCirc;
                case Easing.EaseInOutCirc:
                    return EaseInOutCirc;
                case Easing.EaseInElastic:
                    return EaseInElastic;
                case Easing.EaseOutElastic:
                    return EaseOutElastic;
                case Easing.EaseInOutElastic:
                    return EaseInOutElastic;
                case Easing.EaseInBack:
                    return EaseInBack;
                case Easing.EaseOutBack:
                    return EaseOutBack;
                case Easing.EaseInOutBack:
                    return EaseInOutBack;
                case Easing.EaseInBounce:
                    return EaseInBounce;
                case Easing.EaseOutBounce:
                    return EaseOutBounce;
                case Easing.EaseInOutBounce:
                    return EaseInOutBounce;
                default:
                    return Linear;
            }
        }

        public static float Value(float t, float start, float change, float duration, Easing easing)
        {
            return Function(easing)(t, start, change, duration);
        }
        public static IEnumerator AnimateEasing(float start, float change, float duration, Easing easing, Callback callback, bool useUnscaledDeltaTime)
        {
            return AnimateEasing(start, change, duration, Function(easing), callback, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateEasing(float start, float change, float duration, EasingFunction easing, Callback callback, bool useUnscaledDeltaTime)
        {
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                callback(easing(t, start, change, duration));
                yield return null;
            }
            callback(easing(duration, start, change, duration));
        }
        public static IEnumerator AnimateCurve(float start, float change, float duration, AnimationCurve easing, Callback callback, bool useUnscaledDeltaTime)
        {
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                callback(start + easing.Evaluate(t / duration) * change);
                yield return null;
            }
            callback(start + easing.Evaluate(1) * change);
        }

        public delegate void Set<T>(T x);

        public static IEnumerator AnimateFloat(float start, float target, Set<float> setter, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateFloat(start, target, setter, duration, Function(easing), useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateFloat(float start, float target, Set<float> setter, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            float change = target - start;
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                setter(easing(t, start, change, duration));
                yield return null;
            }
            setter(easing(duration, start, change, duration));
        }
        public static IEnumerator AnimateFloat(float start, float target, Set<float> setter, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            float change = target - start;
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                setter(start + easing.Evaluate(t / duration) * change);
                yield return null;
            }
            setter(start + easing.Evaluate(1) * change);
        }

        public static IEnumerator AnimateVector3(Vector3 start, Vector3 target, Set<Vector3> setter, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(start, target, setter, duration, Function(easing), useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateVector3(Vector3 start, Vector3 target, Set<Vector3> setter, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            Vector3 delta = target - start;
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                setter(start + easing(t, 0, 1, duration) * delta);
                yield return null;
            }
            setter(start + easing(duration, 0, 1, duration) * delta);
        }
        public static IEnumerator AnimateVector3(Vector3 start, Vector3 target, Set<Vector3> setter, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            Vector3 delta = target - start;
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                setter(start + easing.Evaluate(t / duration) * delta);
                yield return null;
            }
            setter(start + easing.Evaluate(1) * delta);
        }

        public static IEnumerator AnimateRotation(Quaternion start, Quaternion target, Set<Quaternion> setter, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(start, target, setter, duration, Function(easing), useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateRotation(Quaternion start, Quaternion target, Set<Quaternion> setter, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                setter(Quaternion.LerpUnclamped(start, target, easing(t, 0, 1, duration)));
                yield return null;
            }
            setter(target);
        }
        public static IEnumerator AnimateRotation(Quaternion start, Quaternion target, Set<Quaternion> setter, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            for (float t = 0; t < duration; t += useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                setter(Quaternion.LerpUnclamped(start, target, easing.Evaluate(t / duration)));
                yield return null;
            }
            setter(target);
        }

        public static IEnumerator AnimatePosition(this Transform transform, Vector3 target, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.position, target, (Vector3 p) => transform.position = p, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimatePosition(this Transform transform, Vector3 target, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.position, target, (Vector3 p) => transform.position = p, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimatePosition(this Transform transform, Vector3 target, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.position, target, (Vector3 p) => transform.position = p, duration, easing, useUnscaledDeltaTime);
        }

        public static IEnumerator AnimateLocalPosition(this Transform transform, Vector3 target, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.localPosition, target, (Vector3 p) => transform.localPosition = p, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalPosition(this Transform transform, Vector3 target, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.localPosition, target, (Vector3 p) => transform.localPosition = p, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalPosition(this Transform transform, Vector3 target, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.localPosition, target, (Vector3 p) => transform.localPosition = p, duration, easing, useUnscaledDeltaTime);
        }

        public static IEnumerator AnimateLocalScale(this Transform transform, Vector3 target, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.localScale, target, (Vector3 p) => transform.localScale = p, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalScale(this Transform transform, Vector3 target, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.localScale, target, (Vector3 p) => transform.localScale = p, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalScale(this Transform transform, Vector3 target, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateVector3(transform.localScale, target, (Vector3 p) => transform.localScale = p, duration, easing, useUnscaledDeltaTime);
        }

        public static IEnumerator AnimateLocalRotation(this Transform transform, Vector3 target, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.localRotation, Quaternion.Euler(target), (Quaternion q) => transform.localRotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalRotation(this Transform transform, Vector3 target, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.localRotation, Quaternion.Euler(target), (Quaternion q) => transform.localRotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalRotation(this Transform transform, Vector3 target, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.localRotation, Quaternion.Euler(target), (Quaternion q) => transform.localRotation = q, duration, easing, useUnscaledDeltaTime);
        }

        public static IEnumerator AnimateLocalRotation(this Transform transform, Quaternion target, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.localRotation, target, (Quaternion q) => transform.localRotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalRotation(this Transform transform, Quaternion target, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.localRotation, target, (Quaternion q) => transform.localRotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateLocalRotation(this Transform transform, Quaternion target, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.localRotation, target, (Quaternion q) => transform.localRotation = q, duration, easing, useUnscaledDeltaTime);
        }

        public static IEnumerator AnimateRotation(this Transform transform, Vector3 target, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.rotation, Quaternion.Euler(target), (Quaternion q) => transform.rotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateRotation(this Transform transform, Vector3 target, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.rotation, Quaternion.Euler(target), (Quaternion q) => transform.rotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateRotation(this Transform transform, Vector3 target, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.rotation, Quaternion.Euler(target), (Quaternion q) => transform.rotation = q, duration, easing, useUnscaledDeltaTime);
        }
        
        public static IEnumerator AnimateRotation(this Transform transform, Quaternion target, float duration, AnimationCurve easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.rotation, target, (Quaternion q) => transform.rotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateRotation(this Transform transform, Quaternion target, float duration, EasingFunction easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.rotation, target, (Quaternion q) => transform.rotation = q, duration, easing, useUnscaledDeltaTime);
        }
        public static IEnumerator AnimateRotation(this Transform transform, Quaternion target, float duration, Easing easing, bool useUnscaledDeltaTime)
        {
            return AnimateRotation(transform.rotation, target, (Quaternion q) => transform.rotation = q, duration, easing, useUnscaledDeltaTime);
        }
    }
}
