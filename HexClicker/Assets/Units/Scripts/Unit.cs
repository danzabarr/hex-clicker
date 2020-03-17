using HexClicker.Behaviour;
using HexClicker.World;
using System.Collections;
using UnityEngine;

namespace HexClicker.Units
{
    [RequireComponent(typeof(Navigation.Agent))]
    public class Unit : Behaviour.Agent
    {
        public Navigation.Agent NavAgent { get; private set; }
        public Trees.Tree TargetTree { get; set; }

        private void Awake()
        {
            NavAgent = GetComponent<Navigation.Agent>();
        }

        public IEnumerator Chop(ChopTreeNode node, float duration)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                yield return null;
            }

            Map.Instance.RemoveTree(TargetTree.vertex, out _);
            TargetTree = null;
            End(node, StateResult.Succeeded);
        }
    }
}
