using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Units
{
    [RequireComponent(typeof(Navigation.Agent))]
    public class Unit : MonoBehaviour
    {
        private Navigation.Agent agent;

        private void Awake()
        {
            agent = GetComponent<Navigation.Agent>();
        }

        private void Update()
        {
            MoveMouseClick();
        }

        private void MoveRandomly()
        {
            if (agent.Stopped)
            {
                float range = Random.Range(3f, 20f);

                Vector2 randomPosition = Random.insideUnitCircle * range + transform.position.xz();
                agent.SetDestination(World.Map.Instance.OnTerrain(randomPosition), 5000);
            }
        }
        private void MoveMouseClick()
        {
            if (Input.GetMouseButtonDown(1) && !UI.UIMethods.IsMouseOverUI)
            {
                if (ScreenCast.MouseScene.Cast(out RaycastHit hitInfo))
                {

                    Buildings.BuildingPart bp = hitInfo.collider.GetComponent<Buildings.BuildingPart>();

                    if (bp != null)
                    {
                        agent.SetDestination(bp.Parent, 5000);
                    }
                    else
                    {
                        agent.SetDestination(hitInfo.point, 5000);
                    }
                }
            }
        }
    }
}
