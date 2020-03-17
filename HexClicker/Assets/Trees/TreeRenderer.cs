using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Trees
{
    public class TreeRenderer : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        [SerializeField] private UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        [SerializeField] private bool receiveShadows;
        [SerializeField, Layer] private int layer;

        public static readonly int BatchSize = 1023;

        private Dictionary<Vector2Int, Tree> allTrees = new Dictionary<Vector2Int, Tree>();
        private List<Batch> batches = new List<Batch>();

        public bool TryGet(Vector2Int node, out Tree tree) => allTrees.TryGetValue(node, out tree);
        public int Count => allTrees.Count;

        public void Clear()
        {
            allTrees = new Dictionary<Vector2Int, Tree>();
            batches = new List<Batch>();
        }

        private void Update()
        {
            for (int i = batches.Count - 1; i >= 0; i--)
            {
                if (batches[i].Changed)
                    batches[i].CreateArrays();

                if (Count > 0)
                    batches[i].Draw(mesh, material, shadowCastingMode, receiveShadows, layer);
            }
        }

        //Untested
        public void Add(ICollection<Tree> list)
        {
            int currentBatchIndex = -1;
            Batch currentBatch = null;

            foreach (Tree tree in list)
            {
                if (tree == null)
                    continue;

                if (currentBatch == null || currentBatch.Count >= BatchSize)
                    currentBatchIndex = FreeBatch(out currentBatch);

                if (currentBatch != null && currentBatch.Add(tree))
                {
                    tree.batch = currentBatchIndex;
                    allTrees.Add(tree.vertex, tree);
                }
            }

            int FreeBatch(out Batch batch)
            {
                batch = null;
                for (int i = 0; i < batches.Count; i++)
                {
                    Batch b = batches[i];
                    if (b.Count < BatchSize)
                    {
                        batch = b;
                        return i;
                    }
                }
                batches.Add(batch = new Batch());
                return batches.Count - 1;
            }
        }

        public bool Add(Tree tree)
        {
            if (tree == null)
                return false;

            if (allTrees.ContainsKey(tree.vertex))
                return false;

            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].Count >= BatchSize)
                    continue;
                if (batches[i].Add(tree))
                {
                    tree.batch = i;
                    allTrees.Add(tree.vertex, tree);
                    return true;
                }
            }

            Batch newBatch = new Batch();
            batches.Add(newBatch);

            if (newBatch.Add(tree))
            {
                tree.batch = batches.Count - 1;
                allTrees.Add(tree.vertex, tree);
                return true;
            }
            else return false;
        }

        public bool Remove(Tree tree)
        {
            if (tree == null)
                return false;

            if (tree.batch < 0 || tree.batch >= batches.Count)
                return false;

            bool removedFromBatch = batches[tree.batch].Remove(tree.index);
            bool removedFromDict = allTrees.Remove(tree.vertex);

            return removedFromBatch || removedFromDict;
        }

        public class Batch
        {
            private Tree[] trees = new Tree[BatchSize];
            public int Count { get; private set; }
            public bool Changed { get; private set; } = true;

            private Matrix4x4[] matrices;
            private MaterialPropertyBlock block;

            public void Draw(Mesh mesh, Material material, UnityEngine.Rendering.ShadowCastingMode shadowCastingMode, bool receiveShadows, int layer)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, matrices, Count, block, shadowCastingMode, receiveShadows, layer);
            }

            public void CreateArrays()
            {
                Count = 0;

                List<Matrix4x4> newPositions = new List<Matrix4x4>();
                List<Vector4> newColors = new List<Vector4>();

                for (int i = 0; i < trees.Length; i++)
                {
                    if (trees[i] != null)
                    {
                        trees[i].index = i;
                        newPositions.Add(Matrix4x4.TRS(trees[i].position, Quaternion.Euler(0, trees[i].rotation, 0), trees[i].scale));
                        newColors.Add(trees[i].color);
                        Count++;
                    }
                }

                matrices = newPositions.ToArray();

                block = new MaterialPropertyBlock();
                if (Count > 0)
                    block.SetVectorArray("_Color", newColors.ToArray());

                Changed = false;
            }

            public Tree Get(int index)
            {
                if (index < 0 || index >= Count)
                    return null;
                return trees[index];
            }

            public bool Add(Tree tree)
            {
                if (tree == null)
                    return false;

                if (Count >= BatchSize)
                    return false;

                for (int i = 0; i < BatchSize; i++)
                {
                    if (trees[i] == null)
                    {
                        trees[i] = tree;
                        tree.index = i;
                        Count++;
                        Changed = true;
                        return true;
                    }
                }
                return false;
            }

            public bool Remove(int index)
            {
                if (index < 0 || index >= Count)
                    return false;

                if (trees[index] == null)
                    return false;

                trees[index] = null;
                Count--;
                Changed = true;
                return true;
            }
        }
    }
}
