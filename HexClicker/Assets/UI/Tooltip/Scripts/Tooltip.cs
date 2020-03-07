using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.Tooltip
{
    [RequireComponent(typeof(CanvasFader))]
    public class Tooltip : MonoBehaviour
    {
        public static Tooltip Instance { get; private set; }
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Canvas tooltipCanvas;
        [SerializeField] private RectTransform tooltipObject;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI bodyText;

        private CanvasFader canvasFader;
        private TooltipTarget _entity;
        private bool hidden = true;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != null)
            {
                Destroy(gameObject);
            }

            canvasFader = GetComponent<CanvasFader>();
        }

        private void Start()
        {
            canvasFader.Alpha = 0;
        }

        //LateUpdate tries to ensure tooltip layout and position is done after its relative parent has finished moving, and before rendering.
        private void LateUpdate()
        {
            SetPosition();
        }

        private void SetPosition()
        {
            if (_entity == null)
                return;

            Vector3 pos = tooltipObject.position;

            if (_entity.type == TooltipTarget.Anchor.Cursor)
                PositionCursor(ref pos);

            else if (_entity.type == TooltipTarget.Anchor.Parent)
                PositionParent(ref pos);

            else if (_entity.type == TooltipTarget.Anchor.Screen)
                PositionScreen(ref pos);

            if (_entity.clampToScreen)
                ClampToScreen(ref pos);

            tooltipObject.position = pos;
        }

        private void PositionCursor(ref Vector3 pos)
        {
            if (_entity == null)
                return;

            float x = Input.mousePosition.x
                    + _entity.offset.x
                    - _entity.tooltipPivot.x * tooltipObject.rect.width * tooltipCanvas.scaleFactor;
            float y = Input.mousePosition.y
                    + _entity.offset.y
                    + _entity.tooltipPivot.y * tooltipObject.rect.height * tooltipCanvas.scaleFactor;

            pos = new Vector3(x, y, 0);
        }

        private void PositionParent(ref Vector3 pos)
        {
            if (_entity == null)
                return;

            if (_entity.parentTransform == null)
                return;

            if (_entity.parentTransform is RectTransform)
            {
                RectTransform parentRect = _entity.parentTransform as RectTransform;

                Vector3 parentPosition = _entity.parentTransform.position;

                if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                    parentPosition = mainCanvas.worldCamera.WorldToScreenPoint(parentPosition);

                float x = parentPosition.x
                            - parentRect.pivot.x * parentRect.rect.size.x * mainCanvas.scaleFactor
                            + _entity.parentPivot.x * parentRect.sizeDelta.x * mainCanvas.scaleFactor
                            + _entity.offset.x
                            - _entity.tooltipPivot.x * tooltipObject.rect.width * tooltipCanvas.scaleFactor;

                float y = parentPosition.y
                            - parentRect.pivot.y * parentRect.rect.size.y * mainCanvas.scaleFactor
                            + _entity.parentPivot.y * parentRect.sizeDelta.y * mainCanvas.scaleFactor
                            + _entity.offset.y
                            + _entity.tooltipPivot.y * tooltipObject.rect.height * tooltipCanvas.scaleFactor;

                pos = new Vector3(x, y, 0);
            }
            else
            {
                Vector3 parentPosition = Camera.main.WorldToScreenPoint(_entity.parentTransform.position);

                float x = parentPosition.x
                            + _entity.offset.x
                            - _entity.tooltipPivot.x * tooltipObject.rect.width * tooltipCanvas.scaleFactor;

                float y = parentPosition.y
                            + _entity.offset.y
                            + _entity.tooltipPivot.y * tooltipObject.rect.height * tooltipCanvas.scaleFactor;

                pos = new Vector3(x, y, 0);

            }
        }

        private void PositionScreen(ref Vector3 pos)
        {
            if (_entity == null)
                return;

            float x = 0
                        + _entity.parentPivot.x * Screen.width
                        + _entity.offset.x
                        - _entity.tooltipPivot.x * tooltipObject.rect.width * tooltipCanvas.scaleFactor;

            float y = Screen.height
                        - _entity.parentPivot.y * Screen.height
                        + _entity.offset.y
                        + _entity.tooltipPivot.y * tooltipObject.rect.height * tooltipCanvas.scaleFactor;

            pos = new Vector3(x, y, 0);
        }

        private void ClampToScreen(ref Vector3 pos)
        {
            float rightEdgeToScreenEdgeDistance = Screen.width - (pos.x + tooltipObject.rect.width * tooltipCanvas.scaleFactor);
            if (rightEdgeToScreenEdgeDistance < 0)
            {
                pos.x += rightEdgeToScreenEdgeDistance;
            }

            float leftEdgeToScreenEdgeDistance = 0 - (pos.x);
            if (leftEdgeToScreenEdgeDistance > 0)
            {
                pos.x += leftEdgeToScreenEdgeDistance;
            }

            float topEdgeToScreenEdgeDistance = Screen.height - (pos.y);
            if (topEdgeToScreenEdgeDistance < 0)
            {
                pos.y += topEdgeToScreenEdgeDistance;
            }

            float bottomEdgeToScreenEdgeDistance = 0 - (pos.y - tooltipObject.rect.height * tooltipCanvas.scaleFactor);
            if (bottomEdgeToScreenEdgeDistance > 0)
            {
                pos.y += bottomEdgeToScreenEdgeDistance;
            }
        }

        public void DisplayInfo(TooltipTarget entity)
        {
            /*StringBuilder nameBuilder = new StringBuilder();
            StringBuilder textBodyBuilder = new StringBuilder();

            nameBuilder.Append(entity.GetTooltipName());
            textBodyBuilder.Append(entity.GetTooltipTextBody());*/

            _entity = entity;

            switch (entity.layout)
            {
                case TooltipTarget.Layout.Default:

                    nameText.gameObject.SetActive(true);
                    nameText.text = entity.title;

                    bodyText.gameObject.SetActive(true);
                    bodyText.text = entity.body;

                    break;

                case TooltipTarget.Layout.NoTitle:

                    nameText.gameObject.SetActive(false);

                    bodyText.gameObject.SetActive(true);
                    bodyText.text = entity.body;

                    break;


                case TooltipTarget.Layout.NoBody:

                    nameText.gameObject.SetActive(true);
                    nameText.text = entity.title;

                    bodyText.gameObject.SetActive(false);
                    break;
                
            }

            //Force a layout, then position the tooltip, then force another layout, ensures the tooltip is in the correct position before it is shown.
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipObject);
            SetPosition();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipObject);

            canvasFader.StartFadeIn(hidden && canvasFader.Alpha == 0 ? entity.delay : 0);
            hidden = false;
        }


        
        public void HideInfo(TooltipTarget entity)
        {
            if (_entity != entity)
                return;
            _entity = null;

            canvasFader.StartFadeOut();
            hidden = true;
        }
    }
}
