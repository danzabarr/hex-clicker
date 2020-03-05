using UnityEngine;
using UnityEngine.EventSystems;

namespace HexClicker.UI.Tooltip
{
    public class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum Anchor
        {
            Cursor,
            Parent,
            Screen
        }

        public enum Layout
        {
            Default,
            NoTitle,
            NoBody
        }


        [Header("Position")]
        public Anchor type;
        public Vector2 offset;
        public Vector2 tooltipPivot;
        public Vector2 parentPivot;
        public Transform parentTransform;
        public bool clampToScreen;

        [Header("Layout")]
        public Layout layout;

        [Header("Content")]
        public string title;
        [TextArea] public string body;

        private Outline outline;

        private void Awake()
        {
            outline = GetComponent<Outline>();
        }

        private void Start()
        {
            if (outline)
                outline.enabled = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tooltip.Instance.DisplayInfo(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.Instance.HideInfo(this);
        }

        void OnMouseEnter()
        {
            if (!UIMethods.IsMouseOverUI)
            {
                if (outline)
                    outline.enabled = true;
                Tooltip.Instance.DisplayInfo(this);
            }
        }

        void OnMouseExit()
        {
            if (outline)
                outline.enabled = false;
            Tooltip.Instance.HideInfo(this);
        }
    }
}
