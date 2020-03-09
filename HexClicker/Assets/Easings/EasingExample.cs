using HexClicker.Animation;
using UnityEngine;
using UnityEngine.EventSystems;

public class EasingExample : MonoBehaviour, IPointerClickHandler
{

    private Coroutine routine;
    public AnimationCurve curve;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (routine != null)
            StopCoroutine(routine);

        //EXAMPLE USAGE:

        //routine = StartCoroutine(Transition.AnimateSimple(1f, 1f, 1f, Transition.EaseInQuart, (float i) => transform.localScale = Vector3.one * i));
        //routine = StartCoroutine(Transition.AnimateAdvanced(1f, 1f, 1f, Transition.EaseOutBounce, (float i) => transform.localScale = Vector3.one * i));
        //routine = StartCoroutine(Transition.AnimateAdvanced(1f, 1f, 1f, Transition.EaseInOutElastic, (float i) => transform.localScale = Vector3.one * i));
        //routine = StartCoroutine(Transition.AnimateAdvanced(1f, 1f, 1f, 1.70157f, Transition.EaseOutBack, (float i) => transform.localScale = Vector3.one * i));
        routine = StartCoroutine(Transition.AnimateCurve(1f, 1f, 1f, curve, (float i) => transform.localScale = Vector3.one * i));
    }

}
