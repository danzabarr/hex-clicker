using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenCast : MonoBehaviour
{
    public static Instance MouseTerrain { get; private set; }
    public static Instance CenterTerrain { get; private set; }
    public static Instance MouseScene { get; private set; }

    public enum ScreenPosition
    {
        Mouse,
        Center,
        Fixed,
        Relative
    }

    public class Instance
    {
        private Camera camera;
        private ScreenPosition positionType;
        private Vector2 positionVector;
        private float maxDistance;
        private int layerMask;

        private bool returned;
        private RaycastHit hit;

        public Instance(Camera camera, ScreenPosition positionType, Vector2 positionVector, float maxDistance, int layerMask)
        {
            this.camera = camera;
            this.positionType = positionType;
            this.positionVector = positionVector;
            this.maxDistance = maxDistance;
            this.layerMask = layerMask;
        }

        public bool Cast(out RaycastHit hit)
        {
            hit = this.hit;
            return returned;
        }

        public bool Cast<T>(out T component) where T : Component
        {
            component = null;
            if (returned)
                component = hit.collider.GetComponent<T>();
            return component != null;
        }

        public bool Cast<T>(out RaycastHit hit, out T component) where T : Component
        {
            component = null;
            hit = this.hit;
            if (returned)
                component = hit.collider.GetComponent<T>();
            return component != null;
        }

        public void Update()
        {
            Vector3 position = positionVector;
            switch(positionType)
            {
                case ScreenPosition.Mouse:
                    position = Input.mousePosition;
                    break;
                case ScreenPosition.Center:
                    position = new Vector3(Screen.width / 2f, Screen.height / 2f);
                    break;
                case ScreenPosition.Relative:
                    position = new Vector3(Screen.width * positionVector.x, Screen.height * positionVector.y);
                    break;
            }
            returned = Physics.Raycast(camera.ScreenPointToRay(position), out hit, maxDistance, layerMask);
        }
    }

    private void Awake()
    {
        Camera main = Camera.main;
        MouseTerrain = new Instance(main, ScreenPosition.Mouse, default, 1000, LayerMask.GetMask("Terrain"));
        CenterTerrain = new Instance(main, ScreenPosition.Center, default, 1000, LayerMask.GetMask("Terrain"));
        MouseScene = new Instance(main, ScreenPosition.Mouse, default, 1000, LayerMask.GetMask("Terrain", "Units", "Buildings"));
    }

    public void LateUpdate()
    {
        MouseTerrain.Update();
        CenterTerrain.Update();
        MouseScene.Update();
    }
}
