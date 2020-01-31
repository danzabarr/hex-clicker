using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WaterPositioning : MonoBehaviour
{
    public GameObject[] water;
    public Transform player;
    public float tileSize;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        int tileX = Mathf.FloorToInt(player.transform.localPosition.x / tileSize);
        int tileZ = Mathf.FloorToInt(player.transform.localPosition.z / tileSize);

        float waterPosX = (tileX + .5f) * tileSize;
        float waterPosZ = (tileZ + .5f) * tileSize;

        foreach(GameObject w in water)
            w.transform.localPosition = new Vector3(waterPosX, w.transform.localPosition.y, waterPosZ);

    }

}
