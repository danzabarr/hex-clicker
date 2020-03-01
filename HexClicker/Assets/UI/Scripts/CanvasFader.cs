using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasFader : MonoBehaviour
    {
        [SerializeField] private float fadeInDuration;
        [SerializeField] private float fadeOutDuration;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public float Alpha
        {
            get => canvasGroup.alpha;
            set => canvasGroup.alpha = value;
        }

        public void StartFadeIn()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeIn());
        }

        public void StartFadeOut(bool destroy = false, bool deactivate = false)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOut(destroy, deactivate));
        }

        private IEnumerator FadeIn()
        {
            for (float t = canvasGroup.alpha; t < 1; t += Time.unscaledDeltaTime / fadeInDuration)
            {
                canvasGroup.alpha = t;
                yield return null;
            }
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
        }

        private IEnumerator FadeOut(bool destroy = false, bool deactivate = false)
        {
            for (float t = canvasGroup.alpha; t >= 0; t -= Time.unscaledDeltaTime / fadeOutDuration)
            {
                canvasGroup.alpha = t;
                yield return null;
            }
            canvasGroup.alpha = 0;
            if (deactivate)
                gameObject.SetActive(false);
            if (destroy)
                Destroy(gameObject);
        }
    }
}
