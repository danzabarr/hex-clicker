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
        routine = StartCoroutine(transform.AnimateLocalRotation(Quaternion.AngleAxis(90, new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle))), duration, easing, false));
        routine = StartCoroutine(transform.AnimatePosition(transform.position + new Vector3(0, 0, 3), duration, easing, false));
        //routine = StartCoroutine(transform.AnimateLocalScale(Vector3.one, duration, easing, false));

    }

}
