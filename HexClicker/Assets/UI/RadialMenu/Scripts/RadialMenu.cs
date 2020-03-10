using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.UI.Menus
{
    public class RadialMenu : MonoBehaviour
    {
        public static RadialMenu Active { get; private set; }

        private static Dictionary<string, RadialMenu> menus = new Dictionary<string, RadialMenu>();

        public static RadialMenu Get(string name) => menus[name];

        [SerializeField] private new Camera camera;
        [SerializeField] private Canvas canvas;
        [Space]
        [SerializeField] private new string name;

        [Header("Size & Shape")]
        [SerializeField] private float radius = .15f;
        [SerializeField, Range(0, 1)] private float innerRadius = .5f;
        [SerializeField] private float resolution = 200;
        [SerializeField] private float spacing = .001f;

        [Header("Open/Close Transitions")]
        [SerializeField] private float openTransitionDuration = .3f;
        [SerializeField] private float closeTransitionDuration = .5f;
        [Space]
        [SerializeField] private Animation.Easing openTransitionEasing = Animation.Easing.EaseOutBack;
        [SerializeField] private Animation.Easing closeTransitionEasing = Animation.Easing.EaseOutExpo;

        [Header("On Hover Transitions")]

        [SerializeField] private float minHoverRadius = 50f;
        [SerializeField] private float maxHoverRadius = 200f;
        [Space]
        [SerializeField] private float hoverScale = 1.1f;
        [Space]
        [SerializeField] private float mouseEnterTransitionDuration = 1f;
        [SerializeField] private float mouseExitTransitionDuration = 1f;
        [Space]
        [SerializeField] private Animation.Easing transitionMouseEnterEasing = Animation.Easing.EaseOutBack;
        [SerializeField] private Animation.Easing transitionMouseExitEasing = Animation.Easing.EaseOutElastic;

        private Coroutine routine;
        private RadialSegment[] segments;
        private bool open;
        private bool wasOpen;
        private float scale;
        public int Length => segments.Length;
        public int HighlightedIndex { get; private set; }
        public RadialSegment HighlightedSegment => HighlightedIndex < 0 || HighlightedIndex >= Length ? null : segments[HighlightedIndex];
        public RadialMenuTarget Target { get; private set; }

        private void Awake()
        {
            menus.Add(name, this);
        }

        private void Start()
        {
            GenerateMeshes();
        }
        
        private void OnValidate()
        {
            GenerateMeshes();
        }


        public bool Open(RadialMenuTarget target)
        {
            if (Active != null)
                return false;
            
            transform.position = target.transform.position;
            open = true;
            Active = this;
            Target = target;

            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Animation.Transition.AnimateEasing(scale, 1 - scale, (1 - scale) * openTransitionDuration, openTransitionEasing, (float i) => { scale = i; }, true));

            return true;
        }

        public void Close()
        {
            open = false;
            if (Target != null)
                Target.Active = false;
            Target = null;
            if (this == Active)
                Active = null;

            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Animation.Transition.AnimateEasing(scale, -scale, scale * closeTransitionDuration, closeTransitionEasing, (float i) => { scale = i; }, true));
        }

        private void LateUpdate()
        {
            Vector3 screen = camera.WorldToScreenPoint(transform.position);
            Vector3 delta = Input.mousePosition - screen;
            float sqDist = delta.x * delta.x + delta.y * delta.y;

            int lastIndex = HighlightedIndex;
            HighlightedIndex = -1;

            if (open && sqDist > minHoverRadius * minHoverRadius && sqDist < maxHoverRadius * maxHoverRadius)
            {
                float angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
                HighlightedIndex = Segment(angle);
            }

            if (Target != null)
                transform.position = Target.transform.position;

            transform.rotation = camera.transform.rotation;
            transform.localScale = Vector3.one * scale * Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward) * canvas.scaleFactor;

            if (lastIndex != HighlightedIndex)
            {
                if (lastIndex != -1)
                    segments[lastIndex].Close(transitionMouseExitEasing, hoverScale, 1f, mouseExitTransitionDuration);
                if (HighlightedIndex != -1)
                    segments[HighlightedIndex].Open(transitionMouseEnterEasing, hoverScale, 1f, mouseEnterTransitionDuration);
            }

            if (this == Active && open && wasOpen && !UI.UIMethods.IsMouseOverUI)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (HighlightedIndex == -1)
                        Close();
                    else
                        HighlightedSegment.Invoke();
                }
                if (Input.GetMouseButtonDown(1))
                {
                    Close();
                }
            }

            wasOpen = open;
        }

        public int Segment(float angleDegrees)
        {
            angleDegrees += (360f / Length) / 2f;
            angleDegrees %= 360;
            angleDegrees += 360;
            angleDegrees %= 360;
            angleDegrees /= 360;
            angleDegrees *= Length;
            int segment = Mathf.FloorToInt(angleDegrees);
            return segment;
        }
        
        private void GenerateMeshes()
        {
            segments = transform.GetComponentsInChildren<RadialSegment>();

            float segmentAngle = Mathf.PI * 2f / Length;
            float inner = radius * innerRadius;

            int points = Mathf.Max((int)(resolution / Length), 1);
            float startRotation = -segmentAngle / 2f;

            Vector3[] vertices = new Vector3[(points + 1) * 2];
            Vector2[] uv = new Vector2[(points + 1) * 2];
            int[] triangles = new int[(points) * 6];

            for (int s = 0; s < Length; s++)
            {
                float segmentStart = startRotation + s * segmentAngle;
                float segmentEnd = startRotation + (s + 1) * segmentAngle;

                float i1 = segmentStart;
                float o1 = segmentStart;
                float i2 = segmentEnd;
                float o2 = segmentEnd;

                if (Length > 1)
                {
                    float oY = Mathf.Sqrt(radius * radius - spacing * spacing);
                    float oA = Mathf.Atan2(spacing, oY);

                    float iY = Mathf.Sqrt(inner * inner - spacing * spacing);
                    float iA = Mathf.Atan2(spacing, iY);

                    i1 += iA;
                    o1 += oA;
                    i2 -= iA;
                    o2 -= oA;
                }

                for (int v = 0; v < points; v++)
                {
                    float i = Mathf.Lerp(i1, i2, (float)v / points);
                    float o = Mathf.Lerp(o1, o2, (float)v / points);

                    vertices[v * 2 + 0] = new Vector3(Mathf.Sin(i), Mathf.Cos(i)) * inner;
                    vertices[v * 2 + 1] = new Vector3(Mathf.Sin(o), Mathf.Cos(o)) * radius;

                    uv[v * 2 + 0] = new Vector2((float)v / points, 0);
                    uv[v * 2 + 1] = new Vector2((float)v / points, 1);

                    triangles[v * 6 + 0] = v * 2 + 0;
                    triangles[v * 6 + 1] = v * 2 + 1;
                    triangles[v * 6 + 2] = v * 2 + 2;

                    triangles[v * 6 + 3] = v * 2 + 1;
                    triangles[v * 6 + 4] = v * 2 + 3;
                    triangles[v * 6 + 5] = v * 2 + 2;
                }

                Vector3 lI = new Vector3(Mathf.Sin(i2), Mathf.Cos(i2)) * inner;
                Vector3 lO = new Vector3(Mathf.Sin(o2), Mathf.Cos(o2)) * radius;

                vertices[points * 2 + 0] = lI;
                vertices[points * 2 + 1] = lO;

                uv[points * 2 + 0] = new Vector2(1, 0);
                uv[points * 2 + 1] = new Vector2(1, 1);

                Mesh mesh = new Mesh()
                {
                    vertices = vertices,
                    uv = uv,
                    triangles = triangles
                };

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();

                segments[s].Mesh = mesh;
                float center = startRotation + (s + .5f) * segmentAngle;
                segments[s].Icon.transform.localPosition = new Vector3(Mathf.Sin(center), Mathf.Cos(center)) * radius * (innerRadius + (1 - innerRadius) * .5f);
            }
        }
    }
}
