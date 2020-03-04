using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace HexClicker.Controls
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [SerializeField] private new Camera camera;
        [SerializeField] private Transform pitch;
        [SerializeField] private Transform zoom;
        [SerializeField] private PostProcessVolume postProcessing;

        [Header("Movement")]

        [SerializeField] private bool keepCameraAboveTerrain = true;
        [SerializeField] private float minimumHeightAboveTerrain = .5f;
        [SerializeField] private float moveSpeed = 1;
        [SerializeField] private float moveAcceleration = 1;
        [SerializeField] private float moveDampening = .00001f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 3;
        [SerializeField] private float rotationAcceleration = 20;
        [SerializeField] private float rotationDampening = .00001f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = .01f;
        [SerializeField] private float zoomAcceleration = .05f;
        [SerializeField] private float zoomDampening = .01f;
        [SerializeField] private float depthOfFieldFocusDistanceOffset = 0;

        [Header("Focus")]
        [SerializeField] private float focusDuration;

        [Header("Edge Scrolling")]
        [SerializeField] private bool enableEdgeScrolling = false;
        [SerializeField] private Vector2 edgeScrollInsets = new Vector2(10, 10);
        [SerializeField] private Vector2 edgeScrollSensitivity = Vector2.one;

        private Vector3 movementVelocity;
        private float rotationVelocity;
        private float zoomVelocity;
        private float zoomAmount = 0.5f;
        public Transform Focus { get; private set; }
        private bool atFocus;
        private Coroutine focusRoutine;

        private DepthOfField depthOfField;

        private void Awake()
        {
            Instance = this;
            postProcessing.profile.TryGetSettings(out depthOfField);
        }

        void LateUpdate()
        {
            if (Input.GetMouseButtonDown(0) && ScreenCast.MouseScene.Cast(out Units.Unit unit))
            {
                SetFocus(unit.transform, .05f);
            }

            Vector3 inputMovement = Vector2.zero;
            float inputRotation = 0;
            float inputZoom = Input.mouseScrollDelta.y;
            Vector2 inputEdgeScroll = Vector2.zero;

            if (Input.GetKey(KeyCode.W))
            {
                inputMovement.z++;
                ClearFocus();
            }

            if (Input.GetKey(KeyCode.A))
            {
                inputMovement.x--;
                ClearFocus();
            }

            if (Input.GetKey(KeyCode.S))
            {
                inputMovement.z--;
                ClearFocus();
            }

            if (Input.GetKey(KeyCode.D))
            {
                inputMovement.x++;
                ClearFocus();
            }
            //if (Input.GetKey(KeyCode.Space))
            //    input.y++;
            //if (Input.GetKey(KeyCode.X))
            //    input.y--;

            inputMovement.Normalize();

            if (enableEdgeScrolling)
            {
                if (Input.mousePosition.x < edgeScrollInsets.x || Input.mousePosition.x > Screen.width - edgeScrollInsets.x
                 || Input.mousePosition.y < edgeScrollInsets.y || Input.mousePosition.y > Screen.height - edgeScrollInsets.y)
                {
                    inputEdgeScroll.x += Input.mousePosition.x - Screen.width / 2f;
                    inputEdgeScroll.y += Input.mousePosition.y - Screen.height / 2;
                    inputEdgeScroll.Normalize();
                    ClearFocus();
                }
            }

            if (Input.GetKey(KeyCode.Q))
                inputRotation++;
            if (Input.GetKey(KeyCode.E))
                inputRotation--;

            if (Focus != null)
            {
                if (atFocus || focusRoutine == null)
                {
                    transform.position = Focus.position;
                    focusRoutine = null;
                }
            }
            else
            {
                movementVelocity += (transform.right * inputMovement.x + transform.up * inputMovement.y + transform.forward * inputMovement.z) * moveAcceleration * Time.deltaTime;
                movementVelocity += (transform.right * inputEdgeScroll.x * edgeScrollSensitivity.x + transform.forward * inputEdgeScroll.y * edgeScrollSensitivity.y) * moveAcceleration * Time.deltaTime;
                movementVelocity = Vector3.ClampMagnitude(movementVelocity, moveSpeed);
                transform.position += movementVelocity * Time.deltaTime;
            }
            movementVelocity *= Mathf.Pow(moveDampening, Time.deltaTime);

            rotationVelocity += inputRotation * rotationAcceleration * Time.deltaTime;
            rotationVelocity = Mathf.Clamp(rotationVelocity, -rotationSpeed, +rotationSpeed);
            transform.rotation *= Quaternion.Euler(0, rotationVelocity * Time.deltaTime, 0);
            rotationVelocity *= Mathf.Pow(rotationDampening, Time.deltaTime);

            zoomVelocity += inputZoom * zoomAcceleration * Time.deltaTime;
            zoomVelocity = Mathf.Clamp(zoomVelocity, -zoomSpeed, zoomSpeed);
            zoomAmount = Mathf.Clamp(zoomAmount - zoomVelocity * Time.deltaTime, 0, 1);
            zoom.localPosition = Vector3.forward * -(zoomAmount * 20);
            pitch.localRotation = Quaternion.Euler(zoomAmount * 50 + 40, pitch.localRotation.eulerAngles.y, pitch.localRotation.eulerAngles.z);
            zoomVelocity *= Mathf.Pow(zoomDampening, Time.deltaTime);

            if (ScreenCast.CenterTerrain.Cast(out RaycastHit hitInfo))
            {
                depthOfField.focusDistance.value = hitInfo.distance + depthOfFieldFocusDistanceOffset;
                depthOfField.focalLength.value = (hitInfo.distance - .5f) / 10f * 220f + 80f;
            }

            camera.transform.localPosition = Vector3.zero;
            float terrain = World.Map.Instance.SampleHeight(camera.transform.position.x, camera.transform.position.z) + minimumHeightAboveTerrain;
            camera.transform.position = new Vector3(camera.transform.position.x, Mathf.Max(terrain, camera.transform.position.y), camera.transform.position.z);


            //Used for culling grass a certain distance away from the camera focal point.
            Shader.SetGlobalVector("_CameraFocalPoint", transform.position);
        }

        /// <summary>
        /// Sets the transform for the camera to follow, and an initial zoom level from 0-1 where 1 is furthest away, and 0 is closest to the subject.
        /// </summary>
        public void SetFocus(Transform focus, float targetZoom)
        {
            Focus = focus;
            if (focus != null)
                ZoomTo(focus, targetZoom);
        }

        public void ClearFocus()
        {
            Focus = null;
            if (focusRoutine != null)
                StopCoroutine(focusRoutine);
            focusRoutine = null;
        }

        /// <summary>
        /// Begins a routine where the camera will center and zoom into a target position and zoom level.
        /// </summary>
        public void ZoomTo(Vector3 targetPosition, float targetZoom)
        {
            if (focusRoutine != null)
                StopCoroutine(focusRoutine);
            focusRoutine = StartCoroutine(ZoomRoutine(targetPosition, targetZoom, focusDuration));
        }

        /// <summary>
        /// Begins a routine where the camera will center and zoom into a target position and zoom level.
        /// </summary>
        public void ZoomTo(Transform targetPosition, float targetZoom)
        {
            if (focusRoutine != null)
                StopCoroutine(focusRoutine);
            focusRoutine = StartCoroutine(ZoomRoutine(targetPosition, targetZoom, focusDuration));
        }
        IEnumerator ZoomRoutine(Vector3 targetPosition, float targetZoom, float duration)
        {
            targetZoom = Mathf.Clamp(targetZoom, 0, 1);

            if (duration > 0)
            {
                float speed = 1f / duration;
                atFocus = false;
                float oldZoom = zoomAmount;
                Vector3 oldPosition = transform.position;
                for (float t = 0; t < 1; t += Time.deltaTime * speed)
                {
                    zoomAmount = Mathf.Lerp(oldZoom, targetZoom, t);
                    transform.position = Vector3.Lerp(oldPosition, targetPosition, t);
                    yield return null;
                }
            }

            zoomAmount = targetZoom;
            transform.position = targetPosition;
            atFocus = true;
        }
        IEnumerator ZoomRoutine(Transform targetPosition, float targetZoom, float duration)
        {
            targetZoom = Mathf.Clamp(targetZoom, 0, 1);
            if (duration > 0)
            {
                float speed = 1f / duration;
                atFocus = false;
                float oldZoom = zoomAmount;
                Vector3 oldPosition = transform.position;
                for (float t = 0; t < 1; t += Time.deltaTime * speed)
                {
                    zoomAmount = Mathf.Lerp(oldZoom, targetZoom, t);
                    transform.position = Vector3.Lerp(oldPosition, targetPosition.position, t);
                    yield return null;
                }
            }
            zoomAmount = targetZoom;
            transform.position = targetPosition.position;
            atFocus = true;
        }
    }
}
