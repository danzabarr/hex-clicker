using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//FOR DEMONSTRATION ONLY
public class MoveAround : MonoBehaviour
{
    public float speed = 1;

    private void Update()
    {
        transform.position = new Vector3(Mathf.Sin(Time.fixedTime) * speed, 0, 0);
    }
}
