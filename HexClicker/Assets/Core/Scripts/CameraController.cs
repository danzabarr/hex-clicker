using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraController : MonoBehaviour
{

    public new Camera camera;
    public Transform pitch;
    public Transform zoom;
    public PostProcessVolume postProcessing;

    public float speed;
    public float acceleration;
    public float dampening;

    public float rotationSpeed;
    public float rotationAcceleration;
    public float rotationDampening;

    public float zoomSpeed;
    public float zoomAcceleration;
    public float zoomDampening;

    private Vector3 velocity;
    private float rotationVelocity;
    private float zoomVelocity;
    private float zoomAmount = 0.5f;
    private DepthOfField depthOfField;

    private void Awake()
    {
        postProcessing.profile.TryGetSettings(out depthOfField);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 input = Vector2.zero;
        float inputRotation = 0;
        float inputZoom = Input.mouseScrollDelta.y;

        if (Input.GetKey(KeyCode.W))
            input.z++;
        if (Input.GetKey(KeyCode.A))
            input.x--;
        if (Input.GetKey(KeyCode.S))
            input.z--;
        if (Input.GetKey(KeyCode.D))
            input.x++;
        //if (Input.GetKey(KeyCode.Space))
        //    input.y++;
        //if (Input.GetKey(KeyCode.X))
        //    input.y--;

        input.Normalize();

        if (Input.GetKey(KeyCode.Q))
            inputRotation++;
        if (Input.GetKey(KeyCode.E))
            inputRotation--;


        velocity += (transform.right * input.x + transform.up * input.y + transform.forward * input.z) * acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, speed);
        transform.position += velocity;
        velocity *= Mathf.Pow(dampening, Time.deltaTime);

        rotationVelocity += inputRotation * rotationAcceleration * Time.deltaTime;
        rotationVelocity = Mathf.Clamp(rotationVelocity, -rotationSpeed, +rotationSpeed);
        transform.rotation *= Quaternion.Euler(0, rotationVelocity, 0);
        rotationVelocity *= Mathf.Pow(rotationDampening, Time.deltaTime);

        zoomVelocity += inputZoom * zoomAcceleration * Time.deltaTime;
        zoomVelocity = Mathf.Clamp(zoomVelocity, -zoomSpeed, zoomSpeed);

        zoomAmount = Mathf.Clamp(zoomAmount - zoomVelocity, 0, 1);

        pitch.localRotation = Quaternion.Euler(zoomAmount * 50 + 40, pitch.localRotation.eulerAngles.y, pitch.localRotation.eulerAngles.z);
        zoom.localPosition = Vector3.forward * -(zoomAmount * 22);

        zoomVelocity *= Mathf.Pow(zoomDampening, Time.deltaTime);
        depthOfField.focusDistance.value = Mathf.Min(zoomAmount * 17 + 2, 20);
        depthOfField.focalLength.value = 10 + (zoomAmount * 22 - 1) * 10;

        camera.transform.localPosition = Vector3.zero;

        float terrain = HexMap.Instance.SampleHeight(camera.transform.position.x, camera.transform.position.z) + 1;
        camera.transform.position = new Vector3(camera.transform.position.x, Mathf.Max(terrain, camera.transform.position.y), camera.transform.position.z);

    }
}
