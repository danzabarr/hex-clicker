using UnityEngine;

namespace HexClicker.Navigation
{
    public class PathMaker : MonoBehaviour
    {
        private Vector2 lastPosition;
        private Vector2Int nearestNode;
        [SerializeField] private Renderer[] renderers;
        public float visualInfluence;
        public float nodeInfluence;

        public void Update()
        {
            //Sets the alpha value of the color of the main material of all the renderers proportionate to the movement since last update.
            Vector2 position = transform.position.xz();
            float distance = Vector2.Distance(position, lastPosition);
            lastPosition = position;
            foreach (Renderer renderer in renderers)
            {
                Color c = renderer.material.color;
                renderer.material.color = new Color(c.r, c.g, c.b, Mathf.Clamp(distance * visualInfluence, 0, 1));
            }

            //Apply path to navigation nodes
            Vector2Int nearestNode = Vector2Int.RoundToInt((transform.position * NavigationGraph.Resolution / World.Map.TileSize).xz());
            if (this.nearestNode != nearestNode)
            {
                this.nearestNode = nearestNode;
                if (NavigationGraph.TryGetNode(nearestNode, out Node node))
                    node.DesirePathCost -= nodeInfluence;
            }
        }
    }
}
