using HexClicker.Animation;
using UnityEngine;
using UnityEngine.EventSystems;

public class EasingExample : MonoBehaviour
{
    private Coroutine routine;
    public AnimationCurve curve;

    public float duration;
    public Easing easing;

    public void OnMouseDown()
    {
        if (routine != null)
            StopCoroutine(routine);

        float randomAngle = Random.value * Mathf.PI * 2;

        Vector3 axis = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));

        Quaternion orientation = Quaternion.AngleAxis(90, axis);

        //routine = StartCoroutine(transform.AnimateLocalRotation(orientation, duration, easing, false));
        //routine = StartCoroutine(transform.AnimatePosition(transform.position + new Vector3(0, 0, 3), duration, easing, false));
        routine = StartCoroutine(transform.AnimateLocalScale(Vector3.one * 2, duration, easing, false));
    }
}
