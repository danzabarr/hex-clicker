using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InstancedRenderer
{
    private MaterialPropertyBlock materialPropertyBlock;
    private List<Batch> batches = new List<Batch>();
    private Batch current;

    public InstancedRenderer(MaterialPropertyBlock materialPropertyBlock)
    {
        this.materialPropertyBlock = materialPropertyBlock;
    }

    public void Add(Matrix4x4 matrix)
    {
        if (current == null || current.Full)
            batches.Add(current = new Batch());

        current.Add(matrix);
    }

    public void Clear()
    {
        batches = new List<Batch>();
        current = null;
    }

    public void Draw(Mesh mesh, Material material, int layer)
    {
        foreach (Batch batch in batches)
            batch.Draw(mesh, material, materialPropertyBlock, layer);
    }

    [System.Serializable]
    public class Batch
    {
        public Matrix4x4[] Matrices { get; } = new Matrix4x4[1024];
        public int Count { get; private set; }
        public bool Full => (Count >= 1023);
        public bool Add(Matrix4x4 matrix)
        {
            if (Full)
                return false;
            Matrices[Count] = matrix;
            Count++;
            return true;
        }
        public void Draw(Mesh mesh, Material material, MaterialPropertyBlock block, int layer)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, Matrices, Count, block, UnityEngine.Rendering.ShadowCastingMode.On, true, layer);
        }
    }
}
    
