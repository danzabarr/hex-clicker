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

    public void Add(Matrix4x4 matrix, Color color)
    {
        if (current == null || current.Full)
            batches.Add(current = new Batch());

        current.Add(matrix, color);
    }

    public void Clear()
    {
        batches = new List<Batch>();
        current = null;
    }

    public void Draw(Mesh mesh, Material material, int layer)
    {
        foreach (Batch batch in batches)
            batch.Draw(mesh, material, layer);
    }

    [System.Serializable]
    public class Batch
    {
        public MaterialPropertyBlock Block { get; private set; }
        public Matrix4x4[] Matrices { get; } = new Matrix4x4[1023];
        public Vector4[] Colors { get; } = new Vector4[1023];
        public int Count { get; private set; }
        public bool Full => (Count >= 1023);
        public bool Add(Matrix4x4 matrix, Color color)
        {
            if (Full) 
                return false;
            if (Block == null)
                Block = new MaterialPropertyBlock();

            Matrices[Count] = matrix;
            Colors[Count] = color;
            Block.SetVectorArray("_Color", Colors);
            Count++;
            return true;
        }
        public void Draw(Mesh mesh, Material material, int layer)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, Matrices, Count, Block, UnityEngine.Rendering.ShadowCastingMode.On, true, layer);
        }
    }
}
    
