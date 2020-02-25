using UnityEngine;

namespace HexClicker.Buildings
{
    public class BuildingManager : MonoBehaviour
    {
        [SerializeField] private Building placingObject;
        [SerializeField] private Material cantBuildMaterial;
        [SerializeField] private float placingRotation;

        private void Awake()
        {
            SetPlacingObject(placingObject);
        }
        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                placingRotation--;
            if (Input.GetKey(KeyCode.RightArrow))
                placingRotation++;

            if (ScreenCast.MouseTerrain.Cast(out RaycastHit hitInfo))//Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")) && hitInfo.collider.GetComponent<HexTile>())
            {
                World.Tile mouse = hitInfo.collider.GetComponent<World.Tile>();
                Matrix4x4 parentTransform = Matrix4x4.TRS(hitInfo.point, Quaternion.Euler(0, placingRotation, 0), Vector3.one);

                placingObject.ToTerrain(parentTransform);
                if (mouse && mouse.RegionID == 1 && !placingObject.CheckCollisions(parentTransform, LayerMask.GetMask("Buildings", "Water")))
                {
                    placingObject.Draw(parentTransform, LayerMask.NameToLayer("Placing"), null, true);

                    if (Input.GetMouseButtonDown(0))
                    {
                        Instantiate(placingObject, hitInfo.point, Quaternion.Euler(0, placingRotation, 0));
                    }

                    return;
                }
                placingObject.Draw(parentTransform, LayerMask.NameToLayer("Placing"), cantBuildMaterial, false);
            }
        }

        public void SetPlacingObject(Building placingObject)
        {
            this.placingObject = placingObject;
            if (placingObject)
                placingObject.ExtractParts();
        }
    }
}

