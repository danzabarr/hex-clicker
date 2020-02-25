using UnityEngine;

namespace HexClicker.Buildings
{
    public class BuildingPart : MonoBehaviour
    {
        public Building parent;
        public Mesh PlacingMesh { get; private set; }
        public Material PlacingMaterial { get; private set; }
        public Matrix4x4 PlacingTransform { get; private set; }
        public Collider[] PlacingColliders { get; private set; }

        public bool followTerrain;
        private float originalElevation;

        public void SetupPlacingObjects()
        {
            PlacingMesh = GetComponent<MeshFilter>()?.sharedMesh;
            PlacingMaterial = GetComponent<MeshRenderer>()?.sharedMaterial;
            PlacingTransform = RelativeMatrix(transform, parent.transform);
            PlacingColliders = GetComponents<Collider>();
            originalElevation = transform.position.y;
        }

        public void RecalculatePlacingTransform()
        {
            PlacingTransform = RelativeMatrix(transform, parent.transform);
        }

        public void ToTerrain(Matrix4x4 parentTransform)
        {
            if (!followTerrain)
                return;

            Vector3 position = (parentTransform * transform.localToWorldMatrix).ExtractPosition();
            float sample = World.Map.Instance.SampleHeight(position.x, position.z) - parentTransform.ExtractPosition().y;
            transform.position = new Vector3(transform.position.x, sample + originalElevation, transform.position.z);
        }

        public static Matrix4x4 RelativeMatrix(Transform t, Transform relativeTo)
        {
            return relativeTo.worldToLocalMatrix * t.localToWorldMatrix;
        }

        public void Draw(Matrix4x4 parentTransform, int layer, Material material = null, bool shadows = true)
        {
            Graphics.DrawMesh(PlacingMesh, parentTransform * parent.transform.localToWorldMatrix * PlacingTransform, material ?? PlacingMaterial, layer, null, 0, null, shadows, shadows);
        }

        public bool CheckCollisions(Matrix4x4 parentTransform, int layerMask)
        {
            foreach (Collider c in PlacingColliders)
            {
                if (c is BoxCollider)
                {
                    BoxCollider box = c as BoxCollider;

                    Matrix4x4 matrix = parentTransform * RelativeMatrix(box.transform, parent.transform);

                    Vector3 center = matrix.ExtractPosition();
                    Quaternion orientation = matrix.ExtractRotation();
                    Vector3 halfExtents = box.size * .5f;
                    halfExtents.Scale(matrix.ExtractScale());

                    if (Physics.CheckBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.Collide))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
