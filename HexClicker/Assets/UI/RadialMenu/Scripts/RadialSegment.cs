using HexClicker.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace HexClicker.UI.Menus
{
    public class RadialSegment : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private UnityEvent onClick;
        private Coroutine routine;
        public MeshFilter MeshFilter { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
        public SpriteRenderer Icon => icon;

        public Mesh Mesh
        {
            get
            {
                if (MeshFilter == null)
                    MeshFilter = GetComponent<MeshFilter>();
                return MeshFilter.sharedMesh;
            }
            set
            {
                if (MeshFilter == null)
                    MeshFilter = GetComponent<MeshFilter>();
                MeshFilter.sharedMesh = value;
            }
        }

        public void Invoke()
        {
            onClick.Invoke();
        }

        public void Open(Easing easing, float openScale, float closedScale, float duration)
        {
            if (routine != null)
                StopCoroutine(routine);

            float currentScale = transform.localScale.x;
            if (currentScale == openScale)
                return;
            float delta = openScale - currentScale;
            float absDelta = Mathf.Abs(delta);
            duration *= Mathf.Abs(openScale - closedScale) / absDelta;

            routine = StartCoroutine(Transition.AnimateEasing(currentScale, delta, duration, easing, (float i) => transform.localScale = Vector3.one * i, true));

        }

        public void Close(Easing easing, float openScale, float closedScale, float duration)
        {
            if (routine != null)
                StopCoroutine(routine);

            float currentScale = transform.localScale.x;
            if (currentScale == closedScale)
                return;
            float delta = closedScale - currentScale;
            float absDelta = Mathf.Abs(delta);
            duration *= Mathf.Abs(openScale - closedScale) / absDelta;

            routine = StartCoroutine(Transition.AnimateEasing(currentScale, delta, duration, easing, (float i) => transform.localScale = Vector3.one * i, true));
        }
    }
}
