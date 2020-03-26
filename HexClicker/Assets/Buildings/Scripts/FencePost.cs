using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FencePost : MonoBehaviour
{
    [SerializeField] private Transform[] connected;
    [SerializeField] private Mesh barsMesh;
    [SerializeField] private Material barsMaterial;

    public void Update()
    {
        if (connected != null)
        foreach (Transform post in connected)
        {
            float xzDistance = Vector2.Distance(transform.position.xz(), post.transform.position.xz());
            float angle = Mathf.Atan2(transform.position.x - post.transform.position.x, transform.position.z - post.transform.position.z) * Mathf.Rad2Deg - 90;
            float modelLength = .5f;
            float shear = (transform.position.y - post.transform.position.y) / modelLength;

            Vector3 scale = new Vector3(xzDistance / modelLength, 1, 1);

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetFloat("_Shear", shear);
            Graphics.DrawMesh(barsMesh, Matrix4x4.TRS(transform.position, Quaternion.AngleAxis(angle, Vector3.up), scale), barsMaterial, gameObject.layer, null, 0, properties);
        }
    }
}
