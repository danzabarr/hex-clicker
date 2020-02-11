using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InstancedRenderer
{
    private List<Batch> batches = new List<Batch>();
    private Batch current;

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

   //DrawMeshInstancedIndirect example. Can't get this to work with shadows casting/receiving... Probably to do with render order.

   /*
   private void SetupTrees(HexagonTile tile)
   {
       foreach (Vector3 vertex in tile.Mesh.vertices)
       {
           // Build matrix.
           Vector4 position = tile.transform.position + vertex;

           float treeSample = SampleTree(position.x, position.z);

           if (treeSample < treesThreshold)
               continue;

           if (position.y < 0.05f)
               continue;

           if (position.y > .6f)
               continue;

           position.w = Random.Range(.25f, .5f);

           positions.Add(position);
       }
       instanceCount = positions.Count;
   }




   private int instanceCount = 0;
   public Mesh instanceMesh;
   public Material instanceMaterial;

   private int cachedInstanceCount = -1;
   private ComputeBuffer positionBuffer;
   private ComputeBuffer argsBuffer;
   private List<Vector4> positions = new List<Vector4>(); 

   void DrawInstances()
   {

       // Update starting position buffer
       if (cachedInstanceCount != instanceCount)
           SetupInstances(positions);

       // Render
       instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
       Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, Vector3.one * 1000), argsBuffer);
   }

   void SetupInstances(List<Vector4> positions)
   {
       instanceCount = positions.Count;
       positionBuffer = new ComputeBuffer(instanceCount, 16);
       positionBuffer.SetData(positions);

       // indirect args
       uint numIndices = (instanceMesh != null) ? (uint)instanceMesh.GetIndexCount(0) : 0;
       uint[] args = new uint[5] { numIndices, (uint)instanceCount, 0, 0, 0 };
       argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
       argsBuffer.SetData(args);

       cachedInstanceCount = instanceCount;
   }
   */
}

