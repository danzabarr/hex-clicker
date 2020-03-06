using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleFader : MonoBehaviour
    {
        [SerializeField] private Color selectedColor;
        [SerializeField] private float fadeInDuration = .1f;
        [SerializeField] private float fadeOutDuration = .2f;
        [SerializeField] private Graphic targetGraphic;

        private Toggle toggle;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void Start()
        {
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                    FadeIn();
                else
                    FadeOut();
            });

            if (toggle.isOn)
                targetGraphic.color = selectedColor;
            else
                targetGraphic.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0);
        }

        private void FadeIn()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeBackgroundAlpha(selectedColor.a, fadeInDuration));
        }

        private void FadeOut()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeBackgroundAlpha(0f, fadeOutDuration));
        }

        private IEnumerator FadeBackgroundAlpha(float targetAlpha, float duration)
        {
            float startAlpha = targetGraphic.color.a;
            duration *= Mathf.Abs(targetAlpha - startAlpha);

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                targetGraphic.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, Mathf.Lerp(startAlpha, targetAlpha, t / duration));
                yield return null;
            }

            targetGraphic.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, targetAlpha);
        }
    }
}
