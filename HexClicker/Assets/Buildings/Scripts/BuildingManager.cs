using UnityEngine;

namespace HexClicker.Buildings
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [SerializeField] private Material cantBuildMaterial;
        [SerializeField] private float placingRotation;
        private Building placingObject;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (placingObject == null)
                return;

            if (Input.GetKey(KeyCode.LeftArrow))
                placingRotation -= 5;

            if (Input.GetKey(KeyCode.RightArrow))
                placingRotation += 5;

            

            if (ScreenCast.MouseTerrain.Cast(out RaycastHit hitInfo))//Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")) && hitInfo.collider.GetComponent<HexTile>())
            {
                World.Tile mouse = hitInfo.collider.GetComponent<World.Tile>();
                Matrix4x4 parentTransform = Matrix4x4.TRS(hitInfo.point, Quaternion.Euler(0, placingRotation, 0), Vector3.one);

                placingObject.ToTerrain(parentTransform);
                if (mouse && mouse.RegionID == 1 && !placingObject.CheckCollisions(parentTransform, LayerMask.GetMask("Buildings", "Units", "Water", "Rocks")))
                {
                    placingObject.Draw(parentTransform, LayerMask.NameToLayer("Placing"), null, true);

                    if (Input.GetMouseButtonDown(0) && !UI.UIMethods.IsMouseOverUI)
                    {
                        Building placed = Instantiate(placingObject, hitInfo.point, Quaternion.Euler(0, placingRotation, 0));
                        placed.gameObject.SetActive(true);
                        placed.OnPlace();
                    }

                    return;
                }
                placingObject.Draw(parentTransform, LayerMask.NameToLayer("Placing"), cantBuildMaterial, false);
            }
        }

        public void SetPlacingObject(Building building)
        {
            if (placingObject)
                Destroy(placingObject.gameObject);
            placingObject = null;
            if (building)
            {
                placingObject = Instantiate(building);
                placingObject.ExtractParts();
                placingObject.gameObject.SetActive(false);
            }
        }
    }
}
