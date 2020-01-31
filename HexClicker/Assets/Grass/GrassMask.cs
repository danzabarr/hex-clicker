using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassMask : MonoBehaviour
{

    [SerializeField]
    public int resolution;
    [SerializeField]
    private Camera cam;

    //[SerializeField]
    //private TerrainChunk chunk;

    [SerializeField]
    private MeshRenderer grassRenderer;

    [SerializeField]
    private MeshRenderer maskBackground;

    private RenderTexture texture;

    //[SerializeField]
    //private NoiseSettings noiseSettings;

    [SerializeField]
    private Shader grassMaskShader;

    public void SetWindDistortionTextureOffset(Vector2 offset)
    {
        grassRenderer.material.SetTextureOffset("_WindDistortionMap", offset);
    }

    public void CreateMaskTextures()
    {
        texture = new RenderTexture(resolution, resolution, 0);
        cam.targetTexture = texture;
        grassRenderer.material.SetTexture("_GrassMask", texture);

        Texture2D background = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        background.filterMode = FilterMode.Point;

        Random.InitState(0);
        //int seedX = Random.Range(-50000, 50000);
        //int seedZ = Random.Range(-50000, 50000);

        //float[] noise = ChunkManager.GenerateHeightMap(chunk.ChunkX, chunk.ChunkZ, 0, 0, resolution - 1, noiseSettings);

        //for (int i = 0; i < resolution * resolution; i++)
        //{
        //    int x = i % resolution;
        //    int z = i / resolution;
         //   
        //    if (noise[i] > .5) background.SetPixel(x, z, Color.black);
       // }
       //
        //background.Apply();
//
        //maskBackground.material.SetTexture("_MainTex", background);

       
    }

    public void Render()
    {
        cam.enabled = true;
        cam.Render();
        cam.enabled = false;
    }
}
