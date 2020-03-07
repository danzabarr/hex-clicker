using UnityEngine;

namespace HexClicker.UI.Menus
{
    public class RadialMenu : MonoBehaviour
    {
        public static RadialMenu Active { get; private set; }

        [SerializeField] private float radius;
        [SerializeField, Range(0, 1)] private float innerRadius;
        [SerializeField] private float resolution;
        [SerializeField] private float spacing;
        [SerializeField] private float highlightScale = 1;
        [SerializeField] private float highlightInDuration;
        [SerializeField] private float highlightOutDuration;
        [SerializeField] private float hideDuration;
        [SerializeField] private float minimumSelectRadius;
        [SerializeField] private float maximumSelectRadius;
        [SerializeField] private new Camera camera;
        [SerializeField] private Canvas canvas;

        private RadialSegment[] segments;
        private bool open;
        private bool wasOpen;
        private float scale;
        public int Length => segments.Length;
        public int HighlightedIndex { get; private set; }
        public RadialSegment HighlightedSegment => HighlightedIndex < 0 || HighlightedIndex >= Length ? null : segments[HighlightedIndex];
        public RadialMenuTarget Target { get; private set; }

        private void Start()
        {
            GenerateMeshes();

            for (int i = 0; i < segments.Length; i++)
                segments[i].transform.localScale = Vector3.zero;
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
        }

        private void LateUpdate()
        {
            Vector3 screen = camera.WorldToScreenPoint(transform.position);
            Vector3 delta = Input.mousePosition - screen;
            float sqDist = delta.x * delta.x + delta.y * delta.y;

            HighlightedIndex = -1;

            if (sqDist > minimumSelectRadius * minimumSelectRadius && sqDist < maximumSelectRadius * maximumSelectRadius)
            {
                float angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
                HighlightedIndex = Segment(angle);
            }

            if (Target != null)
                transform.position = Target.transform.position;

            transform.rotation = camera.transform.rotation;
            transform.localScale = Vector3.one * scale * Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward) * canvas.scaleFactor;

            scale = Mathf.Lerp(scale, open ? 1 : 0, Time.deltaTime / hideDuration);

            if (open)
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    if (i == HighlightedIndex)
                        segments[i].transform.localScale = Vector3.Lerp(segments[i].transform.localScale, Vector3.one * highlightScale, Time.deltaTime / highlightInDuration);
                    else
                        segments[i].transform.localScale = Vector3.Lerp(segments[i].transform.localScale, Vector3.one, Time.deltaTime / highlightOutDuration);
                }
            }

            if (this == Active && open && wasOpen)
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
