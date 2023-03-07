using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public float m_speed = 1.0f;

    public float m_zoomSpeed = 1.0f;
    public float m_startZoom = 4;
    public float m_minZoom = 7;
    public float m_maxZoom = 2;

    private Transform m_world;
    private Camera m_camera;

    // Start is called before the first frame update
    void Start()
    {
        m_world = GameObject.Find("World").transform;
        m_camera = GetComponent<Camera>();
        m_camera.orthographicSize = m_startZoom;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Left-Right rotate
        float velocity = 0.0f; 
        if (Input.GetKey(KeyCode.Q)) { velocity += 1.0f; }
        if (Input.GetKey(KeyCode.D)) { velocity -= 1.0f; }
        velocity *= m_speed;

        transform.RotateAround(m_world.position, Vector3.up, velocity);
        transform.LookAt(m_world);

        // Zoom
        m_camera.orthographicSize = System.Math.Max(System.Math.Min(m_camera.orthographicSize - m_zoomSpeed * Input.GetAxis("Mouse ScrollWheel"), m_minZoom), m_maxZoom);
    }
}
