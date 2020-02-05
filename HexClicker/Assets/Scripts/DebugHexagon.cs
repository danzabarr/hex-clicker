using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class DebugHexagon : MonoBehaviour
{
    public int x;
    public int y;
    public Color color;
    private GUIStyle style;

    public void OnValidate()
    {
        Vector2 cartesian = HexUtils.HexToCartesian(x, y);
        transform.position = new Vector3(cartesian.x, 0, cartesian.y);
        
    }

    public void OnSceneGUI()
    {
     //   style = new GUIStyle(GUI.skin.textArea);
       // style.normal.textColor = Color.white;
    }

    public void OnDrawGizmos()
    {
        //Gizmos.DrawWireMesh(HexUtils.Mesh, transform.position);
        Gizmos.color = color;
        Gizmos.DrawMesh(HexUtils.Mesh, transform.position);
        Gizmos.color = Color.white;
        HexUtils.DrawHexagon(x, y);
        Handles.Label(transform.position, x + "," + y);

        
    }
}
